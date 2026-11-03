"""Run an offline contract smoke against a local ready OCR host."""

from __future__ import annotations

import argparse
import base64
import io
import json
import os
import sys
import urllib.error
import urllib.request
from pathlib import Path
from urllib.parse import urljoin

from PIL import Image, ImageDraw, ImageFont


def _get_json(url: str) -> tuple[int, dict]:
    request = urllib.request.Request(url, headers={"Accept": "application/json"})
    try:
        with urllib.request.urlopen(request, timeout=10) as response:
            return response.status, json.loads(response.read().decode("utf-8"))
    except urllib.error.HTTPError as error:
        body = error.read(4096)
        try:
            payload = json.loads(body.decode("utf-8"))
        except (UnicodeError, json.JSONDecodeError):
            payload = {}
        return error.code, payload


def _make_png(text: str | None, font: ImageFont.FreeTypeFont | ImageFont.ImageFont) -> bytes:
    image = Image.new("RGB", (900, 160), "white")
    if text:
        ImageDraw.Draw(image).text((24, 36), text, font=font, fill="black")
    output = io.BytesIO()
    image.save(output, format="PNG")
    return output.getvalue()


def _post_ocr(url: str, image_bytes: bytes) -> dict:
    payload = {
        "file": base64.b64encode(image_bytes).decode("ascii"),
        "fileType": 1,
        "useDocOrientationClassify": False,
        "useDocUnwarping": False,
        "useTextlineOrientation": True,
        "textRecScoreThresh": 0,
        "visualize": False,
    }
    request = urllib.request.Request(
        url,
        data=json.dumps(payload).encode("utf-8"),
        headers={"Content-Type": "application/json", "Accept": "application/json"},
    )
    with urllib.request.urlopen(request, timeout=40) as response:
        result = json.loads(response.read().decode("utf-8"))
    if result.get("errorCode") != 0 or result.get("errorMsg") != "Success":
        raise RuntimeError("OCR endpoint returned an unsuccessful response.")
    return result


def _validate_response(result: dict, profile_id: str, width: int, height: int) -> dict:
    if result.get("modelProfileId") != profile_id:
        raise RuntimeError("OCR response profile does not match the ready worker.")
    pages = result.get("result", {}).get("ocrResults", [])
    if len(pages) != 1:
        raise RuntimeError("OCR response must contain exactly one page.")
    data = pages[0].get("prunedResult", {})
    texts = data.get("rec_texts")
    scores = data.get("rec_scores")
    polygons = data.get("rec_polys")
    if not isinstance(texts, list) or not isinstance(scores, list) or not isinstance(polygons, list):
        raise RuntimeError("OCR response arrays are missing or invalid.")
    if len(scores) != len(texts) or len(polygons) != len(texts):
        raise RuntimeError("OCR response arrays have inconsistent lengths.")
    if data.get("input_img_shape") != [height, width]:
        raise RuntimeError("OCR response image shape does not match the sample.")

    for polygon in polygons:
        if not isinstance(polygon, list) or len(polygon) != 4:
            raise RuntimeError("OCR response contains a non-quadrilateral region.")
        a, b, c = polygon[:3]
        cross = (b[0] - a[0]) * (c[1] - b[1]) - (b[1] - a[1]) * (c[0] - b[0])
        if cross <= 0:
            raise RuntimeError("OCR response polygon is not clockwise.")

    return {"texts": [text.encode("unicode_escape").decode("ascii") for text in texts], "scores": scores}


def main() -> int:
    parser = argparse.ArgumentParser(description="Smoke-test a local OCR host without external network access.")
    parser.add_argument("--endpoint", default=os.environ.get("OCR_ENDPOINT", "http://127.0.0.1:8080/"))
    parser.add_argument("--model-profile", default=os.environ.get("OCR_MODEL_PROFILE"))
    parser.add_argument("--font", type=Path, help="Optional TrueType font for the Chinese sample.")
    args = parser.parse_args()

    endpoint = args.endpoint.rstrip("/") + "/"
    ready_status, ready = _get_json(urljoin(endpoint, "health/ready"))
    profile_id = ready.get("modelProfileId")
    if ready_status != 200 or ready.get("ready") is not True or not isinstance(profile_id, str):
        raise RuntimeError("OCR host is not ready.")
    if args.model_profile and profile_id != args.model_profile:
        raise RuntimeError("OCR ready profile does not match OCR_MODEL_PROFILE.")

    font = ImageFont.truetype(str(args.font), 48) if args.font else ImageFont.load_default(size=48)
    sample_text = "OCR \u6d4b\u8bd5 123 ABC" if args.font else "OCR TEST 123 ABC"
    samples = (("blank", _make_png(None, font)), ("text", _make_png(sample_text, font)))
    responses = {}
    ocr_url = urljoin(endpoint, "ocr")
    for name, image_bytes in samples:
        result = _post_ocr(ocr_url, image_bytes)
        responses[name] = _validate_response(result, profile_id, 900, 160)

    if not responses["text"]["texts"]:
        raise RuntimeError("OCR did not detect text in the generated text sample.")
    print(json.dumps({"ready": True, "modelProfileId": profile_id, "samples": responses}, ensure_ascii=True))
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:
        print(f"OCR smoke failed: {type(error).__name__}: {error}", file=sys.stderr)
        raise SystemExit(1)
