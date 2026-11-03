"""Validation and identity generation for prepared local models."""

from __future__ import annotations

import hashlib
import json
import os
import re
from pathlib import Path, PurePosixPath
from urllib.parse import urlsplit


SCHEMA_VERSION = 1
ARTIFACTS = {
    "PP-OCRv6_small_det": "PP-OCRv6_small_det",
    "PP-OCRv6_small_rec": "PP-OCRv6_small_rec",
    "PP-LCNet_x1_0_textline_ori": "PP-LCNet_x1_0_textline_ori",
}
MODEL_SOURCES = {
    "PP-OCRv6_small_det": "https://paddle-model-ecology.bj.bcebos.com/paddlex/official_inference_model/paddle3.0.0/PP-OCRv6_small_det_infer.tar",
    "PP-OCRv6_small_rec": "https://paddle-model-ecology.bj.bcebos.com/paddlex/official_inference_model/paddle3.0.0/PP-OCRv6_small_rec_infer.tar",
    "PP-LCNet_x1_0_textline_ori": "https://paddle-model-ecology.bj.bcebos.com/paddlex/official_inference_model/paddle3.0.0/PP-LCNet_x1_0_textline_ori_infer.tar",
}
MODEL_ARCHIVE_SHA256 = {
    "PP-OCRv6_small_det": "bfb7c1e59f0faa6b540ebdca93aea3f4b1f2477805b389fbee117820d68fe9f5",
    "PP-OCRv6_small_rec": "da460f968ce9f88325ac3a34fa302077d6e9b0dcefb16ba3137cd7796f879d06",
    "PP-LCNet_x1_0_textline_ori": "6171f69605215a85624d650e9079fa45f7c3eaf944296181bcc5395bf3ddc7f6",
}
OFFICIAL_MODEL_HOST = "paddle-model-ecology.bj.bcebos.com"
OFFICIAL_MODEL_PREFIX = "/paddlex/official_inference_model/paddle3.0.0/"
EXPECTED_RUNTIME = {
    "paddleocr": "3.7.0",
    "paddlex": "3.7.0",
    "paddlepaddle": "3.3.1",
}
_SHA256 = re.compile(r"^[0-9a-f]{64}$")


class ManifestError(ValueError):
    """Raised when a local model manifest or model file fails validation."""


def _hash_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for chunk in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _safe_relative_path(value: str) -> PurePosixPath:
    if not isinstance(value, str) or "\\" in value or ":" in value:
        raise ManifestError("Manifest contains an invalid model file path.")
    path = PurePosixPath(value)
    if not value or path.is_absolute() or any(part in ("", ".", "..") for part in path.parts):
        raise ManifestError("Manifest contains an invalid model file path.")
    return path


def _validate_source(value: str) -> None:
    if not isinstance(value, str):
        raise ManifestError("Manifest model source is not an approved PaddleX HTTPS URL.")
    parsed = urlsplit(value)
    if (
        parsed.scheme != "https"
        or parsed.hostname != OFFICIAL_MODEL_HOST
        or parsed.username is not None
        or not parsed.path.startswith(OFFICIAL_MODEL_PREFIX)
        or parsed.query
        or parsed.fragment
    ):
        raise ManifestError("Manifest model source is not an approved PaddleX HTTPS URL.")


