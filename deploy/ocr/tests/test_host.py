from __future__ import annotations

import asyncio
import base64
import hashlib
import importlib.util
import io
import json
import os
import tempfile
import tarfile
from types import SimpleNamespace
import unittest
from pathlib import Path
from unittest.mock import patch

from PIL import Image
from starlette.requests import Request

from config import Settings
from inputs import InputError, parse_request
from manifest import (
    ARTIFACTS,
    EXPECTED_RUNTIME,
    MODEL_ARCHIVE_SHA256,
    MODEL_SOURCES,
    ManifestError,
    create_profile_id,
    load_manifest,
)
from scheduler import (
    BusyError,
    ClientDisconnected,
    InferenceScheduler,
    InferenceTimeout,
    ServiceUnavailable,
)
from server import _BodyAdmission, ocr
from worker import WorkerUnavailable, _clockwise_polygons, _nullable_scores


_PREPARE_MODELS_SPEC = importlib.util.spec_from_file_location(
    "prepare_models", Path(__file__).resolve().parents[1] / "prepare-models.py"
)
prepare_models = importlib.util.module_from_spec(_PREPARE_MODELS_SPEC)
_PREPARE_MODELS_SPEC.loader.exec_module(prepare_models)


def _image_bytes(image_format: str = "PNG", size: tuple[int, int] = (2, 3)) -> bytes:
    output = io.BytesIO()
    Image.new("RGB", size, "white").save(output, format=image_format)
    return output.getvalue()


def _request_body(image_bytes: bytes, **overrides) -> bytes:
    request = {
        "file": base64.b64encode(image_bytes).decode("ascii"),
        "fileType": 1,
        "useDocOrientationClassify": False,
        "useDocUnwarping": False,
        "useTextlineOrientation": True,
        "textRecScoreThresh": 0,
        "visualize": False,
    }
    request.update(overrides)
    return json.dumps(request).encode("utf-8")


def _settings(**overrides) -> Settings:
    values = {
        "host": "127.0.0.1",
        "port": 8080,
        "models_dir": Path("models"),
        "max_body_bytes": 100_000,
        "max_image_bytes": 50_000,
        "max_pixels": 100,
        "max_dimension": 20,
        "max_waiting": 8,
        "inference_timeout_seconds": 0.2,
        "startup_timeout_seconds": 2.0,
        "worker_join_timeout_seconds": 0.2,
        "cpu_threads": 1,
    }
    values.update(overrides)
    return Settings(**values)


async def _connected() -> bool:
    return False


class InputTests(unittest.TestCase):
    def test_accepts_supported_single_images(self):
        for image_format in ("PNG", "JPEG", "BMP"):
            with self.subTest(image_format=image_format):
                parsed = parse_request(_request_body(_image_bytes(image_format)), _settings())
                self.assertEqual((parsed.width, parsed.height), (2, 3))

    def test_rejects_invalid_protocol_and_encoding(self):
        valid_image = _image_bytes()
        for body in (b"not json", _request_body(valid_image, fileType=True), _request_body(valid_image, extra=True)):
            with self.subTest(body=body[:24]), self.assertRaises(InputError):
                parse_request(body, _settings())
        with self.assertRaises(InputError):
            parse_request(_request_body(b"not an image"), _settings())
        duplicate_field = b'{"file":"x","file":"y"}'
        with self.assertRaises(InputError):
            parse_request(duplicate_field, _settings())

    def test_rejects_pdf_and_oversized_image_dimensions(self):
        with self.assertRaises(InputError):
            parse_request(_request_body(b"%PDF-1.7"), _settings())
        large_image = _image_bytes(size=(11, 10))
        with self.assertRaises(InputError):
            parse_request(_request_body(large_image), _settings(max_pixels=100))
        too_wide = _image_bytes(size=(21, 1))
        with self.assertRaises(InputError):
            parse_request(_request_body(too_wide), _settings(max_dimension=20))

    def test_rejects_body_and_decoded_image_over_limits(self):
        image = _image_bytes()
        with self.assertRaises(InputError):
            parse_request(_request_body(image), _settings(max_body_bytes=40))
        with self.assertRaises(InputError):
            parse_request(_request_body(image), _settings(max_image_bytes=4, max_body_bytes=4096))


