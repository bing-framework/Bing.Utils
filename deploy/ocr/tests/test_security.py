"""OCR-RV-005/006/007 的安全边界回归，不运行模型或高资源攻击样例。"""

from __future__ import annotations

import asyncio
import json
import struct
import unittest
import zlib
from types import SimpleNamespace
from unittest.mock import patch

from starlette.requests import Request

from config import Settings
from inputs import InputError, _check_json_depth, _inspect_image
from scheduler import InferenceScheduler
from server import _BodyAdmission, ocr
from test_host import _FakeWorker, _image_bytes, _request_body, _settings


def _chunk(tag: bytes, data: bytes) -> bytes:
    return struct.pack('>I', len(data)) + tag + data + struct.pack('>I', zlib.crc32(tag + data) & 0xffffffff)


def _apng(width: int, height: int) -> bytes:
    return (b'\x89PNG\r\n\x1a\n'
            + _chunk(b'IHDR', struct.pack('>IIBBBBB', width, height, 8, 6, 0, 0, 0))
            + _chunk(b'acTL', struct.pack('>II', 2, 0))
            + _chunk(b'fcTL', struct.pack('>IIIIIHHBB', 0, 1, 1, 0, 0, 1, 10, 1, 0))
            + _chunk(b'IDAT', zlib.compress(b'\0' * 5)) + _chunk(b'IEND', b''))


class ImageBoundaryTests(unittest.TestCase):
    def test_unsupported_formats_never_enter_pillow(self):
        # 禁止插件入口比依赖机器快慢的计时断言更直接。
        for raw in (b'#define' + b' ' * 200 + b'_width 1\r',
                    b'%!PS-Adobe-3.0 EPSF-3.0\n%%BeginBinary:-18\n',
                    b'GIF89a' + b'\0' * 30, b'%PDF-1.7'):
            with self.subTest(prefix=raw[:8]), patch('inputs.Image.open') as decoder:
                with self.assertRaises(InputError):
                    _inspect_image(raw, Settings.from_env())
                decoder.assert_not_called()

    def test_apng_is_rejected_before_canvas_allocation_even_with_small_header(self):
        for size in ((8193, 1), (1, 1)):
            with self.subTest(size=size), patch('inputs.Image.open') as decoder:
                with self.assertRaises(InputError):
                    _inspect_image(_apng(*size), Settings.from_env())
                decoder.assert_not_called()

    def test_dimensions_checked_before_decoder_for_each_supported_format(self):
        for image_format in ('PNG', 'JPEG', 'BMP'):
            data = _image_bytes(image_format, (21, 1))
            with self.subTest(format=image_format), patch('inputs.Image.open') as decoder:
                with self.assertRaises(InputError):
                    _inspect_image(data, _settings())
                decoder.assert_not_called()

    def test_png_truncation_and_missing_end_are_rejected_before_decoder(self):
        data = _image_bytes()
        for raw in (data[:-12], data[:35], data[:8] + b'\xff' * 25):
            with self.subTest(size=len(raw)), patch('inputs.Image.open') as decoder:
                with self.assertRaises(InputError):
                    _inspect_image(raw, _settings())
                decoder.assert_not_called()

    def test_json_structure_scan_handles_quoted_brackets_and_escaped_quotes(self):
        for value in ('[{}]', '\\"[', '\\\\"{}', '\\' * 4 + '[', '中文}[]'):
            _check_json_depth(json.dumps({'file': value}).encode('utf-8'))
        with self.assertRaises(InputError):
            _check_json_depth(b'[' * 9 + b'0' + b']' * 9)

    def test_body_deadline_rejects_non_finite_configuration(self):
        for value in ('nan', 'inf', '-1', '0'):
            with self.subTest(value=value), patch.dict('os.environ', {'OCR_BODY_TIMEOUT_SECONDS': value}):
                with self.assertRaises(ValueError):
                    Settings.from_env()


