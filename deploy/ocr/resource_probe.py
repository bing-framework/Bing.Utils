"""Measure OCR host and worker memory for a fixed large-image workload."""

from __future__ import annotations

import argparse
import asyncio
import importlib.metadata
import io
import json
import os
import platform
import sys
import time
from dataclasses import replace
from datetime import datetime, timezone
from pathlib import Path

import psutil
from PIL import Image

from config import Settings
from scheduler import BusyError, InferenceScheduler, InferenceTimeout
from worker import ProcessWorker


SCENARIO_IMAGES = {
    "maximum-image": (5_000, 4_000),
    "queue": (1_000, 1_000),
}
MAX_DIMENSION = 8_192
MAX_PROBE_SECONDS = 120.0
CLEANUP_RESERVE_SECONDS = 10.0
CLEANUP_TIMEOUT_SECONDS = 7.0
SAMPLE_INTERVAL_SECONDS = 0.1


class ProbeFailure(RuntimeError):
    """Raised when the probe cannot complete its declared workload."""


class _ObservedWorker:
    def __init__(self, worker: ProcessWorker):
        self._worker = worker
        self._gate_enabled = False
        self._gate_entered = asyncio.Event()
        self._gate_release = asyncio.Event()
        self._active = 0
        self.max_actual_parallelism = 0
        self.actual_inference_calls = 0

    @property
    def ready(self) -> bool:
        return self._worker.ready

    @property
    def profile_id(self) -> str | None:
        return self._worker.profile_id

    async def ensure_ready(self) -> str:
        return await self._worker.ensure_ready()

    async def recover(self) -> None:
        await self._worker.recover()

    async def close(self) -> None:
        await self._worker.close()

    def hold_inference(self) -> None:
        self._gate_entered = asyncio.Event()
        self._gate_release = asyncio.Event()
        self._gate_enabled = True
        self._active = 0
        self.max_actual_parallelism = 0
        self.actual_inference_calls = 0

    def release_inference(self) -> None:
        self._gate_release.set()

    async def infer(self, image_bytes: bytes, text_rec_score_thresh: float) -> dict:
        if self._gate_enabled:
            self._gate_entered.set()
            await self._gate_release.wait()

        self._active += 1
        self.actual_inference_calls += 1
        self.max_actual_parallelism = max(self.max_actual_parallelism, self._active)
        try:
            return await self._worker.infer(image_bytes, text_rec_score_thresh)
        finally:
            self._active -= 1