class HttpProtocolTests(unittest.IsolatedAsyncioTestCase):
    async def _request(self, body: bytes):
        incoming = [{"type": "http.request", "body": body, "more_body": False}]

        async def receive():
            if incoming:
                return incoming.pop(0)
            return {"type": "http.disconnect"}

        worker = _FakeWorker()
        worker.gate.set()
        scheduler = InferenceScheduler(worker, max_waiting=8, inference_timeout_seconds=1)
        app = SimpleNamespace(
            state=SimpleNamespace(
                settings=_settings(),
                scheduler=scheduler,
                body_admission=_BodyAdmission(9),
            )
        )
        scope = {
            "type": "http",
            "asgi": {"version": "3.0"},
            "http_version": "1.1",
            "method": "POST",
            "scheme": "http",
            "path": "/ocr",
            "raw_path": b"/ocr",
            "query_string": b"",
            "headers": [
                (b"content-type", b"application/json"),
                (b"content-length", str(len(body)).encode("ascii")),
            ],
            "server": ("127.0.0.1", 8080),
            "client": ("127.0.0.1", 12345),
            "app": app,
        }
        response = await ocr(Request(scope, receive))
        await scheduler.close()
        return response

    async def test_success_response_matches_sdk_shape_and_profile(self):
        response = await self._request(_request_body(_image_bytes()))
        payload = json.loads(response.body)
        self.assertEqual(response.status_code, 200)
        self.assertEqual(payload["errorCode"], 0)
        self.assertEqual(payload["errorMsg"], "Success")
        self.assertEqual(payload["modelProfileId"], "fake-profile")
        self.assertEqual(
            set(payload["result"]["ocrResults"][0]["prunedResult"]),
            {"rec_texts", "rec_scores", "rec_polys", "input_img_shape"},
        )

    async def test_invalid_body_returns_400_without_echoing_input(self):
        response = await self._request(b"not json")
        payload = json.loads(response.body)
        self.assertEqual(response.status_code, 400)
        self.assertNotIn("not json", payload["errorMsg"])