class HttpSecurityTests(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.worker = _FakeWorker()
        self.worker.gate.set()
        self.scheduler = InferenceScheduler(self.worker, max_waiting=8, inference_timeout_seconds=1)
        self.admission = _BodyAdmission(9)
        self.app = SimpleNamespace(state=SimpleNamespace(
            settings=_settings(body_timeout_seconds=0.1), scheduler=self.scheduler,
            body_admission=self.admission))

    async def asyncTearDown(self):
        await self.scheduler.close()

    def request(self, body: bytes = b'', receive=None, content_type=b'application/json'):
        if receive is None:
            delivered = False
            async def receive():
                nonlocal delivered
                if not delivered:
                    delivered = True
                    return {'type': 'http.request', 'body': body, 'more_body': False}
                # 调度器轮询断开时不能误报客户端已断开。
                await asyncio.Event().wait()
        scope = {'type': 'http', 'asgi': {'version': '3.0'}, 'http_version': '1.1',
                 'method': 'POST', 'scheme': 'http', 'path': '/ocr', 'raw_path': b'/ocr',
                 'query_string': b'', 'headers': [(b'content-type', content_type)],
                 'server': ('127.0.0.1', 8080), 'client': ('127.0.0.1', 12345), 'app': self.app}
        return Request(scope, receive)

    async def test_invalid_json_returns_400_and_releases_admission(self):
        for body in (b'{"fileType":' + b'9' * 4301 + b'}',
                     b'{"file":' + b'[' * 1100 + b'0' + b']' * 1100 + b'}',
                     b'{"file":"\\ud800"}', b'\xff'):
            with self.subTest(body_length=len(body)):
                response = await ocr(self.request(body))
                self.assertEqual(response.status_code, 400)
                self.assertEqual(self.admission._count, 0)
        response = await asyncio.wait_for(ocr(self.request(_request_body(_image_bytes()))), 1)
        self.assertEqual(response.status_code, 200)

    async def test_nine_stalled_bodies_expire_and_capacity_is_reusable(self):
        received = 0
        ready = asyncio.Event()
        async def stalled():
            nonlocal received
            received += 1
            if received == 9:
                ready.set()
            await asyncio.Event().wait()
        requests = [asyncio.create_task(ocr(self.request(receive=stalled))) for _ in range(9)]
        try:
            await asyncio.wait_for(ready.wait(), 1)
            self.assertEqual((await ocr(self.request())).status_code, 429)
            responses = await asyncio.wait_for(asyncio.gather(*requests), 2)
            self.assertTrue(all(response.status_code == 408 for response in responses))
            self.assertEqual(self.worker.infer_calls, 0)
            self.assertEqual(self.admission._count, 0)
            self.assertEqual((await ocr(self.request(_request_body(_image_bytes())))).status_code, 200)
        finally:
            for request in requests:
                request.cancel()
            await asyncio.gather(*requests, return_exceptions=True)

    async def test_dripping_body_cannot_reset_absolute_deadline(self):
        chunks = 0
        async def drip():
            nonlocal chunks
            await asyncio.sleep(0.01)
            chunks += 1
            return {'type': 'http.request', 'body': b' ', 'more_body': True}
        response = await asyncio.wait_for(ocr(self.request(receive=drip)), 1)
        self.assertEqual(response.status_code, 408)
        self.assertGreater(chunks, 1)
        self.assertEqual(self.admission._count, 0)

    async def test_caller_cancellation_releases_upload_capacity(self):
        started = asyncio.Event()
        async def stalled():
            started.set()
            await asyncio.Event().wait()
        request = asyncio.create_task(ocr(self.request(receive=stalled)))
        await started.wait()
        request.cancel()
        with self.assertRaises(asyncio.CancelledError):
            await request
        self.assertEqual(self.admission._count, 0)

    async def test_unsupported_content_type_is_rejected_without_reading_body(self):
        async def forbidden():
            raise AssertionError('Rejected Content-Type must not read a body')
        response = await ocr(self.request(receive=forbidden, content_type=b'text/plain'))
        self.assertEqual(response.status_code, 400)
        self.assertEqual(self.admission._count, 0)
