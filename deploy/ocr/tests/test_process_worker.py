"""OS-process lifecycle coverage for the OCR worker and scheduler."""

from __future__ import annotations

import asyncio
import multiprocessing
import os
import tempfile
import time
import unittest
from pathlib import Path

import worker as worker_module
from config import Settings
from scheduler import BusyError, InferenceScheduler, InferenceTimeout, ServiceUnavailable
from worker import ProcessWorker, WorkerUnavailable


HARD_TIMEOUT_SECONDS = 12
MARKER_TIMEOUT_SECONDS = 5


def _write_marker(path: Path) -> None:
    path.write_text(str(os.getpid()), encoding="utf-8")


def _controlled_worker_entry(connection, settings: Settings) -> None:
    """替代模型运行时，保留真实 spawn、Pipe 和 worker 生命周期。"""
    root = settings.models_dir
    pid = os.getpid()
    startup_hold = root / "hold-startup"
    if startup_hold.exists():
        _write_marker(root / f"startup-entered-{pid}")
        while startup_hold.exists():
            time.sleep(0.01)

    try:
        connection.send(("ready", f"controlled-profile:{pid}"))
        while True:
            try:
                command = connection.recv()
            except EOFError:
                return
            if command == ("shutdown",):
                return
            if not isinstance(command, tuple) or len(command) != 4 or command[0] != "infer":
                continue

            _, request_id, image_bytes, _ = command
            if image_bytes == b"hang":
                _write_marker(root / f"hang-entered-{pid}")
                while True:
                    time.sleep(0.01)
            if image_bytes == b"wait-for-release":
                _write_marker(root / f"wait-entered-{pid}")
                release = root / "release-inference"
                deadline = time.monotonic() + 8
                while not release.exists() and time.monotonic() < deadline:
                    time.sleep(0.01)
                _write_marker(root / f"wait-completed-{pid}")
            if image_bytes == b"crash":
                _write_marker(root / f"crash-entered-{pid}")
                os._exit(73)

            connection.send(
                (
                    "result",
                    request_id,
                    {
                        "rec_texts": [f"pid-{pid}"],
                        "rec_scores": [0.99],
                        "rec_polys": [[[0, 0], [1, 0], [1, 1], [0, 1]]],
                        "input_img_shape": [1, 1],
                    },
                )
            )
    finally:
        connection.close()