class ManifestTests(unittest.TestCase):
    def _write_manifest(self, root: Path) -> dict:
        artifacts = []
        for name, model_dir in ARTIFACTS.items():
            directory = root / model_dir
            directory.mkdir()
            files = []
            for filename, content in (("inference.json", b"{}"), ("inference.pdiparams", b"weights")):
                path = directory / filename
                path.write_bytes(content)
                import hashlib

                files.append({"path": filename, "sha256": hashlib.sha256(content).hexdigest()})
            artifacts.append(
                {
                    "name": name,
                    "sourceUrl": MODEL_SOURCES[name],
                    "archiveSha256": MODEL_ARCHIVE_SHA256[name],
                    "files": files,
                }
            )
        manifest = {"schemaVersion": 1, "artifacts": artifacts}
        (root / "model-manifest.json").write_text(json.dumps(manifest), encoding="utf-8")
        return manifest

    def test_checks_pinned_sources_files_and_builds_actual_profile(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            manifest = self._write_manifest(root)
            loaded = load_manifest(root)
            profile_id = create_profile_id(loaded, EXPECTED_RUNTIME)
            self.assertTrue(profile_id.startswith("ppocrv6-small:"))
            self.assertEqual(len(profile_id.rsplit(":", 1)[1]), 64)
            self.assertEqual(loaded, manifest)

    def test_rejects_changed_model_files_and_unmanifested_files(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            self._write_manifest(root)
            model_dir = root / ARTIFACTS["PP-OCRv6_small_det"]
            (model_dir / "extra.bin").write_bytes(b"unexpected")
            with self.assertRaises(ManifestError):
                load_manifest(root)

        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            self._write_manifest(root)
            (root / ARTIFACTS["PP-OCRv6_small_det"] / "inference.json").write_text("changed", encoding="utf-8")
            with self.assertRaises(ManifestError):
                load_manifest(root)

    def test_rejects_wrong_source(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            manifest = self._write_manifest(root)
            manifest["artifacts"][0]["sourceUrl"] = "https://example.invalid/model.tar"
            (root / "model-manifest.json").write_text(json.dumps(manifest), encoding="utf-8")
            with self.assertRaises(ManifestError):
                load_manifest(root)

    def test_rejects_unapproved_archive_digest(self):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            manifest = self._write_manifest(root)
            manifest["artifacts"][0]["archiveSha256"] = "a" * 64
            (root / "model-manifest.json").write_text(json.dumps(manifest), encoding="utf-8")
            with self.assertRaises(ManifestError):
                load_manifest(root)


class ModelPreparationTests(unittest.TestCase):
    class _Response:
        def __init__(self, url: str, content: bytes):
            self._url = url
            self._content = io.BytesIO(content)
            self.headers = {"Content-Length": str(len(content))}

        def __enter__(self):
            return self

        def __exit__(self, exc_type, exc_value, traceback):
            return False

        def geturl(self):
            return self._url

        def read(self, size: int = -1):
            return self._content.read(size)

    class _Opener:
        def __init__(self, archives: dict[str, bytes]):
            self.archives = archives

        def open(self, request, timeout: int):
            return ModelPreparationTests._Response(request.full_url, self.archives[request.full_url])

    @staticmethod
    def _archive_bytes() -> bytes:
        output = io.BytesIO()
        with tarfile.open(fileobj=output, mode="w") as archive:
            for name, content in (("inference.json", b"{}"), ("inference.pdiparams", b"weights")):
                entry = tarfile.TarInfo(name)
                entry.size = len(content)
                archive.addfile(entry, io.BytesIO(content))
        return output.getvalue()

    def test_rejects_downloaded_bytes_before_extraction(self):
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary) / "models"
            archives = {source: b"unapproved archive bytes" for source in MODEL_SOURCES.values()}
            opener = self._Opener(archives)
            with patch.object(prepare_models.urllib.request, "build_opener", return_value=opener):
                with patch.object(prepare_models, "_extract_archive", wraps=prepare_models._extract_archive) as extract:
                    with self.assertRaises(prepare_models.ModelPreparationError):
                        prepare_models.prepare_models(output)
            extract.assert_not_called()
            self.assertFalse(output.exists())

    def test_accepts_archive_bytes_matching_pinned_digests(self):
        with tempfile.TemporaryDirectory() as temporary:
            output = Path(temporary) / "models"
            archive_bytes = self._archive_bytes()
            archives = {source: archive_bytes for source in MODEL_SOURCES.values()}
            expected_digests = {
                name: hashlib.sha256(archive_bytes).hexdigest()
                for name in MODEL_SOURCES
            }
            opener = self._Opener(archives)
            with patch.dict(MODEL_ARCHIVE_SHA256, expected_digests):
                with patch.object(prepare_models.urllib.request, "build_opener", return_value=opener):
                    prepared = prepare_models.prepare_models(output)
                loaded = load_manifest(prepared)

            self.assertEqual(
                {item["name"]: item["archiveSha256"] for item in loaded["artifacts"]},
                expected_digests,
            )


class ConfigAndResultTests(unittest.TestCase):
    def test_default_image_and_json_limits_match_the_sdk_contract(self):
        with patch.dict(os.environ, {}, clear=True):
            settings = Settings.from_env()
        self.assertEqual(settings.max_image_bytes, 20 * 1024 * 1024)
        self.assertEqual(settings.max_body_bytes, 28 * 1024 * 1024)
        self.assertEqual(settings.max_dimension, 8192)
        self.assertEqual(settings.max_pixels, 20_000_000)
        self.assertEqual(settings.max_waiting, 8)

    def test_scores_can_be_missing_and_polygons_are_clockwise(self):
        self.assertEqual(_nullable_scores([], 2), [None, None])
        self.assertEqual(_nullable_scores([0.9, float("nan"), 1.2, None], 4), [0.9, None, None, None])
        counter_clockwise = [[[0, 0], [0, 2], [2, 2], [2, 0]]]
        polygon = _clockwise_polygons(counter_clockwise, 1, 2, 2)[0]
        a, b, c = polygon[:3]
        cross = (b[0] - a[0]) * (c[1] - b[1]) - (b[1] - a[1]) * (c[0] - b[0])
        self.assertGreater(cross, 0)

    def test_result_normalization_rejects_invalid_quads(self):
        with self.assertRaises(RuntimeError):
            _clockwise_polygons([[[0, 0], [1, 1], [0, 1]]], 1, 2, 2)


class _FakeWorker:
    def __init__(self):
        self.ready = True
        self.profile_id = "fake-profile"
        self.active = 0
        self.max_active = 0
        self.infer_calls = 0
        self.ensure_calls = 0
        self.recover_calls = 0
        self.closed = False
        self.gate = asyncio.Event()
        self.recover_started = asyncio.Event()
        self.recover_release = None
        self.hang_first = False
        self.crash_first = False

    async def ensure_ready(self):
        self.ensure_calls += 1
        if self.closed:
            raise WorkerUnavailable("closed")
        if not self.ready:
            self.ready = True
        return self.profile_id

    async def infer(self, image_bytes: bytes, threshold: float):
        self.infer_calls += 1
        current_call = self.infer_calls
        self.active += 1
        self.max_active = max(self.max_active, self.active)
        try:
            if self.crash_first and current_call == 1:
                self.ready = False
                raise WorkerUnavailable("fake crash")
            if self.hang_first and current_call == 1:
                await asyncio.Future()
            if not self.gate.is_set():
                await self.gate.wait()
            if self.closed:
                raise WorkerUnavailable("closed")
            return {
                "rec_texts": [str(current_call)],
                "rec_scores": [0.99],
                "rec_polys": [[[0, 0], [1, 0], [1, 1], [0, 1]]],
                "input_img_shape": [3, 2],
            }
        finally:
            self.active -= 1

    async def recover(self):
        self.recover_calls += 1
        self.recover_started.set()
        if self.recover_release is not None:
            await self.recover_release.wait()
        self.ready = True

    async def close(self):
        self.closed = True
        self.gate.set()


class SchedulerTests(unittest.IsolatedAsyncioTestCase):
    async def test_admission_is_bounded_and_cancelled_waiter_releases_capacity(self):
        worker = _FakeWorker()
        scheduler = InferenceScheduler(worker, max_waiting=1, inference_timeout_seconds=1)
        active = asyncio.create_task(scheduler.submit(b"first", 0, _connected))
        while worker.infer_calls == 0:
            await asyncio.sleep(0)
        queued = asyncio.create_task(scheduler.submit(b"second", 0, _connected))
        await asyncio.sleep(0.01)
        with self.assertRaises(BusyError):
            await scheduler.submit(b"rejected", 0, _connected)

        queued.cancel()
        with self.assertRaises(asyncio.CancelledError):
            await queued
        next_queued = asyncio.create_task(scheduler.submit(b"third", 0, _connected))
        await asyncio.sleep(0.01)
        worker.gate.set()
        self.assertEqual((await active)["rec_texts"], ["1"])
        self.assertEqual((await next_queued)["rec_texts"], ["2"])
        self.assertEqual(worker.max_active, 1)
        await scheduler.close()

    async def test_disconnect_while_queued_removes_the_ticket(self):
        worker = _FakeWorker()
        scheduler = InferenceScheduler(worker, max_waiting=1, inference_timeout_seconds=1)
        active = asyncio.create_task(scheduler.submit(b"first", 0, _connected))
        while worker.infer_calls == 0:
            await asyncio.sleep(0)
        with self.assertRaises(ClientDisconnected):
            await scheduler.submit(b"second", 0, lambda: asyncio.sleep(0, result=True))
        worker.gate.set()
        await active
        await scheduler.close()

    async def test_timeout_recovers_worker_without_retrying_failed_request(self):
        worker = _FakeWorker()
        worker.hang_first = True
        worker.gate.set()
        scheduler = InferenceScheduler(worker, max_waiting=0, inference_timeout_seconds=0.02)
        with self.assertRaises(InferenceTimeout):
            await scheduler.submit(b"first", 0, _connected)
        self.assertEqual(worker.infer_calls, 1)
        self.assertEqual(worker.recover_calls, 1)
        self.assertEqual((await scheduler.submit(b"second", 0, _connected))["rec_texts"], ["2"])
        await scheduler.close()

    async def test_timeout_response_precedes_rewarmup_but_keeps_active_slot(self):
        worker = _FakeWorker()
        worker.hang_first = True
        worker.gate.set()
        worker.recover_release = asyncio.Event()
        scheduler = InferenceScheduler(worker, max_waiting=0, inference_timeout_seconds=0.02)
        request = asyncio.create_task(scheduler.submit(b"first", 0, _connected))
        await worker.recover_started.wait()
        with self.assertRaises(InferenceTimeout):
            await asyncio.wait_for(request, timeout=0.2)
        with self.assertRaises(BusyError):
            await scheduler.submit(b"second", 0, _connected)
        self.assertEqual(worker.infer_calls, 1)
        worker.recover_release.set()
        await scheduler.close()

    async def test_worker_crash_returns_unavailable_and_next_request_restarts(self):
        worker = _FakeWorker()
        worker.crash_first = True
        worker.gate.set()
        scheduler = InferenceScheduler(worker, max_waiting=0, inference_timeout_seconds=1)
        with self.assertRaises(ServiceUnavailable):
            await scheduler.submit(b"first", 0, _connected)
        self.assertEqual(worker.infer_calls, 1)
        self.assertEqual((await scheduler.submit(b"second", 0, _connected))["rec_texts"], ["2"])
        self.assertEqual(worker.ensure_calls, 2)
        await scheduler.close()

    async def test_shutdown_cancels_waiting_and_releases_active_work(self):
        worker = _FakeWorker()
        scheduler = InferenceScheduler(worker, max_waiting=1, inference_timeout_seconds=1)
        active = asyncio.create_task(scheduler.submit(b"first", 0, _connected))
        while worker.infer_calls == 0:
            await asyncio.sleep(0)
        queued = asyncio.create_task(scheduler.submit(b"second", 0, _connected))
        await asyncio.sleep(0.01)
        await scheduler.close()
        with self.assertRaises(ServiceUnavailable):
            await active
        with self.assertRaises(ServiceUnavailable):
            await queued
        self.assertTrue(worker.closed)

    async def test_queue_limit_cannot_exceed_eight(self):
        with self.assertRaises(ValueError):
            InferenceScheduler(_FakeWorker(), max_waiting=9)


if __name__ == "__main__":
    unittest.main()
