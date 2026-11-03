"""Bounded FIFO admission and cancellation-aware OCR scheduling."""

from __future__ import annotations

import asyncio
from collections import deque
from dataclasses import dataclass, field
from typing import Awaitable, Callable

from worker import WorkerUnavailable


class BusyError(RuntimeError):
    """Raised when the active slot and bounded waiting queue are full."""


class InferenceTimeout(RuntimeError):
    """Raised when active inference exceeds its hard time limit."""


class ServiceUnavailable(RuntimeError):
    """Raised when the worker cannot be started or has shut down."""


class ClientDisconnected(RuntimeError):
    """Raised when a queued request disconnects before receiving the slot."""


@dataclass
class _Ticket:
    image_bytes: bytes
    score_threshold: float
    result: asyncio.Future
    started: asyncio.Event = field(default_factory=asyncio.Event)
    task: asyncio.Task | None = None


class InferenceScheduler:
    """Keep at most one inference active and a bounded number waiting."""

    def __init__(self, worker, max_waiting: int = 8, inference_timeout_seconds: float = 30.0):
        if not 0 <= max_waiting <= 8:
            raise ValueError("max_waiting must be between zero and eight.")
        if inference_timeout_seconds <= 0:
            raise ValueError("inference_timeout_seconds must be greater than zero.")
        self._worker = worker
        self._max_waiting = max_waiting
        self._inference_timeout_seconds = inference_timeout_seconds
        self._guard = asyncio.Lock()
        self._waiting: deque[_Ticket] = deque()
        self._active: _Ticket | None = None
        self._closed = False

    @property
    def ready(self) -> bool:
        return self._worker.ready and not self._closed

    @property
    def model_profile_id(self) -> str | None:
        return self._worker.profile_id if self.ready else None

    async def submit(
        self,
        image_bytes: bytes,
        score_threshold: float,
        is_disconnected: Callable[[], Awaitable[bool]],
    ) -> dict:
        loop = asyncio.get_running_loop()
        ticket = _Ticket(image_bytes, score_threshold, loop.create_future())
        ticket.result.add_done_callback(_consume_future_exception)
        async with self._guard:
            if self._closed:
                raise ServiceUnavailable("OCR host is shutting down.")
            if self._active is None:
                self._activate(ticket)
            elif len(self._waiting) >= self._max_waiting:
                raise BusyError("OCR capacity is full.")
            else:
                self._waiting.append(ticket)

        try:
            while not ticket.started.is_set():
                if ticket.result.done():
                    return await asyncio.shield(ticket.result)
                if await is_disconnected():
                    if await self._remove_waiting(ticket):
                        raise ClientDisconnected("Queued OCR request disconnected.")
                    if ticket.started.is_set():
                        break
                try:
                    await asyncio.wait_for(ticket.started.wait(), timeout=0.1)
                except asyncio.TimeoutError:
                    continue
            return await asyncio.shield(ticket.result)
        except asyncio.CancelledError:
            await self._remove_waiting(ticket)
            raise

    async def close(self) -> None:
        async with self._guard:
            if self._closed:
                return
            self._closed = True
            waiting = list(self._waiting)
            self._waiting.clear()
            for ticket in waiting:
                if not ticket.result.done():
                    ticket.result.set_exception(ServiceUnavailable("OCR host is shutting down."))
            active = self._active
            active_task = active.task if active is not None else None

        await self._worker.close()
        if active_task is not None and not active_task.done():
            await asyncio.gather(active_task, return_exceptions=True)

    async def _remove_waiting(self, ticket: _Ticket) -> bool:
        async with self._guard:
            if ticket.started.is_set():
                return False
            try:
                self._waiting.remove(ticket)
            except ValueError:
                return False
            if not ticket.result.done():
                ticket.result.cancel()
            return True

    def _activate(self, ticket: _Ticket) -> None:
        self._active = ticket
        ticket.started.set()
        ticket.task = asyncio.create_task(self._execute(ticket))

    async def _execute(self, ticket: _Ticket) -> None:
        try:
            await self._worker.ensure_ready()
            result = await asyncio.wait_for(
                self._worker.infer(ticket.image_bytes, ticket.score_threshold),
                timeout=self._inference_timeout_seconds,
            )
            if not ticket.result.done():
                ticket.result.set_result(result)
        except asyncio.TimeoutError:
            if not ticket.result.done():
                ticket.result.set_exception(InferenceTimeout("OCR inference exceeded its time limit."))
            try:
                await self._worker.recover()
            except Exception:
                pass
        except (WorkerUnavailable, ServiceUnavailable) as error:
            if not ticket.result.done():
                ticket.result.set_exception(ServiceUnavailable("OCR worker is unavailable."))
        except Exception as error:
            if not ticket.result.done():
                ticket.result.set_exception(ServiceUnavailable("OCR worker failed."))
        finally:
            await self._finish(ticket)

    async def _finish(self, ticket: _Ticket) -> None:
        async with self._guard:
            if self._active is not ticket:
                return
            if self._closed or not self._waiting:
                self._active = None
                return
            next_ticket = self._waiting.popleft()
            self._activate(next_ticket)


def _consume_future_exception(future: asyncio.Future) -> None:
    if future.cancelled():
        return
    try:
        future.exception()
    except asyncio.CancelledError:
        pass
