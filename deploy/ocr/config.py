"""Configuration for the private OCR host."""

from __future__ import annotations

import math
import os
from dataclasses import dataclass
from pathlib import Path


def _int_env(name: str, default: int, allow_zero: bool = False) -> int:
    raw = os.environ.get(name)
    if raw is None:
        return default
    try:
        value = int(raw)
    except ValueError as error:
        raise ValueError(f"{name} must be an integer.") from error
    if value < 0 or (value == 0 and not allow_zero):
        qualifier = "non-negative" if allow_zero else "greater than zero"
        raise ValueError(f"{name} must be {qualifier}.")
    return value


def _float_env(name: str, default: float) -> float:
    raw = os.environ.get(name)
    if raw is None:
        return default
    try:
        value = float(raw)
    except ValueError as error:
        raise ValueError(f"{name} must be a number.") from error
    if not math.isfinite(value) or value <= 0:
        raise ValueError(f"{name} must be greater than zero.")
    return value


@dataclass(frozen=True)
class Settings:
    host: str
    port: int
    models_dir: Path
    max_body_bytes: int
    max_image_bytes: int
    max_pixels: int
    max_dimension: int
    max_waiting: int
    inference_timeout_seconds: float
    startup_timeout_seconds: float
    worker_join_timeout_seconds: float
    cpu_threads: int
    body_timeout_seconds: float = 30.0

    @classmethod
    def from_env(cls) -> "Settings":
        settings = cls(
            host=os.environ.get("OCR_HOST", "127.0.0.1"),
            port=_int_env("OCR_PORT", 8080),
            models_dir=Path(os.environ.get("OCR_MODELS_DIR", "models")).expanduser(),
            max_body_bytes=_int_env("OCR_MAX_BODY_BYTES", 28 * 1024 * 1024),
            max_image_bytes=_int_env("OCR_MAX_IMAGE_BYTES", 20 * 1024 * 1024),
            max_pixels=_int_env("OCR_MAX_PIXELS", 20_000_000),
            max_dimension=_int_env("OCR_MAX_DIMENSION", 8_192),
            max_waiting=_int_env("OCR_MAX_WAITING", 8, allow_zero=True),
            inference_timeout_seconds=30.0,
            startup_timeout_seconds=_float_env("OCR_STARTUP_TIMEOUT_SECONDS", 180.0),
            worker_join_timeout_seconds=_float_env("OCR_WORKER_JOIN_TIMEOUT_SECONDS", 5.0),
            cpu_threads=_int_env("OCR_CPU_THREADS", 2),
            body_timeout_seconds=_float_env("OCR_BODY_TIMEOUT_SECONDS", 30.0),
        )
        settings.validate()
        return settings

    def validate(self) -> None:
        if not self.host.strip():
            raise ValueError("OCR_HOST must not be empty.")
        if not 1 <= self.port <= 65535:
            raise ValueError("OCR_PORT must be between 1 and 65535.")
        if self.max_waiting > 8:
            raise ValueError("OCR_MAX_WAITING cannot exceed 8.")
        base64_body_minimum = 4 * ((self.max_image_bytes + 2) // 3) + 4096
        if self.max_body_bytes < base64_body_minimum:
            raise ValueError("OCR_MAX_BODY_BYTES must leave room for base64 and JSON overhead.")
        if self.max_dimension > self.max_pixels:
            raise ValueError("OCR_MAX_DIMENSION cannot exceed OCR_MAX_PIXELS.")
