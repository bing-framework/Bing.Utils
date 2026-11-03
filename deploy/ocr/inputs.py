"""Strict parsing for the SDK-compatible single-image request."""

from __future__ import annotations

import base64
import binascii
import io
import json
import re
import struct
import warnings
from dataclasses import dataclass

from PIL import Image, UnidentifiedImageError

from config import Settings


class InputError(ValueError):
    """Raised when a request body or image is outside the supported contract."""


@dataclass(frozen=True)
class OcrInput:
    image_bytes: bytes
    width: int
    height: int


_REQUEST_FIELDS = {
    "file",
    "fileType",
    "useDocOrientationClassify",
    "useDocUnwarping",
    "useTextlineOrientation",
    "textRecScoreThresh",
    "visualize",
}


def _reject_constant(value: str) -> None:
    raise InputError(f"Invalid JSON number: {value}.")


def _unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise InputError("Request body contains a duplicate JSON field.")
        result[key] = value
    return result


# 协议是扁平对象；在 JSON 解码前限制结构深度，跳过字符串中的括号和转义。
_JSON_TOKENS = re.compile(rb'["\\{}\[\]]')
_MAX_JSON_DEPTH = 8


def _check_json_depth(body: bytes) -> None:
    depth = 0
    in_string = False
    escaped_at = -1
    for token in _JSON_TOKENS.finditer(body):
        position = token.start()
        value = token[0]
        if position == escaped_at:
            continue
        if in_string:
            if value == b'\\':
                escaped_at = position + 1
            elif value == b'"':
                in_string = False
        elif value == b'"':
            in_string = True
        elif value in (b'{', b'['):
            depth += 1
            if depth > _MAX_JSON_DEPTH:
                raise InputError("JSON nesting exceeds the configured limit.")
        elif value in (b'}', b']'):
            depth -= 1
            if depth < 0:
                raise InputError("Request body must be valid UTF-8 JSON.")


def _bounded_integer(value: str) -> int:
    if len(value) > 20:
        raise InputError("JSON integer is too large.")
    return int(value)


