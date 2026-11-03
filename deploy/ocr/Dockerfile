FROM ghcr.io/astral-sh/uv:0.11.1 AS uv
FROM python:3.11-slim

ENV PYTHONDONTWRITEBYTECODE=1 \
    PYTHONUNBUFFERED=1 \
    PIP_DISABLE_PIP_VERSION_CHECK=1

RUN apt-get update \
    && apt-get install -y --no-install-recommends libgl1 libglib2.0-0 libgomp1 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=uv /uv /usr/local/bin/uv
WORKDIR /app
COPY requirements-linux.lock ./requirements-linux.lock
RUN --mount=type=cache,target=/root/.cache/uv uv pip sync --system --require-hashes requirements-linux.lock

COPY config.py inputs.py manifest.py scheduler.py server.py smoke.py worker.py resource_probe.py verify_offline.py ./
RUN useradd --system --uid 10001 --create-home ocr \
    && mkdir -p /models \
    && chown 10001:10001 /models

ENV OCR_HOST=0.0.0.0 \
    OCR_PORT=8080 \
    OCR_MODELS_DIR=/models
USER 10001:10001
EXPOSE 8080
ENTRYPOINT ["python", "server.py"]
