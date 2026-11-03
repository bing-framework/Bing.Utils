"""显式启用的真实 Paddle 进程故障恢复测试；无环境时普通测试跳过。"""
from __future__ import annotations

import asyncio
import io
import os
import unittest

from PIL import Image

from config import Settings
from scheduler import InferenceScheduler, InferenceTimeout
from worker import ProcessWorker


def _blank() -> bytes:
    with Image.new('RGB', (160, 80), 'white') as image:
        output = io.BytesIO()
        image.save(output, format='PNG')
        return output.getvalue()


async def _connected() -> bool:
    return False


@unittest.skipUnless(os.environ.get('OCR_REAL_PROCESS_TESTS') == '1',
                     'Set OCR_REAL_PROCESS_TESTS=1 and OCR_MODELS_DIR to run real Paddle recovery')
class RealWorkerRecoveryTests(unittest.IsolatedAsyncioTestCase):
    async def asyncSetUp(self):
        self.worker = ProcessWorker(Settings.from_env())
        self.addAsyncCleanup(self._close)
        await asyncio.wait_for(self.worker.ensure_ready(), 75)

    async def _close(self):
        await asyncio.wait_for(self.worker.close(), 15)

    async def test_real_paddle_crash_reloads_models_and_recognizes_next_image(self):
        process = self.worker._slot.process
        old_pid = process.pid
        process.terminate()
        await asyncio.to_thread(process.join, 5)
        self.assertFalse(process.is_alive())
        response = await asyncio.wait_for(self.worker.infer(_blank(), 0.0), 75)
        self.assertNotEqual(self.worker._slot.process.pid, old_pid)
        self.assertEqual(response['rec_texts'], [])
        self.assertEqual(response['input_img_shape'], [80, 160])

    async def test_real_paddle_unresponsive_process_is_killed_and_replaced(self):
        import psutil
        process = self.worker._slot.process
        old_pid = process.pid
        suspended = psutil.Process(old_pid)
        scheduler = InferenceScheduler(self.worker, max_waiting=0, inference_timeout_seconds=0.2)
        # 仅暂停本测试新建的独占模型进程；finally 保证失败路径也能恢复/回收它。
        suspended.suspend()
        try:
            with self.assertRaises(InferenceTimeout):
                await asyncio.wait_for(scheduler.submit(_blank(), 0.0, _connected), 5)
            execution = scheduler._active.task
            await asyncio.wait_for(execution, 75)
            self.assertFalse(process.is_alive())
            self.assertNotEqual(self.worker._slot.process.pid, old_pid)
            response = await asyncio.wait_for(self.worker.infer(_blank(), 0.0), 30)
            self.assertEqual(response['rec_texts'], [])
        finally:
            if process.is_alive():
                suspended.resume()
            await asyncio.wait_for(scheduler.close(), 15)
