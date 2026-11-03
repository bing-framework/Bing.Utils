"""Single spawned PaddleOCR process and its request protocol."""

from __future__ import annotations

import asyncio
import importlib.metadata
import io
import json
import logging
import math
import multiprocessing
import os
import threading
import time
import uuid
from dataclasses import dataclass, field
from pathlib import Path

from config import Settings
from manifest import ARTIFACTS, EXPECTED_RUNTIME, ManifestError, create_profile_id, load_manifest


class WorkerUnavailable(RuntimeError):
    """Raised when the OCR process is not ready or exits unexpectedly."""


class WorkerResponseError(RuntimeError):
    """Raised when the OCR process cannot handle a request."""


@dataclass
class _WorkerSlot:
    process: multiprocessing.Process
    connection: object
    profile_id: str
    io_lock: threading.Lock = field(default_factory=threading.Lock, repr=False)


@dataclass
class _StartingWorker:
    process: multiprocessing.Process
    connection: object


class ProcessWorker:
    """Own one spawned OCR worker and replace it after timeout or failure."""

    def __init__(self, settings: Settings):
        self._settings = settings
        self._context = multiprocessing.get_context("spawn")
        self._slot: _WorkerSlot | None = None
        self._starting: _StartingWorker | None = None
        self._state_lock = threading.Lock()
        self._async_lock = asyncio.Lock()
        self._closed = False

    @property
    def ready(self) -> bool:
        with self._state_lock:
            slot = self._slot
            return slot is not None and slot.process.is_alive()

    @property
    def profile_id(self) -> str | None:
        with self._state_lock:
            return self._slot.profile_id if self._slot and self._slot.process.is_alive() else None

    async def ensure_ready(self) -> str:
        async with self._async_lock:
            return await asyncio.to_thread(self._ensure_ready_sync)

    async def infer(self, image_bytes: bytes, text_rec_score_thresh: float) -> dict:
        await self.ensure_ready()
        return await asyncio.to_thread(self._infer_sync, image_bytes, text_rec_score_thresh)

    async def recover(self) -> None:
        async with self._async_lock:
            await asyncio.to_thread(self._recover_sync)

    async def close(self) -> None:
        with self._state_lock:
            self._closed = True
            starting = self._starting
        if starting is not None and starting.process.is_alive():
            try:
                starting.process.terminate()
            except OSError:
                pass
        async with self._async_lock:
            await asyncio.to_thread(self._close_sync)

    def _ensure_ready_sync(self) -> str:
        with self._state_lock:
            if self._closed:
                raise WorkerUnavailable("OCR worker is closed.")
            slot = self._slot
            if slot is not None and slot.process.is_alive():
                return slot.profile_id
        if slot is not None:
            self._retire(slot, terminate=True)

        parent, child = self._context.Pipe(duplex=True)
        process = self._context.Process(
            target=_worker_entry,
            args=(child, self._settings),
            name="bing-ocr-paddle-worker",
        )
        try:
            process.start()
        except BaseException as error:
            parent.close()
            child.close()
            raise WorkerUnavailable("OCR worker could not start.") from error
        child.close()

        with self._state_lock:
            closed = self._closed
            if not closed:
                self._starting = _StartingWorker(process, parent)
        if closed:
            if process.is_alive():
                process.terminate()
            process.join(self._settings.worker_join_timeout_seconds)
            if process.is_alive():
                process.kill()
                process.join()
            parent.close()
            raise WorkerUnavailable("OCR worker is closed.")

        deadline = time.monotonic() + self._settings.startup_timeout_seconds
        try:
            while time.monotonic() < deadline:
                if parent.poll(0.1):
                    message = parent.recv()
                    if message[0] == "ready" and isinstance(message[1], str):
                        slot = _WorkerSlot(process, parent, message[1])
                        with self._state_lock:
                            closed = self._closed
                            if not closed:
                                self._slot = slot
                                self._starting = None
                        if closed:
                            raise WorkerUnavailable("OCR worker is closed.")
                        return slot.profile_id
                    raise WorkerUnavailable("OCR worker failed during model startup.")
                if not process.is_alive():
                    raise WorkerUnavailable("OCR worker exited during model startup.")
            raise WorkerUnavailable("OCR worker startup timed out.")
        except (EOFError, OSError) as error:
            raise WorkerUnavailable("OCR worker exited during model startup.") from error
        finally:
            with self._state_lock:
                is_active_slot = self._slot is not None and self._slot.process is process
                if self._starting is not None and self._starting.process is process:
                    self._starting = None
            if not is_active_slot:
                parent.close()
                if process.is_alive():
                    process.terminate()
                process.join(self._settings.worker_join_timeout_seconds)
                if process.is_alive():
                    process.kill()
                    process.join()

    def _infer_sync(self, image_bytes: bytes, text_rec_score_thresh: float) -> dict:
        with self._state_lock:
            slot = self._slot
        if slot is None or not slot.process.is_alive():
            if slot is not None:
                self._retire(slot, terminate=True)
            raise WorkerUnavailable("OCR worker is not running.")

        request_id = uuid.uuid4().hex
        try:
            # 取消 asyncio.to_thread 不会终止底层读操作。回收线程需等此区段退出后才能关闭管道。
            with slot.io_lock:
                slot.connection.send(("infer", request_id, image_bytes, text_rec_score_thresh))
                while True:
                    if slot.connection.poll(0.1):
                        message = slot.connection.recv()
                        if not isinstance(message, tuple) or len(message) < 2 or message[1] != request_id:
                            raise WorkerResponseError("OCR worker returned an invalid response.")
                        if message[0] == "result" and len(message) == 3 and isinstance(message[2], dict):
                            return message[2]
                        raise WorkerResponseError("OCR worker failed to recognize the image.")
                    if not slot.process.is_alive():
                        raise WorkerUnavailable("OCR worker exited during inference.")
        except (EOFError, OSError, BrokenPipeError) as error:
            self._retire(slot, terminate=True)
            raise WorkerUnavailable("OCR worker exited during inference.") from error
        except WorkerResponseError:
            if not slot.process.is_alive():
                self._retire(slot, terminate=True)
                raise WorkerUnavailable("OCR worker exited during inference.")
            raise

    def _recover_sync(self) -> None:
        with self._state_lock:
            slot = self._slot
            self._slot = None
            closed = self._closed
        if slot is not None:
            self._retire(slot, terminate=True)
        if not closed:
            self._ensure_ready_sync()

    def _close_sync(self) -> None:
        with self._state_lock:
            slot = self._slot
            self._slot = None
            self._starting = None
        if slot is not None:
            self._retire(slot, terminate=True)

    def _retire(self, slot: _WorkerSlot, terminate: bool) -> None:
        with self._state_lock:
            if self._slot is slot:
                self._slot = None
        # 先结束写端，使未完成读取收到 EOF；禁止直接关闭仍有 Windows overlapped I/O 的读端。
        if slot.process.is_alive() and terminate:
            slot.process.terminate()
        slot.process.join(self._settings.worker_join_timeout_seconds)
        if slot.process.is_alive():
            slot.process.kill()
            slot.process.join()
        with slot.io_lock:
            try:
                slot.connection.close()
            except OSError:
                pass


