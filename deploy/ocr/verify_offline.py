"""在 Docker --network none 中启动真实 HTTP 宿主并验证本地模型识别。"""
from __future__ import annotations

import argparse
import json
import os
import socket
import subprocess
import sys
import time
from pathlib import Path

from PIL import ImageFont

from smoke import _get_json, _make_png, _post_ocr, _validate_response


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--font', type=Path, help='可选的本地中文字体')
    args = parser.parse_args()
    interfaces = sorted(name for _, name in socket.if_nameindex())
    if interfaces != ['lo']:
        raise RuntimeError('This probe requires an isolated Docker --network none container.')
    environment = os.environ.copy()
    environment.update(OCR_HOST='127.0.0.1', OCR_PORT='8080', PYTHONUNBUFFERED='1')
    process = subprocess.Popen([sys.executable, '-B', 'server.py'], env=environment,
                               stdout=sys.stderr, stderr=sys.stderr)
    try:
        deadline = time.monotonic() + 90
        ready = None
        while time.monotonic() < deadline:
            if process.poll() is not None:
                raise RuntimeError('OCR host exited before readiness.')
            try:
                status, payload = _get_json('http://127.0.0.1:8080/health/ready')
                if status == 200 and payload.get('ready'):
                    ready = payload
                    break
            except OSError:
                pass
            time.sleep(0.25)
        if ready is None:
            raise TimeoutError('Offline model warmup exceeded 90 seconds.')
        profile = ready['modelProfileId']
        font = ImageFont.truetype(str(args.font), 48) if args.font else ImageFont.load_default(size=48)
        text = 'OCR 测试 123 ABC' if args.font else 'OCR TEST 123 ABC'
        results = {}
        for name, label in (('blank', None), ('text', text)):
            response = _post_ocr('http://127.0.0.1:8080/ocr', _make_png(label, font))
            results[name] = _validate_response(response, profile, 900, 160)
        if results['blank']['texts'] or '123' not in ''.join(results['text']['texts']):
            raise RuntimeError('Offline blank/text recognition did not match the sample.')
        if args.font and not any('\\u6d4b' in line for line in results['text']['texts']):
            raise RuntimeError('Offline Chinese sample was not recognized.')
        print(json.dumps({'networkInterfaces': interfaces, 'ready': True,
                          'modelProfileId': profile, 'samples': results}, ensure_ascii=True))
        return 0
    finally:
        if process.poll() is None:
            process.terminate()
            try:
                process.wait(timeout=10)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait(timeout=5)


if __name__ == '__main__':
    raise SystemExit(main())