class _MemorySampler:
    def __init__(self, interval_seconds: float):
        self._host = psutil.Process(os.getpid())
        self._interval_seconds = interval_seconds
        self._stop = asyncio.Event()
        self.sample_count = 0
        self.max_process_count = 0
        self.peak_combined_rss_bytes = 0
        self.peak_host_rss_bytes = 0
        self.peak_children_rss_bytes = 0
        self.peak_combined_working_set_bytes: int | None = None
        self._working_set_samples = 0

    def stop(self) -> None:
        self._stop.set()

    async def run(self) -> None:
        self.sample()
        while not self._stop.is_set():
            try:
                await asyncio.wait_for(self._stop.wait(), timeout=self._interval_seconds)
            except asyncio.TimeoutError:
                self.sample()
        self.sample()

    def sample(self) -> None:
        try:
            children = self._host.children(recursive=True)
        except (psutil.NoSuchProcess, psutil.ZombieProcess, psutil.AccessDenied):
            children = []

        processes = [self._host, *children]
        unique: dict[int, psutil.Process] = {process.pid: process for process in processes}
        host_rss = 0
        children_rss = 0
        working_set = 0
        working_set_count = 0

        for pid, process in unique.items():
            try:
                info = process.memory_info()
            except (psutil.NoSuchProcess, psutil.ZombieProcess, psutil.AccessDenied):
                continue
            if pid == self._host.pid:
                host_rss += info.rss
            else:
                children_rss += info.rss
            peak_wset = getattr(info, "peak_wset", None)
            if peak_wset is not None:
                working_set += int(peak_wset)
                working_set_count += 1

        combined_rss = host_rss + children_rss
        self.sample_count += 1
        self.max_process_count = max(self.max_process_count, len(unique))
        self.peak_host_rss_bytes = max(self.peak_host_rss_bytes, host_rss)
        self.peak_children_rss_bytes = max(self.peak_children_rss_bytes, children_rss)
        self.peak_combined_rss_bytes = max(self.peak_combined_rss_bytes, combined_rss)
        if working_set_count:
            self._working_set_samples += 1
            if self.peak_combined_working_set_bytes is None:
                self.peak_combined_working_set_bytes = working_set
            else:
                self.peak_combined_working_set_bytes = max(
                    self.peak_combined_working_set_bytes,
                    working_set,
                )

    def as_dict(self) -> dict:
        return {
            "samplingIntervalSeconds": self._interval_seconds,
            "sampleCount": self.sample_count,
            "maxObservedProcessCount": self.max_process_count,
            "peakRssBytes": {
                "host": self.peak_host_rss_bytes,
                "children": self.peak_children_rss_bytes,
                "combined": self.peak_combined_rss_bytes,
            },
            "peakWorkingSetBytes": self.peak_combined_working_set_bytes,
            "workingSetMetric": "psutil memory_info().peak_wset" if self._working_set_samples else None,
            "workingSetInterpretation": (
                "Sum of per-process lifetime peak_wset values; their peaks may not have occurred simultaneously. "
                "RSS is a separately sampled simultaneous sum."
            ),
        }


async def _connected() -> bool:
    return False


def _create_png(width: int, height: int) -> bytes:
    output = io.BytesIO()
    with Image.new("RGB", (width, height), color="white") as image:
        image.save(output, format="PNG", compress_level=1)
    return output.getvalue()


def _distribution_version(name: str) -> str | None:
    try:
        return importlib.metadata.version(name)
    except importlib.metadata.PackageNotFoundError:
        return None


async def _wait_for_gate(observer: _ObservedWorker, first_request: asyncio.Task, timeout: float) -> None:
    try:
        await asyncio.wait_for(observer._gate_entered.wait(), timeout=timeout)
    except asyncio.TimeoutError as error:
        if first_request.done():
            first_request.result()
        raise ProbeFailure("The active request did not reach the controlled inference gate.") from error


async def _wait_for_queue_depth(scheduler: InferenceScheduler, expected: int, timeout: float) -> int:
    deadline = time.monotonic() + timeout
    maximum = 0
    while time.monotonic() < deadline:
        depth = len(scheduler._waiting)
        maximum = max(maximum, depth)
        if depth >= expected:
            return maximum
        await asyncio.sleep(0.01)
    raise ProbeFailure(f"Only {maximum} of {expected} expected waiting requests were admitted.")


async def _wait_for_process_exit(process, timeout: float) -> int:
    deadline = time.monotonic() + timeout
    while process.is_alive() and time.monotonic() < deadline:
        await asyncio.sleep(0.02)
    process.join(timeout=0)
    if process.is_alive():
        raise ProbeFailure(f"Timed-out worker PID {process.pid} did not exit within the remaining probe budget.")
    return process.exitcode