def _worker_entry(connection, settings: Settings) -> None:
    try:
        logging.getLogger("paddlex").setLevel(logging.WARNING)
        engine = _PaddleEngine(settings)
        profile_id = engine.warmup()
        connection.send(("ready", profile_id))
        while True:
            try:
                command = connection.recv()
            except EOFError:
                return
            if command == ("shutdown",):
                return
            if not isinstance(command, tuple) or len(command) != 4 or command[0] != "infer":
                continue
            _, request_id, image_bytes, score_threshold = command
            try:
                result = engine.recognize(image_bytes, score_threshold)
                connection.send(("result", request_id, result))
            except BaseException:
                connection.send(("error", request_id, "recognition_failed"))
    except BaseException as error:
        try:
            connection.send(("startup_error", type(error).__name__))
        except (BrokenPipeError, EOFError, OSError):
            pass
    finally:
        connection.close()


class _PaddleEngine:
    def __init__(self, settings: Settings):
        self._settings = settings
        self._models_dir = settings.models_dir.resolve(strict=True)
        self._manifest = load_manifest(self._models_dir)
        runtime = {
            "paddleocr": importlib.metadata.version("paddleocr"),
            "paddlex": importlib.metadata.version("paddlex"),
            "paddlepaddle": importlib.metadata.version("paddlepaddle"),
        }
        if runtime != EXPECTED_RUNTIME:
            raise ManifestError("Installed OCR package versions do not match the pinned profile.")
        self._runtime = runtime
        # Paddle 3.3.1 的 PP-OCRv6 PIR oneDNN 路径在 Windows/Linux 均出现未实现指令。
        # 固定使用已验证的 CPU 路径，配置参与模型身份计算，不在请求中自动切换。
        self._enable_mkldnn = False
        self._ocr = None

    def warmup(self) -> str:
        import numpy as np
        import paddle
        from PIL import Image
        from paddleocr import PaddleOCR

        paddle.set_device("cpu")
        paddle.base.core.set_num_threads(self._settings.cpu_threads)
        models = {name: self._models_dir / directory for name, directory in ARTIFACTS.items()}
        self._ocr = PaddleOCR(
            device="cpu",
            cpu_threads=self._settings.cpu_threads,
            enable_mkldnn=self._enable_mkldnn,
            use_doc_orientation_classify=False,
            use_doc_unwarping=False,
            use_textline_orientation=True,
            text_detection_model_name="PP-OCRv6_small_det",
            text_detection_model_dir=str(models["PP-OCRv6_small_det"]),
            text_recognition_model_name="PP-OCRv6_small_rec",
            text_recognition_model_dir=str(models["PP-OCRv6_small_rec"]),
            textline_orientation_model_name="PP-LCNet_x1_0_textline_ori",
            textline_orientation_model_dir=str(models["PP-LCNet_x1_0_textline_ori"]),
        )
        warmup_image = np.asarray(Image.new("RGB", (32, 32), "white"))[:, :, ::-1]
        self._ocr.predict(input=np.ascontiguousarray(warmup_image), text_rec_score_thresh=0.0)
        return create_profile_id(
            self._manifest,
            self._runtime,
            self._settings.cpu_threads,
            self._enable_mkldnn,
        )

    def recognize(self, image_bytes: bytes, score_threshold: float) -> dict:
        import numpy as np
        from PIL import Image

        if self._ocr is None:
            raise RuntimeError("PaddleOCR was not warmed up.")
        with Image.open(io.BytesIO(image_bytes), formats=("PNG", "JPEG", "BMP")) as image:
            image_array = np.ascontiguousarray(np.asarray(image.convert("RGB"))[:, :, ::-1])
            height, width = image_array.shape[:2]
        raw_results = list(self._ocr.predict(input=image_array, text_rec_score_thresh=score_threshold))
        data = _first_result(raw_results)
        texts = _to_list(data.get("rec_texts", []))
        scores = _nullable_scores(_to_list(data.get("rec_scores")), len(texts))
        polygons = _clockwise_polygons(_to_list(data.get("rec_polys", [])), len(texts), width, height)
        return {
            "rec_texts": texts,
            "rec_scores": scores,
            "rec_polys": polygons,
            "input_img_shape": [height, width],
        }


