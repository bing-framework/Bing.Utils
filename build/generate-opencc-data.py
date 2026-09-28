"""从固定 OpenCC UTF-8 快照生成简繁转换资源。"""

import argparse
import gzip
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "asset/opencc"
TARGET = ROOT / "asset/opencc/data"


def load_dictionary(name):
    entries = {}
    for number, line in enumerate((SOURCE / name).read_text(encoding="utf-8").splitlines(), 1):
        if not line or line.startswith("#"):
            continue
        parts = line.split("\t")
        if len(parts) != 2 or not parts[0] or not parts[1]:
            raise ValueError(f"{name}:{number}: invalid entry")
        value = parts[1].split(" ", 1)[0]
        if not value or parts[0] in entries:
            raise ValueError(f"{name}:{number}: duplicate or empty value")
        entries[parts[0]] = value
    return entries


def generate(phrase_name, character_name):
    # 单字映射作为回退；即使同键存在，也以词组规则优先。
    entries = load_dictionary(character_name)
    entries.update(load_dictionary(phrase_name))
    content = "".join(f"{key}\t{entries[key]}\n" for key in sorted(entries))
    return gzip.compress(content.encode("utf-8"), compresslevel=9, mtime=0), len(entries)


def reverse_dictionary(name):
    candidates = {}
    preferences = {}
    for number, line in enumerate((SOURCE / name).read_text(encoding="utf-8").splitlines(), 1):
        if line.startswith("# @reverse-prefer:"):
            fields = line.split(":", 1)[1].split()
            if len(fields) not in (1, 2):
                raise ValueError(f"{name}:{number}: invalid reverse preference")
            preferences[fields[0]] = fields[-1]
            continue
        if not line or line.startswith("#"):
            continue
        parts = line.split("\t")
        if len(parts) != 2 or not parts[0] or not parts[1]:
            raise ValueError(f"{name}:{number}: invalid entry")
        for value in parts[1].split(" "):
            candidates.setdefault(value, []).append(parts[0])
    for key, preferred in preferences.items():
        if key not in candidates or preferred not in candidates[key]:
            raise ValueError(f"{name}: invalid reverse preference for {key}")
        candidates[key].remove(preferred)
        candidates[key].insert(0, preferred)
    return {key: values[0] for key, values in candidates.items()}


def generate_regional(*names, reverse_variants=None):
    entries = reverse_dictionary(reverse_variants) if reverse_variants else {}
    for name in names:
        entries.update(load_dictionary(name))
    content = "".join(f"{key}\t{entries[key]}\n" for key in sorted(entries))
    return gzip.compress(content.encode("utf-8"), compresslevel=9, mtime=0), len(entries)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    for name, phrases, characters in (
        ("s2t.gz", "STPhrases.txt", "STCharacters.txt"),
        ("t2s.gz", "TSPhrases.txt", "TSCharacters.txt"),
    ):
        payload, count = generate(phrases, characters)
        destination = TARGET / name
        if args.check:
            if not destination.exists() or destination.read_bytes() != payload:
                raise SystemExit(f"generated data differs: {destination}")
        else:
            TARGET.mkdir(parents=True, exist_ok=True)
            destination.write_bytes(payload)
        print(f"{name}: {count} records, {len(payload)} bytes")

    for name, sources, reverse_variants in (
        ("tw-forward.gz", ("TWVariants.txt", "TWVariantsPhrases.txt", "TWPhrases.txt"), None),
        ("tw-reverse.gz", ("TWVariantsRevPhrases.txt", "TWPhrasesRev.txt"), "TWVariants.txt"),
        ("hk-forward.gz", ("HKVariants.txt", "HKVariantsPhrases.txt"), None),
        ("hk-reverse.gz", ("HKVariantsRevPhrases.txt",), "HKVariants.txt"),
    ):
        payload, count = generate_regional(*sources, reverse_variants=reverse_variants)
        destination = TARGET / name
        if args.check:
            if not destination.exists() or destination.read_bytes() != payload:
                raise SystemExit(f"generated data differs: {destination}")
        else:
            TARGET.mkdir(parents=True, exist_ok=True)
            destination.write_bytes(payload)
        print(f"{name}: {count} records, {len(payload)} bytes")


if __name__ == "__main__":
    main()
