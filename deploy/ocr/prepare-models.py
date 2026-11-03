"""Download and validate the pinned PaddleX OCR model archives."""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import shutil
import tarfile
import tempfile
import urllib.request
import uuid
from pathlib import Path, PurePosixPath

from manifest import ARTIFACTS, MODEL_ARCHIVE_SHA256, MODEL_SOURCES, SCHEMA_VERSION


MAX_ARCHIVE_BYTES = 512 * 1024 * 1024
MAX_EXTRACTED_BYTES = 2 * 1024 * 1024 * 1024
DOWNLOAD_TIMEOUT_SECONDS = 60
REQUIRED_FILES = {"inference.json", "inference.pdiparams"}


class ModelPreparationError(RuntimeError):
    """Raised when a pinned model cannot be downloaded or safely prepared."""


class _OfficialRedirects(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, request, file_pointer, code, message, headers, new_url):
        if not _is_approved_url(new_url):
            raise ModelPreparationError("Model download redirected outside the approved PaddleX host.")
        return super().redirect_request(request, file_pointer, code, message, headers, new_url)


def _is_approved_url(value: str) -> bool:
    return value in MODEL_SOURCES.values()


def _download(url: str, destination: Path) -> str:
    if not _is_approved_url(url):
        raise ModelPreparationError("Model URL is not approved.")
    request = urllib.request.Request(url, headers={"User-Agent": "Bing.Utils-OCR-model-preparer/1"})
    opener = urllib.request.build_opener(_OfficialRedirects())
    digest = hashlib.sha256()
    written = 0
    try:
        with opener.open(request, timeout=DOWNLOAD_TIMEOUT_SECONDS) as response:
            if not _is_approved_url(response.geturl()):
                raise ModelPreparationError("Model download resolved outside the approved PaddleX host.")
            content_length = response.headers.get("Content-Length")
            if content_length is not None and int(content_length) > MAX_ARCHIVE_BYTES:
                raise ModelPreparationError("Model archive exceeds the configured download limit.")
            with destination.open("xb") as output:
                while True:
                    chunk = response.read(1024 * 1024)
                    if not chunk:
                        break
                    written += len(chunk)
                    if written > MAX_ARCHIVE_BYTES:
                        raise ModelPreparationError("Model archive exceeds the configured download limit.")
                    digest.update(chunk)
                    output.write(chunk)
    except (OSError, ValueError) as error:
        raise ModelPreparationError("A pinned PaddleX model archive could not be downloaded.") from error
    return digest.hexdigest()


def _safe_member_path(name: str) -> PurePosixPath:
    if "\\" in name or "\x00" in name:
        raise ModelPreparationError("Model archive contains an invalid path.")
    path = PurePosixPath(name)
    if path.is_absolute() or any(part in ("", ".", "..") for part in path.parts):
        raise ModelPreparationError("Model archive contains a path outside its extraction directory.")
    return path


def _extract_archive(archive: Path, extraction: Path) -> Path:
    extracted_bytes = 0
    seen = set()
    try:
        with tarfile.open(archive, mode="r:*") as source:
            for member in source:
                relative = _safe_member_path(member.name)
                key = relative.as_posix()
                if key in seen:
                    raise ModelPreparationError("Model archive contains duplicate paths.")
                seen.add(key)
                destination = extraction.joinpath(*relative.parts)
                if member.isdir():
                    destination.mkdir(parents=True, exist_ok=True)
                    continue
                if not member.isfile():
                    raise ModelPreparationError("Model archive contains a link or special file.")
                extracted_bytes += member.size
                if member.size < 0 or extracted_bytes > MAX_EXTRACTED_BYTES:
                    raise ModelPreparationError("Expanded model archive exceeds the configured limit.")
                destination.parent.mkdir(parents=True, exist_ok=True)
                source_file = source.extractfile(member)
                if source_file is None:
                    raise ModelPreparationError("Model archive contains an unreadable file.")
                remaining = member.size
                with source_file, destination.open("xb") as output:
                    while remaining:
                        chunk = source_file.read(min(1024 * 1024, remaining))
                        if not chunk:
                            raise ModelPreparationError("Model archive contains a truncated file.")
                        output.write(chunk)
                        remaining -= len(chunk)
    except (tarfile.TarError, OSError) as error:
        raise ModelPreparationError("A model archive is invalid or could not be extracted.") from error

    candidates = [extraction]
    candidates.extend(path for path in extraction.rglob("*") if path.is_dir())
    valid = [path for path in candidates if all((path / name).is_file() for name in REQUIRED_FILES)]
    if len(valid) != 1:
        raise ModelPreparationError("Model archive does not contain one unambiguous inference model directory.")
    return valid[0]


