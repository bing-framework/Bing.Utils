"""从固定的 UTF-8 拼音快照生成构建时使用的压缩词库。"""

import argparse
import gzip
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "asset/pinyin"
TARGET = ROOT / "asset/pinyin/data"


def source_lines(name):
    for line_number, line in enumerate((SOURCE / name).read_text(encoding="utf-8").splitlines(), 1):
        value = line.split("#", 1)[0].strip()
        if value:
            yield line_number, value


def build_characters():
    result = {}
    for line_number, line in source_lines("characters.txt"):
        try:
            code_point, readings = line.split(":", 1)
            code_point = int(code_point.strip().removeprefix("U+"), 16)
            reading = readings.strip().split(",", 1)[0]
            if not reading or code_point > 0x10FFFF or 0xD800 <= code_point <= 0xDFFF:
                raise ValueError("invalid code point or reading")
        except ValueError as error:
            raise ValueError(f"characters.txt:{line_number}: {line}") from error
        if code_point in result:
            raise ValueError(f"characters.txt:{line_number}: duplicate U+{code_point:X}")
        result[code_point] = reading
    return "".join(f"{code_point:X}\t{reading}\n" for code_point, reading in sorted(result.items()))


def build_phrases():
    result = {}
    for line_number, line in source_lines("phrases.txt"):
        try:
            phrase, readings = line.split(":", 1)
            phrase = phrase.strip()
            syllables = readings.strip().split()
            if len(phrase) < 2 or len(phrase) != len(syllables) or "\t" in phrase:
                raise ValueError("invalid phrase or syllables")
        except ValueError as error:
            raise ValueError(f"phrases.txt:{line_number}: {line}") from error
        # 同一词组存在多种读音时，采用数据源中首条记录。
        result.setdefault(phrase, " ".join(syllables))
    return "".join(f"{phrase}\t{result[phrase]}\n" for phrase in sorted(result))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true", help="仅验证生成文件与快照一致")
    args = parser.parse_args()
    payloads = {"characters.gz": build_characters(), "phrases.gz": build_phrases()}
    for name, content in payloads.items():
        expected = gzip.compress(content.encode("utf-8"), compresslevel=9, mtime=0)
        destination = TARGET / name
        if args.check:
            if not destination.exists() or destination.read_bytes() != expected:
                raise SystemExit(f"generated data differs: {destination}")
        else:
            TARGET.mkdir(parents=True, exist_ok=True)
            destination.write_bytes(expected)
        print(f"{name}: {len(content.splitlines())} records, {len(expected)} bytes")


if __name__ == "__main__":
    main()