def load_manifest(models_dir: Path) -> dict:
    try:
        root = models_dir.resolve(strict=True)
    except OSError as error:
        raise ManifestError("Local model directory is missing.") from error
    manifest_path = root / "model-manifest.json"
    if manifest_path.is_symlink() or not manifest_path.is_file():
        raise ManifestError("Local model manifest is missing.")
    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    except (OSError, UnicodeError, json.JSONDecodeError) as error:
        raise ManifestError("Local model manifest cannot be read.") from error

    if not isinstance(manifest, dict) or manifest.get("schemaVersion") != SCHEMA_VERSION:
        raise ManifestError("Local model manifest schema is unsupported.")
    artifacts = manifest.get("artifacts")
    if not isinstance(artifacts, list) or len(artifacts) != len(ARTIFACTS):
        raise ManifestError("Local model manifest does not list the required model set.")

    by_name = {}
    for item in artifacts:
        if not isinstance(item, dict) or not isinstance(item.get("name"), str):
            raise ManifestError("Local model manifest has an invalid artifact entry.")
        name = item["name"]
        if name in by_name or name not in ARTIFACTS:
            raise ManifestError("Local model manifest has a duplicate or unknown artifact.")
        _validate_source(item.get("sourceUrl", ""))
        if item["sourceUrl"] != MODEL_SOURCES[name]:
            raise ManifestError("Manifest model source does not match the pinned model artifact.")
        archive_sha256 = item.get("archiveSha256", "")
        if not isinstance(archive_sha256, str) or not _SHA256.fullmatch(archive_sha256):
            raise ManifestError("Local model manifest has an invalid archive digest.")
        if archive_sha256 != MODEL_ARCHIVE_SHA256[name]:
            raise ManifestError("Local model archive digest does not match the pinned artifact.")
        files = item.get("files")
        if not isinstance(files, list) or not files:
            raise ManifestError("Local model manifest does not list model files.")

        checked_files = set()
        model_dir_unresolved = root / ARTIFACTS[name]
        if model_dir_unresolved.is_symlink():
            raise ManifestError("A required local model directory is missing.")
        try:
            model_dir = model_dir_unresolved.resolve(strict=True)
        except OSError as error:
            raise ManifestError("A required local model directory is missing.") from error
        if model_dir.parent != root or not model_dir.is_dir():
            raise ManifestError("A required local model directory is missing.")
        for file_entry in files:
            if not isinstance(file_entry, dict):
                raise ManifestError("Local model manifest has an invalid file entry.")
            relative = _safe_relative_path(file_entry.get("path", ""))
            expected_digest = file_entry.get("sha256", "")
            if not isinstance(expected_digest, str) or not _SHA256.fullmatch(expected_digest):
                raise ManifestError("Local model manifest has an invalid file digest.")
            candidate = model_dir.joinpath(*relative.parts)
            try:
                resolved = candidate.resolve(strict=True)
            except OSError as error:
                raise ManifestError("A required local model file is missing.") from error
            if resolved != model_dir and model_dir not in resolved.parents:
                raise ManifestError("A model file resolves outside its model directory.")
            if candidate.is_symlink() or not candidate.is_file():
                raise ManifestError("A required local model file is missing or not a regular file.")
            try:
                actual_digest = _hash_file(candidate)
            except OSError as error:
                raise ManifestError("A required local model file cannot be read.") from error
            if actual_digest != expected_digest:
                raise ManifestError("A local model file does not match its manifest digest.")
            if relative.as_posix() in checked_files:
                raise ManifestError("Local model manifest contains a duplicate file entry.")
            checked_files.add(relative.as_posix())

        if not {"inference.json", "inference.pdiparams"}.issubset(checked_files):
            raise ManifestError("A model is missing inference.json or inference.pdiparams.")
        if len(checked_files) != len(files):
            raise ManifestError("Local model manifest contains duplicate file entries.")
        actual_files = set()
        for directory, subdirectories, filenames in os.walk(model_dir, followlinks=False):
            current = Path(directory)
            for subdirectory in subdirectories:
                if (current / subdirectory).is_symlink():
                    raise ManifestError("A model contains a symbolic link.")
            for filename in filenames:
                candidate = current / filename
                if candidate.is_symlink() or not candidate.is_file():
                    raise ManifestError("A model contains a non-regular file.")
                actual_files.add(candidate.relative_to(model_dir).as_posix())
        if actual_files != checked_files:
            raise ManifestError("The local model file set does not match its manifest.")
        by_name[name] = item

    if set(by_name) != set(ARTIFACTS):
        raise ManifestError("Local model manifest does not list the required model set.")
    return {"schemaVersion": SCHEMA_VERSION, "artifacts": [by_name[name] for name in ARTIFACTS]}


def create_profile_id(
    manifest: dict,
    runtime: dict[str, str],
    cpu_threads: int = 2,
    enable_mkldnn: bool = True,
) -> str:
    if runtime != EXPECTED_RUNTIME:
        raise ManifestError("Installed PaddleOCR runtime versions do not match the pinned profile.")
    if type(cpu_threads) is not int or cpu_threads <= 0:
        raise ManifestError("OCR CPU thread count is invalid.")
    if type(enable_mkldnn) is not bool:
        raise ManifestError("OCR CPU engine configuration is invalid.")
    identity = {
        "runtime": runtime,
        "manifest": manifest,
        "detector": "PP-OCRv6_small_det",
        "recognizer": "PP-OCRv6_small_rec",
        "textlineOrientation": "PP-LCNet_x1_0_textline_ori",
        "device": "cpu",
        "cpuThreads": cpu_threads,
        "enableMkldnn": enable_mkldnn,
        "useDocOrientationClassify": False,
        "useDocUnwarping": False,
        "useTextlineOrientation": True,
        "textRecScoreThresh": 0.0,
        "visualize": False,
    }
    encoded = json.dumps(identity, sort_keys=True, separators=(",", ":")).encode("utf-8")
    digest = hashlib.sha256(encoded).hexdigest()
    return f"ppocrv6-small:{digest}"