def _file_records(model_dir: Path) -> list[dict[str, str]]:
    records = []
    for path in sorted(model_dir.rglob("*")):
        if path.is_symlink():
            raise ModelPreparationError("Extracted model contains a symbolic link.")
        if path.is_file():
            digest = hashlib.sha256()
            with path.open("rb") as source:
                for chunk in iter(lambda: source.read(1024 * 1024), b""):
                    digest.update(chunk)
            records.append({"path": path.relative_to(model_dir).as_posix(), "sha256": digest.hexdigest()})
        elif not path.is_dir():
            raise ModelPreparationError("Extracted model contains a non-regular file.")
    return records


def prepare_models(output_dir: Path) -> Path:
    destination = output_dir.expanduser().absolute()
    if destination.exists():
        raise ModelPreparationError("Output model directory already exists; refusing to mix or replace model files.")
    destination.parent.mkdir(parents=True, exist_ok=True)
    staging = destination.parent / f".{destination.name}.staging-{uuid.uuid4().hex}"
    staging.mkdir()
    artifacts = []
    try:
        with tempfile.TemporaryDirectory(prefix="ocr-model-archives-") as temporary:
            temporary_dir = Path(temporary)
            for name, source_url in MODEL_SOURCES.items():
                archive = temporary_dir / f"{name}.tar"
                archive_sha256 = _download(source_url, archive)
                if archive_sha256 != MODEL_ARCHIVE_SHA256[name]:
                    raise ModelPreparationError(
                        f"Downloaded archive digest does not match the pinned artifact: {name}."
                    )
                extraction = temporary_dir / f"{name}-expanded"
                extraction.mkdir()
                model_root = _extract_archive(archive, extraction)
                final_model_dir = staging / ARTIFACTS[name]
                shutil.copytree(model_root, final_model_dir, symlinks=False)
                files = _file_records(final_model_dir)
                if not REQUIRED_FILES.issubset({item["path"] for item in files}):
                    raise ModelPreparationError("Prepared model is missing the required inference files.")
                artifacts.append(
                    {
                        "name": name,
                        "sourceUrl": source_url,
                        "archiveSha256": archive_sha256,
                        "files": files,
                    }
                )

        manifest = {"schemaVersion": SCHEMA_VERSION, "artifacts": artifacts}
        manifest_path = staging / "model-manifest.json"
        with manifest_path.open("x", encoding="utf-8", newline="\n") as output:
            json.dump(manifest, output, ensure_ascii=True, indent=2)
            output.write("\n")
        os.replace(staging, destination)
        return destination
    except BaseException:
        shutil.rmtree(staging, ignore_errors=True)
        raise


def main() -> int:
    parser = argparse.ArgumentParser(description="Download the pinned local PaddleX OCR models.")
    parser.add_argument(
        "--output-dir",
        type=Path,
        default=Path(__file__).resolve().parent / "models",
        help="New output directory for models and their source manifest.",
    )
    args = parser.parse_args()
    try:
        output = prepare_models(args.output_dir)
    except ModelPreparationError as error:
        print(f"Model preparation failed: {error}")
        return 1
    print(f"Prepared local OCR models at {output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