async def _record_request(
    scheduler: InferenceScheduler,
    label: str,
    image_bytes: bytes,
    records: list[dict],
    counts: dict[str, int],
) -> dict | None:
    started = time.monotonic()
    try:
        response = await scheduler.submit(image_bytes, 0.0, _connected)
    except BusyError:
        counts["capacityRejected"] += 1
        status = "capacity_rejected"
        error = None
        error_type = "BusyError"
        response = None
    except (InferenceTimeout, asyncio.TimeoutError) as caught:
        counts["timeout"] += 1
        status = "timeout"
        error = f"{type(caught).__name__}: {caught}"
        error_type = type(caught).__name__
        response = None
    except asyncio.CancelledError:
        counts["cancelled"] += 1
        records.append(
            {
                "id": label,
                "status": "cancelled",
                "durationSeconds": round(time.monotonic() - started, 6),
            }
        )
        raise
    except Exception as caught:
        counts["failure"] += 1
        status = "failure"
        error = f"{type(caught).__name__}: {caught}"
        error_type = type(caught).__name__
        response = None
    else:
        counts["success"] += 1
        status = "success"
        error = None
        error_type = None

    record = {
        "id": label,
        "status": status,
        "durationSeconds": round(time.monotonic() - started, 6),
    }
    if error is not None:
        record["error"] = error
    if error_type is not None:
        record["errorType"] = error_type
    if response is not None:
        record["recognizedTextCount"] = len(response.get("rec_texts", []))
        record["inputImageShape"] = response.get("input_img_shape")
    records.append(record)
    return response


async def _measure_capacity_probe(
    scheduler: InferenceScheduler,
    observer: _ObservedWorker,
    image_bytes: bytes,
    records: list[dict],
    counts: dict[str, int],
    probe_result: dict,
    timeout: float,
) -> dict:
    observer.hold_inference()
    started = time.monotonic()
    tasks: list[asyncio.Task] = []
    max_queued = 0
    rejected_before = counts["capacityRejected"]
    probe_result.update(
        {
            "durationSeconds": None,
            "requestedConcurrency": 10,
            "maxActualParallelism": 0,
            "maxQueuedRequests": 0,
            "actualInferenceCalls": 0,
            "capacityRejectedRequests": 0,
            "classification": "Controlled queue occupancy probe; not natural concurrent throughput or P95.",
        }
    )
    try:
        first = asyncio.create_task(_record_request(scheduler, "queue-01", image_bytes, records, counts))
        tasks.append(first)
        await _wait_for_gate(observer, first, timeout)

        for queued_count in range(1, 9):
            request_number = queued_count + 1
            tasks.append(
                asyncio.create_task(
                    _record_request(
                        scheduler,
                        f"queue-{request_number:02d}",
                        image_bytes,
                        records,
                        counts,
                    )
                )
            )
            max_queued = max(max_queued, await _wait_for_queue_depth(scheduler, queued_count, timeout))
            probe_result["maxQueuedRequests"] = max_queued

        rejected = await _record_request(scheduler, "queue-10", image_bytes, records, counts)
        if rejected is not None or records[-1]["status"] != "capacity_rejected":
            raise ProbeFailure("The tenth concurrent request was not rejected at the full queue limit.")
        probe_result["capacityRejectedRequests"] = counts["capacityRejected"] - rejected_before

        observer.release_inference()
        completed = await asyncio.gather(*tasks, return_exceptions=True)
        failures = [item for item in completed if isinstance(item, BaseException)]
        if failures:
            raise ProbeFailure(f"{len(failures)} queued request task(s) did not complete.") from failures[0]

        return probe_result
    finally:
        observer.release_inference()
        unfinished = [task for task in tasks if not task.done()]
        for task in unfinished:
            task.cancel()
        if unfinished:
            await asyncio.gather(*unfinished, return_exceptions=True)
        probe_result["durationSeconds"] = round(time.monotonic() - started, 6)
        probe_result["maxActualParallelism"] = observer.max_actual_parallelism
        probe_result["maxQueuedRequests"] = max_queued
        probe_result["actualInferenceCalls"] = observer.actual_inference_calls
        probe_result["capacityRejectedRequests"] = counts["capacityRejected"] - rejected_before


