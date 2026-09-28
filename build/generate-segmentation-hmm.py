"""从固定 jieba v0.42.1 概率表生成外置中文分词模型。"""

import argparse
import ast
import gzip
import struct
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "asset/segmentation"
TARGET = SOURCE / "hmm-model.gz"
STATES = "BMES"
MIN_FLOAT = -3.14e100


def load_probability(name):
    tree = ast.parse((SOURCE / name).read_text(encoding="utf-8"), filename=name)
    assignment = next(
        node for node in tree.body
        if isinstance(node, ast.Assign)
        and len(node.targets) == 1
        and isinstance(node.targets[0], ast.Name)
        and node.targets[0].id == "P"
    )
    return ast.literal_eval(assignment.value)


def generate():
    start = load_probability("prob_start.py")
    transitions = load_probability("prob_trans.py")
    emissions = load_probability("prob_emit.py")
    payload = bytearray(b"BHM1")
    for state in STATES:
        payload.extend(struct.pack("<d", start[state]))
    for source in STATES:
        for target in STATES:
            payload.extend(struct.pack("<d", transitions[source].get(target, MIN_FLOAT)))
    for state in STATES:
        items = sorted((ord(character), probability)
                       for character, probability in emissions[state].items())
        payload.extend(struct.pack("<i", len(items)))
        for code_point, probability in items:
            payload.extend(struct.pack("<id", code_point, probability))
    return gzip.compress(bytes(payload), compresslevel=9, mtime=0), sum(
        len(emissions[state]) for state in STATES
    )


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--check", action="store_true")
    args = parser.parse_args()
    payload, count = generate()
    if args.check:
        if not TARGET.exists() or TARGET.read_bytes() != payload:
            raise SystemExit(f"generated data differs: {TARGET}")
    else:
        TARGET.write_bytes(payload)
    print(f"hmm-model.gz: {count} emissions, {len(payload)} bytes")


if __name__ == "__main__":
    main()