class ProcessWorkerLifecycleTests(unittest.IsolatedAsyncioTestCase):
    def setUp(self) -> None:
        self._original_entry = worker_module._worker_entry
        worker_module._worker_entry = _controlled_worker_entry
        self._temporary = tempfile.TemporaryDirectory()
        self.root = Path(self._temporary.name)
        self.baseline_child_pids = {process.pid for process in multiprocessing.active_children()}
        self.worker: ProcessWorker | None = None
        self.scheduler: InferenceScheduler | None = None
        self.processes: list[multiprocessing.Process] = []

    def tearDown(self) -> None:
        worker_module._worker_entry = self._original_entry
        self._temporary.cleanup()

    def _new_worker(self) -> ProcessWorker:
        settings = Settings(
            host="127.0.0.1",
            port=8080,
            models_dir=self.root,
            max_body_bytes=4096,
            max_image_bytes=2048,
            max_pixels=100,
            max_dimension=10,
            max_waiting=1,
            inference_timeout_seconds=1,
            startup_timeout_seconds=8,
            worker_join_timeout_seconds=0.5,
            cpu_threads=1,
        )
        self.worker = ProcessWorker(settings)
        return self.worker

    def _remember(self, process: multiprocessing.Process | None) -> None:
        if process is not None and all(existing is not process for existing in self.processes):
            self.processes.append(process)

    def _remember_worker_processes(self) -> None:
        if self.worker is None:
            return
        starting = self.worker._starting
        slot = self.worker._slot
        self._remember(starting.process if starting is not None else None)
        self._remember(slot.process if slot is not None else None)

    async def _wait_for_path(self, path: Path, timeout: float = MARKER_TIMEOUT_SECONDS) -> None:
        deadline = time.monotonic() + timeout
        while not path.exists():
            if time.monotonic() >= deadline:
                self.fail(f"Timed out waiting for process marker: {path.name}")
            await asyncio.sleep(0.01)

    async def _wait_for_startup_marker(self) -> Path:
        deadline = time.monotonic() + MARKER_TIMEOUT_SECONDS
        while time.monotonic() < deadline:
            markers = list(self.root.glob("startup-entered-*"))
            if markers:
                return markers[0]
            await asyncio.sleep(0.01)
        self.fail("Timed out waiting for startup marker")

    async def _assert_stopped(self, process: multiprocessing.Process) -> None:
        deadline = time.monotonic() + MARKER_TIMEOUT_SECONDS
        while process.is_alive() and time.monotonic() < deadline:
            await asyncio.sleep(0.01)
        process.join(timeout=0)
        self.assertFalse(process.is_alive(), f"worker PID {process.pid} is still running")

    async def _cleanup(self) -> None:
        self._remember_worker_processes()
        close_error = None
        try:
            if self.scheduler is not None:
                await asyncio.wait_for(self.scheduler.close(), HARD_TIMEOUT_SECONDS)
            elif self.worker is not None:
                await asyncio.wait_for(self.worker.close(), HARD_TIMEOUT_SECONDS)
        except BaseException as error:
            close_error = error

        self._remember_worker_processes()
        still_alive = [process for process in self.processes if process.is_alive()]
        for process in still_alive:
            process.terminate()
        for process in still_alive:
            process.join(timeout=MARKER_TIMEOUT_SECONDS)

        self.assertFalse(still_alive, "worker close left a spawned process alive")
        active_child_pids = {process.pid for process in multiprocessing.active_children()}
        self.assertFalse(
            active_child_pids - self.baseline_child_pids,
            "test left an unjoined multiprocessing child",
        )
        if close_error is not None:
            raise close_error

    async def test_timeout_terminates_hung_pid_and_next_request_uses_replacement(self):
        worker = self._new_worker()
        try:
            old_profile = await asyncio.wait_for(worker.ensure_ready(), HARD_TIMEOUT_SECONDS)
            old_process = worker._slot.process
            self._remember(old_process)
            self.scheduler = InferenceScheduler(worker, max_waiting=0, inference_timeout_seconds=0.25)

            request = asyncio.create_task(self.scheduler.submit(b"hang", 0, _connected))
            await self._wait_for_path(self.root / f"hang-entered-{old_process.pid}")
            execution = self.scheduler._active.task
            with self.assertRaises(InferenceTimeout):
                await asyncio.wait_for(request, HARD_TIMEOUT_SECONDS)
            await asyncio.wait_for(execution, HARD_TIMEOUT_SECONDS)

            await self._assert_stopped(old_process)
            self.assertNotEqual(worker.profile_id, old_profile)
            replacement = worker._slot.process
            self._remember(replacement)
            self.assertTrue(replacement.is_alive())
            result = await asyncio.wait_for(self.scheduler.submit(b"success", 0, _connected), HARD_TIMEOUT_SECONDS)
            self.assertEqual(result["rec_texts"], [f"pid-{replacement.pid}"])
        finally:
            await self._cleanup()

    async def test_crashed_child_is_replaced_for_the_next_request(self):
        worker = self._new_worker()
        try:
            old_profile = await asyncio.wait_for(worker.ensure_ready(), HARD_TIMEOUT_SECONDS)
            old_process = worker._slot.process
            self._remember(old_process)
            self.scheduler = InferenceScheduler(worker, max_waiting=0, inference_timeout_seconds=3)

            request = asyncio.create_task(self.scheduler.submit(b"crash", 0, _connected))
            await self._wait_for_path(self.root / f"crash-entered-{old_process.pid}")
            with self.assertRaises(ServiceUnavailable):
                await asyncio.wait_for(request, HARD_TIMEOUT_SECONDS)
            # Linux 子进程可在 marker 可见前已完成回收；此时 active 为 None 是正确状态。
            active = self.scheduler._active
            if active is not None:
                await asyncio.wait_for(active.task, HARD_TIMEOUT_SECONDS)
            await self._assert_stopped(old_process)

            result = await asyncio.wait_for(self.scheduler.submit(b"success", 0, _connected), HARD_TIMEOUT_SECONDS)
            replacement = worker._slot.process
            self._remember(replacement)
            self.assertNotEqual(worker.profile_id, old_profile)
            self.assertEqual(result["rec_texts"], [f"pid-{replacement.pid}"])
            self.assertTrue(replacement.is_alive())
        finally:
            await self._cleanup()

    async def test_cancelled_caller_keeps_active_capacity_until_inference_finishes(self):
        worker = self._new_worker()
        try:
            await asyncio.wait_for(worker.ensure_ready(), HARD_TIMEOUT_SECONDS)
            process = worker._slot.process
            self._remember(process)
            self.scheduler = InferenceScheduler(worker, max_waiting=0, inference_timeout_seconds=5)

            request = asyncio.create_task(self.scheduler.submit(b"wait-for-release", 0, _connected))
            await self._wait_for_path(self.root / f"wait-entered-{process.pid}")
            execution = self.scheduler._active.task
            request.cancel()
            with self.assertRaises(asyncio.CancelledError):
                await asyncio.wait_for(request, HARD_TIMEOUT_SECONDS)

            self.assertTrue(process.is_alive())
            with self.assertRaises(BusyError):
                await asyncio.wait_for(self.scheduler.submit(b"too-soon", 0, _connected), HARD_TIMEOUT_SECONDS)
            self.assertTrue(process.is_alive())

            (self.root / "release-inference").write_text("release", encoding="utf-8")
            await asyncio.wait_for(execution, HARD_TIMEOUT_SECONDS)
            await self._wait_for_path(self.root / f"wait-completed-{process.pid}")
            result = await asyncio.wait_for(self.scheduler.submit(b"success", 0, _connected), HARD_TIMEOUT_SECONDS)
            self.assertEqual(result["rec_texts"], [f"pid-{process.pid}"])
            self.assertTrue(process.is_alive())
        finally:
            await self._cleanup()

    async def test_close_terminates_active_inference_process(self):
        worker = self._new_worker()
        try:
            await asyncio.wait_for(worker.ensure_ready(), HARD_TIMEOUT_SECONDS)
            process = worker._slot.process
            self._remember(process)
            self.scheduler = InferenceScheduler(worker, max_waiting=0, inference_timeout_seconds=5)

            request = asyncio.create_task(self.scheduler.submit(b"hang", 0, _connected))
            await self._wait_for_path(self.root / f"hang-entered-{process.pid}")
            await asyncio.wait_for(self.scheduler.close(), HARD_TIMEOUT_SECONDS)
            with self.assertRaises(ServiceUnavailable):
                await asyncio.wait_for(request, HARD_TIMEOUT_SECONDS)
            await self._assert_stopped(process)
        finally:
            await self._cleanup()

    async def test_close_terminates_worker_still_starting(self):
        worker = self._new_worker()
        try:
            (self.root / "hold-startup").write_text("hold", encoding="utf-8")
            startup = asyncio.create_task(worker.ensure_ready())
            await self._wait_for_startup_marker()
            process = worker._starting.process
            self._remember(process)

            await asyncio.wait_for(worker.close(), HARD_TIMEOUT_SECONDS)
            with self.assertRaises(WorkerUnavailable):
                await asyncio.wait_for(startup, HARD_TIMEOUT_SECONDS)
            await self._assert_stopped(process)
        finally:
            await self._cleanup()


async def _connected() -> bool:
    return False


if __name__ == "__main__":
    unittest.main()