def _settings_for_probe(operation_budget_seconds: float, scenario: str, result: dict) -> Settings:
    settings = Settings.from_env()
    width, height = SCENARIO_IMAGES[scenario]
    pixels = width * height
    result["configuredLimits"] = {
        "maxBodyBytes": settings.max_body_bytes,
        "maxImageBytes": settings.max_image_bytes,
        "maxPixels": settings.max_pixels,
        "maxDimension": settings.max_dimension,
        "maxWaiting": settings.max_waiting,
        "inferenceTimeoutSeconds": settings.inference_timeout_seconds,
        "startupTimeoutSeconds": settings.startup_timeout_seconds,
        "workerJoinTimeoutSeconds": settings.worker_join_timeout_seconds,
        "bodyTimeoutSeconds": settings.body_timeout_seconds,
        "cpuThreads": settings.cpu_threads,
    }
    if settings.max_pixels < pixels:
        raise ProbeFailure(
            f"Configured maxPixels={settings.max_pixels} cannot admit the {pixels}-pixel {scenario} image."
        )
    if settings.max_dimension < max(width, height):
        raise ProbeFailure(
            f"Configured maxDimension={settings.max_dimension} cannot admit a {width}x{height} {scenario} image."
        )
    if scenario == "queue" and settings.max_waiting != 8:
        raise ProbeFailure(f"Configured maxWaiting={settings.max_waiting}; the probe requires exactly 8 waiting slots.")

    adjusted = replace(
        settings,
        startup_timeout_seconds=min(settings.startup_timeout_seconds, operation_budget_seconds),
        worker_join_timeout_seconds=min(settings.worker_join_timeout_seconds, CLEANUP_TIMEOUT_SECONDS / 2),
    )
    result["effectiveProbeDeadlines"] = {
        "startupTimeoutSeconds": adjusted.startup_timeout_seconds,
        "workerJoinTimeoutSeconds": adjusted.worker_join_timeout_seconds,
    }
    return adjusted


def _empty_result(max_seconds: float, scenario: str) -> dict:
    width, height = SCENARIO_IMAGES[scenario]
    return {
        "schemaVersion": 1,
        "status": "failed",
        "scenario": scenario,
        "startedAtUtc": datetime.now(timezone.utc).isoformat(),
        "configuredMaxSeconds": max_seconds,
        "durationSeconds": None,
        "error": None,
        "runtime": {},
        "environment": {
            "pythonVersion": sys.version,
            "pythonImplementation": platform.python_implementation(),
            "os": platform.system(),
            "osRelease": platform.release(),
            "architecture": platform.machine(),
            "processor": platform.processor(),
            "logicalCpuCount": os.cpu_count(),
            "physicalCpuCount": psutil.cpu_count(logical=False),
        },
        "configuredLimits": None,
        "effectiveProbeDeadlines": None,
        "modelProfileId": None,
        "modelDirectory": None,
        "input": {
            "format": "PNG",
            "width": width,
            "height": height,
            "pixels": width * height,
            "maxEdge": max(width, height),
            "maxAllowedDimension": MAX_DIMENSION,
            "pngBytes": None,
        },
        "singleRequest": None,
        "controlledQueueOccupancyProbe": None,
        "counts": {"success": 0, "capacityRejected": 0, "timeout": 0, "failure": 0, "cancelled": 0},
        "requests": [],
        "memory": None,
        "baseline": {"historicalEquivalent": "not_applicable"},
        "thresholds": {"cer": None, "p95": None, "status": "not_set"},
        "measurementScope": {
            "includes": "Python host scheduler process plus its real ProcessWorker child process(es).",
            "excludes": ["HTTP request handling", ".NET client memory", "browser memory"],
        },
    }


