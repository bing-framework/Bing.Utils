"""HTTP host exposing the single-image OCR protocol."""

from __future__ import annotations

import asyncio
import logging
from contextlib import asynccontextmanager

from starlette.applications import Starlette
from starlette.requests import ClientDisconnect as StarletteClientDisconnect
from starlette.requests import Request
from starlette.responses import JSONResponse
from starlette.routing import Route

from config import Settings
from inputs import InputError, parse_request
from scheduler import (
    BusyError,
    ClientDisconnected,
    InferenceScheduler,
    InferenceTimeout,
    ServiceUnavailable,
)
from worker import ProcessWorker


logger = logging.getLogger("bing.ocr")


class _BodyAdmission:
    """Bound concurrent body parsing to the active plus waiting request budget."""

    def __init__(self, capacity: int):
        self._capacity = capacity
        self._count = 0
        self._lock = asyncio.Lock()

    async def try_acquire(self) -> bool:
        async with self._lock:
            if self._count >= self._capacity:
                return False
            self._count += 1
            return True

    async def release(self) -> None:
        async with self._lock:
            self._count -= 1


def _consume_startup(task: asyncio.Task) -> None:
    try:
        task.result()
    except Exception:
        logger.warning("OCR worker startup failed; readiness remains unavailable.")


@asynccontextmanager
async def lifespan(app: Starlette):
    settings = Settings.from_env()
    worker = ProcessWorker(settings)
    scheduler = InferenceScheduler(
        worker,
        max_waiting=settings.max_waiting,
        inference_timeout_seconds=settings.inference_timeout_seconds,
    )
    app.state.settings = settings
    app.state.scheduler = scheduler
    app.state.body_admission = _BodyAdmission(1 + settings.max_waiting)
    startup = asyncio.create_task(worker.ensure_ready())
    startup.add_done_callback(_consume_startup)
    try:
        yield
    finally:
        await scheduler.close()


async def health_live(request: Request) -> JSONResponse:
    return JSONResponse({"live": True})


async def health_ready(request: Request) -> JSONResponse:
    scheduler: InferenceScheduler = request.app.state.scheduler
    profile_id = scheduler.model_profile_id
    if profile_id is None:
        return JSONResponse({"ready": False}, status_code=503)
    return JSONResponse({"ready": True, "modelProfileId": profile_id})


async def ocr(request: Request) -> JSONResponse:
    settings: Settings = request.app.state.settings
    scheduler: InferenceScheduler = request.app.state.scheduler
    admission: _BodyAdmission = request.app.state.body_admission
    if not await admission.try_acquire():
        return _error(429, "OCR capacity is full.")
    try:
        if request.headers.get("content-type", "").split(";", 1)[0].strip().lower() != "application/json":
            raise InputError("Content-Type must be application/json.")
        # 绝对期限覆盖停滞和持续滴流；推理期限继续独立保护常驻子进程。
        try:
            async with asyncio.timeout(settings.body_timeout_seconds):
                body = await _read_bounded_body(request, settings.max_body_bytes)
        except TimeoutError:
            return _error(408, "OCR request body timed out.")
        parsed = parse_request(body, settings)
        result = await scheduler.submit(parsed.image_bytes, 0.0, request.is_disconnected)
        profile_id = scheduler.model_profile_id
        if profile_id is None:
            raise ServiceUnavailable("OCR worker is unavailable.")
        return JSONResponse(
            {
                "errorCode": 0,
                "errorMsg": "Success",
                "modelProfileId": profile_id,
                "result": {"ocrResults": [{"prunedResult": result}]},
            }
        )
    except InputError as error:
        return _error(400, str(error))
    except BusyError:
        return _error(429, "OCR capacity is full.")
    except InferenceTimeout:
        return _error(504, "OCR inference timed out.")
    except (ClientDisconnected, StarletteClientDisconnect):
        return JSONResponse({"errorCode": 499, "errorMsg": "Client disconnected."}, status_code=499)
    except (ServiceUnavailable, RuntimeError):
        return _error(503, "OCR service is unavailable.")
    finally:
        await admission.release()


async def _read_bounded_body(request: Request, limit: int) -> bytes:
    content_length = request.headers.get("content-length")
    if content_length is not None:
        try:
            declared_length = int(content_length)
        except ValueError as error:
            raise InputError("Content-Length is invalid.") from error
        if declared_length < 0 or declared_length > limit:
            raise InputError("Request body exceeds the configured limit.")

    chunks = bytearray()
    async for chunk in request.stream():
        if len(chunks) + len(chunk) > limit:
            raise InputError("Request body exceeds the configured limit.")
        chunks.extend(chunk)
    return bytes(chunks)


def _error(status: int, message: str) -> JSONResponse:
    return JSONResponse({"errorCode": status, "errorMsg": message}, status_code=status)


app = Starlette(
    routes=[
        Route("/health/live", health_live, methods=["GET"]),
        Route("/health/ready", health_ready, methods=["GET"]),
        Route("/ocr", ocr, methods=["POST"]),
    ],
    lifespan=lifespan,
)


def main() -> None:
    import uvicorn

    settings = Settings.from_env()
    uvicorn.run(app, host=settings.host, port=settings.port, access_log=False, log_level="warning")


if __name__ == "__main__":
    main()