def parse_request(body: bytes, settings: Settings) -> OcrInput:
    if len(body) > settings.max_body_bytes:
        raise InputError("Request body exceeds the configured limit.")
    _check_json_depth(body)
    try:
        request = json.loads(
            body.decode("utf-8"),
            parse_constant=_reject_constant,
            parse_int=_bounded_integer,
            object_pairs_hook=_unique_object,
        )
    except InputError:
        raise
    except (ValueError, RecursionError) as error:
        raise InputError("Request body must be valid UTF-8 JSON.") from error
    if not isinstance(request, dict) or set(request) != _REQUEST_FIELDS:
        raise InputError("Request fields do not match the OCR protocol.")

    if type(request["fileType"]) is not int or request["fileType"] != 1:
        raise InputError("Only fileType 1 is supported.")
    if (
        request["useDocOrientationClassify"] is not False
        or request["useDocUnwarping"] is not False
        or request["useTextlineOrientation"] is not True
        or request["visualize"] is not False
    ):
        raise InputError("OCR options do not match the supported profile.")
    threshold = request["textRecScoreThresh"]
    if type(threshold) not in (int, float) or threshold != 0:
        raise InputError("textRecScoreThresh must be zero.")

    encoded = request["file"]
    if not isinstance(encoded, str) or not encoded.isascii():
        raise InputError("file must be a Base64 string.")
    max_encoded = 4 * ((settings.max_image_bytes + 2) // 3)
    if not encoded or len(encoded) > max_encoded:
        raise InputError("Encoded image exceeds the configured limit.")
    try:
        image_bytes = base64.b64decode(encoded, validate=True)
    except (binascii.Error, ValueError) as error:
        raise InputError("file is not valid Base64.") from error
    if not image_bytes or len(image_bytes) > settings.max_image_bytes:
        raise InputError("Decoded image exceeds the configured limit.")

    width, height = _inspect_image(image_bytes, settings)
    return OcrInput(image_bytes=image_bytes, width=width, height=height)


def _check_dimensions(width: int, height: int, settings: Settings) -> None:
    if (width <= 0 or height <= 0 or width > settings.max_dimension
            or height > settings.max_dimension or width > settings.max_pixels // height):
        raise InputError("Image dimensions exceed the configured limit.")


def _image_header(data: bytes, settings: Settings) -> tuple[str, int, int]:
    """在调用解码器前校验格式、声明尺寸和 PNG 动画标记；不分配像素。"""
    if data.startswith(b'\x89PNG\r\n\x1a\n'):
        if len(data) < 33 or data[8:16] != b'\0\0\0\rIHDR':
            raise InputError("PNG header is invalid.")
        width, height = struct.unpack_from('>II', data, 16)
        _check_dimensions(width, height, settings)
        offset = 8
        saw_data = False
        while offset + 12 <= len(data):
            length = int.from_bytes(data[offset:offset + 4], 'big')
            tag = data[offset + 4:offset + 8]
            end = offset + 12 + length
            if end > len(data):
                raise InputError("PNG chunk is truncated.")
            if tag in (b'acTL', b'fcTL', b'fdAT'):
                raise InputError("Animated PNG images are not supported.")
            if tag == b'IHDR' and offset != 8:
                raise InputError("PNG contains a duplicate header.")
            saw_data |= tag == b'IDAT'
            if tag == b'IEND':
                if length != 0 or not saw_data:
                    raise InputError("PNG end marker is invalid.")
                return 'PNG', width, height
            offset = end
        raise InputError("PNG end marker is missing.")
    if data.startswith(b'\xff\xd8'):
        offset = 2
        while offset < len(data):
            if data[offset] != 255:
                break
            while offset < len(data) and data[offset] == 255:
                offset += 1
            if offset >= len(data):
                break
            marker = data[offset]
            offset += 1
            if marker in (0xD9, 0xDA):
                break
            if marker == 1 or 0xD0 <= marker <= 0xD7:
                continue
            if offset + 2 > len(data):
                break
            length = int.from_bytes(data[offset:offset + 2], 'big')
            if length < 2 or offset + length > len(data):
                break
            if marker in (0xC0, 0xC1, 0xC2, 0xC3, 0xC5, 0xC6, 0xC7,
                          0xC9, 0xCA, 0xCB, 0xCD, 0xCE, 0xCF):
                if length < 8:
                    break
                height, width = struct.unpack_from('>HH', data, offset + 3)
                _check_dimensions(width, height, settings)
                return 'JPEG', width, height
            offset += length
        raise InputError("JPEG frame header is invalid.")
    if data.startswith(b'BM') and len(data) >= 26:
        header_size = int.from_bytes(data[14:18], 'little')
        if header_size == 12:
            width, height = struct.unpack_from('<HH', data, 18)
        elif header_size >= 40 and len(data) >= 14 + header_size:
            width, height = struct.unpack_from('<ii', data, 18)
            height = abs(height)
        else:
            raise InputError("BMP header is invalid.")
        _check_dimensions(width, height, settings)
        return 'BMP', width, height
    raise InputError("Only PNG, JPEG, and BMP images are supported.")


def _inspect_image(image_bytes: bytes, settings: Settings) -> tuple[int, int]:
    image_format, width, height = _image_header(image_bytes, settings)
    try:
        with warnings.catch_warnings():
            warnings.simplefilter("error", Image.DecompressionBombWarning)
            with Image.open(io.BytesIO(image_bytes), formats=(image_format,)) as image:
                if image.format not in {"PNG", "JPEG", "BMP"}:
                    raise InputError("Only PNG, JPEG, and BMP images are supported.")
                if image.size != (width, height):
                    raise InputError("Decoded image dimensions do not match its header.")
                if getattr(image, "n_frames", 1) != 1:
                    raise InputError("Multi-frame images are not supported.")
                image.verify()

            with Image.open(io.BytesIO(image_bytes), formats=(image_format,)) as image:
                if image.format not in {"PNG", "JPEG", "BMP"} or getattr(image, "n_frames", 1) != 1:
                    raise InputError("Only single-frame PNG, JPEG, and BMP images are supported.")
                image.load()
        return width, height
    except InputError:
        raise
    except (OSError, UnidentifiedImageError, Image.DecompressionBombError, Image.DecompressionBombWarning) as error:
        raise InputError("Image data is invalid or exceeds the decoder limit.") from error