def _first_result(results: list) -> dict:
    if not results:
        return {}
    result = results[0]
    payload = getattr(result, "json", None)
    if callable(payload):
        payload = payload()
    if isinstance(payload, str):
        payload = json.loads(payload)
    if not isinstance(payload, dict):
        raise RuntimeError("PaddleOCR returned an unsupported result object.")
    nested = payload.get("res", payload)
    if not isinstance(nested, dict):
        raise RuntimeError("PaddleOCR returned an unsupported result payload.")
    return nested


def _to_list(value) -> list:
    if hasattr(value, "tolist"):
        value = value.tolist()
    if value is None:
        return []
    if not isinstance(value, list):
        raise RuntimeError("PaddleOCR result field is not an array.")
    return value


def _nullable_scores(scores: list, text_count: int) -> list:
    if not scores and text_count:
        return [None] * text_count
    if len(scores) != text_count:
        raise RuntimeError("PaddleOCR returned inconsistent recognition arrays.")
    normalized = []
    for score in scores:
        try:
            value = float(score)
        except (TypeError, ValueError):
            normalized.append(None)
            continue
        normalized.append(value if math.isfinite(value) and 0.0 <= value <= 1.0 else None)
    return normalized
def _clockwise_polygons(polygons: list, text_count: int, width: int, height: int) -> list:
    if len(polygons) != text_count:
        raise RuntimeError("PaddleOCR returned inconsistent recognition arrays.")
    normalized = []
    for polygon in polygons:
        if not isinstance(polygon, list) or len(polygon) != 4:
            raise RuntimeError("PaddleOCR returned a non-quadrilateral text region.")
        points = []
        for point in polygon:
            if not isinstance(point, list) or len(point) != 2:
                raise RuntimeError("PaddleOCR returned an invalid text polygon.")
            x, y = float(point[0]), float(point[1])
            if not math.isfinite(x) or not math.isfinite(y) or x < 0 or y < 0 or x > width or y > height:
                raise RuntimeError("PaddleOCR returned a text polygon outside the input image.")
            points.append([x, y])

        crosses = []
        for index in range(4):
            a = points[index]
            b = points[(index + 1) % 4]
            c = points[(index + 2) % 4]
            crosses.append((b[0] - a[0]) * (c[1] - b[1]) - (b[1] - a[1]) * (c[0] - b[0]))
        if any(value == 0 or not math.isfinite(value) for value in crosses):
            raise RuntimeError("PaddleOCR returned a degenerate text polygon.")
        if any((value > 0) != (crosses[0] > 0) for value in crosses[1:]):
            raise RuntimeError("PaddleOCR returned a non-convex text polygon.")
        if crosses[0] < 0:
            points.reverse()
        normalized.append(points)
    return normalized