async def _execute_probe(
    result: dict,
    scenario: str,
    operation_budget_seconds: float,
    cleanup_reserve_seconds: float,
) -> None:
    worker: ProcessWorker | None = None
    scheduler: InferenceScheduler | None = None
    sampler = _MemorySampler(SAMPLE_INTERVAL_SECONDS)
    sampler_task = asyncio.create_task(sampler.run())
    close_error: BaseException | None = None
    probe_started = time.monotonic()
    try:
        settings = _settings_for_probe(operation_budget_seconds, scenario, result)
        result["modelDirectory"] = str(settings.models_dir.resolve())
        result["runtime"] = {
            name: _distribution_version(name)
            for name in ("paddleocr", "paddlex", "paddlepaddle", "Pillow", "psutil")
        }

        worker = ProcessWorker(settings)
        observer = _ObservedWorker(worker)
        scheduler = InferenceScheduler(
            observer,
            max_waiting=settings.max_waiting,
            inference_timeout_seconds=30.0,
        )

        warmup_started = time.monotonic()
        profile_id = await asyncio.wait_for(worker.ensure_ready(), timeout=operation_budget_seconds)
        result["modelProfileId"] = profile_id
        result["warmupDurationSeconds"] = round(time.monotonic() - warmup_started, 6)

        width, height = SCENARIO_IMAGES[scenario]
        png_started = time.monotonic()
        image_bytes = await asyncio.wait_for(
            asyncio.to_thread(_create_png, width, height),
            timeout=max(0.1, operation_budget_seconds - (time.monotonic() - probe_started)),
        )
        result["imageCreationDurationSeconds"] = round(time.monotonic() - png_started, 6)
        result["input"]["pngBytes"] = len(image_bytes)
        if len(image_bytes) > settings.max_image_bytes:
            raise ProbeFailure(
                f"Generated PNG size {len(image_bytes)} exceeds configured maxImageBytes={settings.max_image_bytes}."
            )

        if scenario == "maximum-image":
            result["singleRequest"] = {
                "requestedConcurrency": 1,
                "durationSeconds": None,
                "workerPidBeforeRequest": None,
                "oldWorkerExited": None,
            }
            slot = worker._slot
            if slot is None or not slot.process.is_alive():
                raise ProbeFailure("The warmed ProcessWorker slot is unavailable before the maximum-image request.")
            old_process = slot.process
            result["singleRequest"]["workerPidBeforeRequest"] = old_process.pid

            single_timeout = operation_budget_seconds - (time.monotonic() - probe_started)
            if single_timeout <= 0:
                raise ProbeFailure("The operation budget expired before the maximum-image inference.")
            single_started = time.monotonic()
            await asyncio.wait_for(
                _record_request(
                    scheduler,
                    "maximum-image-01",
                    image_bytes,
                    result["requests"],
                    result["counts"],
                ),
                timeout=single_timeout,
            )
            result["singleRequest"]["durationSeconds"] = round(time.monotonic() - single_started, 6)
            request_record = next(
                (record for record in result["requests"] if record["id"] == "maximum-image-01"),
                None,
            )
            if request_record is None:
                raise ProbeFailure("The maximum-image request did not produce a result record.")
            result["singleRequest"]["status"] = request_record["status"]
            result["singleRequest"]["errorType"] = request_record.get("errorType")

            if request_record["status"] == "success":
                result["status"] = "success"
            elif request_record["status"] == "timeout" and request_record.get("errorType") == "InferenceTimeout":
                remaining = operation_budget_seconds - (time.monotonic() - probe_started)
                if remaining <= 0:
                    raise ProbeFailure("No deadline remains to verify timeout process reclamation.")
                exit_code = await _wait_for_process_exit(old_process, remaining)
                result["singleRequest"]["oldWorkerExited"] = True
                result["singleRequest"]["oldWorkerExitCode"] = exit_code
                result["outcomeClassification"] = "VERIFIED_BOUNDARY"
                result["status"] = "timeout_boundary"
            else:
                raise ProbeFailure(
                    "Maximum-image request did not succeed or raise the expected InferenceTimeout; "
                    f"observed {request_record['status']} ({request_record.get('errorType')})."
                )
        else:
            remaining = operation_budget_seconds - (time.monotonic() - probe_started)
            if remaining <= 0:
                raise ProbeFailure("The operation budget expired before the queue occupancy probe.")
            queue_probe_result: dict = {}
            result["controlledQueueOccupancyProbe"] = queue_probe_result
            await asyncio.wait_for(
                _measure_capacity_probe(
                    scheduler,
                    observer,
                    image_bytes,
                    result["requests"],
                    result["counts"],
                    queue_probe_result,
                    min(5.0, remaining),
                ),
                timeout=remaining,
            )

            expected_counts = {
                "success": 9,
                "capacityRejected": 1,
                "timeout": 0,
                "failure": 0,
                "cancelled": 0,
            }
            if result["counts"] != expected_counts:
                raise ProbeFailure("Observed request counts differ from the declared queue workload.")
            if result["controlledQueueOccupancyProbe"]["maxQueuedRequests"] != 8:
                raise ProbeFailure("The scheduler did not reach the expected eight waiting requests.")
            result["status"] = "success"
    finally:
        if scheduler is not None:
            try:
                await asyncio.wait_for(scheduler.close(), timeout=min(CLEANUP_TIMEOUT_SECONDS, cleanup_reserve_seconds))
            except BaseException as error:
                close_error = error
        elif worker is not None:
            try:
                await asyncio.wait_for(worker.close(), timeout=min(CLEANUP_TIMEOUT_SECONDS, cleanup_reserve_seconds))
            except BaseException as error:
                close_error = error

        sampler.stop()
        try:
            await asyncio.wait_for(sampler_task, timeout=1.0)
        except BaseException as error:
            if close_error is None:
                close_error = error
        result["memory"] = sampler.as_dict()
        result["durationSeconds"] = round(time.monotonic() - probe_started, 6)
        if close_error is not None:
            result["cleanupError"] = f"{type(close_error).__name__}: {close_error}"
            result["status"] = "failed"


def _max_seconds(value: str) -> float:
    try:
        seconds = float(value)
    except ValueError as error:
        raise argparse.ArgumentTypeError("max seconds must be a number") from error
    if not 10 <= seconds <= MAX_PROBE_SECONDS:
        raise argparse.ArgumentTypeError("max seconds must be between 10 and 120")
    return seconds


def _parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", required=True, type=Path, help="UTF-8 JSON result path")
    parser.add_argument(
        "--scenario",
        choices=tuple(SCENARIO_IMAGES),
        default="queue",
        help=(
            "maximum-image: one 5000x4000 request, recording success or a verified timeout boundary; "
            "queue: controlled 1+8 queue and tenth-request rejection with a 1000x1000 PNG (default)"
        ),
    )
    parser.add_argument(
        "--max-seconds",
        type=_max_seconds,
        default=MAX_PROBE_SECONDS,
        help="hard phase limit from 10 to 120 seconds (default: 120)",
    )
    return parser.parse_args(argv)


def _write_result(path: Path, result: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(result, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


async def _run(args: argparse.Namespace) -> dict:
    result = _empty_result(args.max_seconds, args.scenario)
    operation_budget = args.max_seconds - CLEANUP_RESERVE_SECONDS
    if operation_budget <= 0:
        result["error"] = "The configured hard limit leaves no operation budget after cleanup reserve."
        return result

    try:
        await asyncio.wait_for(
            _execute_probe(result, args.scenario, operation_budget, args.max_seconds - operation_budget),
            timeout=args.max_seconds,
        )
    except asyncio.TimeoutError:
        result["counts"]["timeout"] += 1
        result["error"] = f"Probe exceeded its {args.max_seconds:g}-second hard limit."
    except BaseException as error:
        result["error"] = f"{type(error).__name__}: {error}"
    if result["status"] not in {"success", "timeout_boundary"} and result["error"] is None:
        result["error"] = "Probe did not complete all declared requests successfully."
    return result


def main(argv: list[str] | None = None) -> int:
    args = _parse_args(argv)
    result = asyncio.run(_run(args))
    try:
        _write_result(args.output, result)
    except OSError as error:
        print(f"Could not write resource probe JSON: {error}", file=sys.stderr)
        return 2
    print(f"Resource probe {result['status']}; JSON: {args.output}")
    if result["status"] not in {"success", "timeout_boundary"}:
        print(result.get("error") or result.get("cleanupError") or "probe failed", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
