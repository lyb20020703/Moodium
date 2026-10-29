"""Convert a binary 14-float Gaussian PLY into a Bubble World MSPC and round atlas."""
import argparse
import math
import struct
import json
import hashlib
import subprocess
import sys
from datetime import datetime, timezone
from pathlib import Path

import numpy as np
from PIL import Image

RECORD = np.dtype([("position", "<f4", 3), ("size", "<f2"), ("color", "u1", 4)])


def read_gaussians(path: Path):
    with path.open("rb") as stream:
        header = []
        while True:
            line = stream.readline()
            if not line:
                raise ValueError("Missing PLY end_header")
            text = line.decode("ascii").strip()
            header.append(text)
            if text == "end_header":
                break
        expected = ["x", "y", "z", "f_dc_0", "f_dc_1", "f_dc_2", "opacity",
                    "scale_0", "scale_1", "scale_2", "rot_0", "rot_1", "rot_2", "rot_3"]
        properties = [line.removeprefix("property float ") for line in header if line.startswith("property ")]
        count = int(next(line.split()[-1] for line in header if line.startswith("element vertex ")))
        if "format binary_little_endian 1.0" not in header or properties != expected:
            raise ValueError("Expected a binary 14-float Gaussian PLY")
        values = np.frombuffer(stream.read(), dtype="<f4")
        if values.size != count * 14:
            raise ValueError("Truncated PLY payload")
        return values.reshape(count, 14)


def round_atlas(colors, path: Path):
    size = 4
    while (size // 4) ** 2 < len(colors):
        size *= 2
    pixels = np.zeros((size, size, 4), dtype=np.uint8)
    alpha = np.array([[0, 48, 48, 0], [48, 255, 255, 48], [48, 255, 255, 48], [0, 48, 48, 0]], dtype=np.uint8)
    per_row = size // 4
    for index, color in enumerate(colors):
        x, y = (index % per_row) * 4, (index // per_row) * 4
        pixels[y:y + 4, x:x + 4, :3] = color[:3]
        pixels[y:y + 4, x:x + 4, 3] = alpha
    # Unity SetPixels32 starts at the bottom row; PNG starts at the top.
    # Match BubbleWorldData.BuildRoundAtlas and the baked WriteRoundUVs layout.
    Image.fromarray(pixels[::-1].copy(), "RGBA").save(path)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("source", type=Path)
    parser.add_argument("output_folder", type=Path)
    parser.add_argument("--count", type=int, default=500000)
    parser.add_argument("--max-radius", type=float, default=72.0)
    parser.add_argument("--eye-height", type=float, default=1.35)
    parser.add_argument("--frame-json", type=Path, help="Registered source frame and measured eye height; writes provenance.")
    parser.add_argument("--verify-panorama", type=Path, help="Run full canonical registration as the final export gate.")
    parser.add_argument("--row-transform", type=float, nargs=9, metavar=("M00", "M01", "M02", "M10", "M11", "M12", "M20", "M21", "M22"),
                        help="Optional 3×3 row-vector transform that aligns a source PLY to the Bubble World frame.")
    args = parser.parse_args()
    frame = None
    if args.frame_json:
        if args.row_transform is not None:
            parser.error("Use either --frame-json or --row-transform")
        frame = json.loads(args.frame_json.read_text())
        args.row_transform = frame["row_transform_for_converter"]
        args.eye_height = float(frame["eye_height_above_ground_m"])
    if not .2 <= args.eye_height <= 2:
        raise ValueError("Eye height must be within 0.2–2m")
    data = read_gaussians(args.source)
    positions = data[:, :3]
    if args.row_transform is not None:
        matrix = np.asarray(args.row_transform, dtype=np.float64).reshape(3, 3)
        if not np.isfinite(matrix).all() or not np.allclose(matrix.T @ matrix, np.eye(3), atol=5e-6):
            raise ValueError("Frame must be orthogonal (rotation or reflection), without scale/shear")
        positions = positions @ matrix.astype(np.float32)
    # Orthogonal transforms preserve distance. Use source radii to keep selection
    # and ordering identical across refinements, including float32 tie cases.
    radii = np.linalg.norm(data[:, :3], axis=1)
    opacity = 1 / (1 + np.exp(-np.clip(data[:, 6], -80, 80)))
    candidates = np.flatnonzero(np.isfinite(data).all(axis=1) & (opacity >= .08) & (radii <= args.max_radius))
    if len(candidates) < args.count:
        raise ValueError(f"Only {len(candidates)} eligible points for requested {args.count}")
    weights = opacity[candidates] / np.sqrt(1 + radii[candidates] / 12)
    chosen = np.random.default_rng(20261028).choice(candidates, args.count, replace=False, p=weights / weights.sum())
    colour = np.rint(np.clip((.5 + .28209479177387814 * data[chosen, 3:6]) * 255, 0, 255)).astype(np.uint8)
    sizes = np.exp(data[chosen, 7:10]).max(axis=1) * 1.68 * math.sqrt(len(data) / args.count)
    sizes = np.minimum(np.maximum(sizes, np.maximum(.025, radii[chosen] * .0045)), np.minimum(.9, .035 + radii[chosen] * .012))
    records = np.zeros(args.count, dtype=RECORD)
    records["position"], records["size"] = positions[chosen], sizes.astype(np.float16)
    records["color"][:, :3], records["color"][:, 3] = colour, 255
    order = np.argsort(radii[chosen], kind="stable")
    records = records[order]
    args.output_folder.mkdir(parents=True, exist_ok=True)
    maximum = float(np.linalg.norm(records["position"], axis=1).max())
    (args.output_folder / "points.bytes").write_bytes(struct.pack("<4siiff", b"MSPC", 1, len(records), args.eye_height, maximum) + records.tobytes())
    round_atlas(records["color"], args.output_folder / "point_colors_round.png")
    if frame is not None:
        frame = dict(frame, exported_at=datetime.now(timezone.utc).isoformat(),
                     source_sha256=hashlib.sha256(args.source.read_bytes()).hexdigest(),
                     points_sha256=hashlib.sha256((args.output_folder / "points.bytes").read_bytes()).hexdigest(),
                     count=len(records), runtime_source_rotation="identity")
        (args.output_folder / "frame.json").write_text(json.dumps(frame, indent=2) + "\n")
    print(f"Exported {len(records)} points, max radius {maximum:.3f} m")
    if args.verify_panorama:
        # MSPC rendering alpha is intentionally opaque. Preserve the original Gaussian
        # confidence separately so verification uses the reference tool's >.35 filter.
        confidence = args.output_folder / "registration_opacity.npy"
        np.save(confidence, opacity[chosen][order])
        sampling_positions = args.output_folder / "registration_source_positions.npy"
        np.save(sampling_positions, data[chosen][order, :3])
        subprocess.run([sys.executable, str(Path(__file__).with_name("register_nested_world.py")),
                        str(args.output_folder / "points.bytes"), str(args.verify_panorama),
                        str(args.output_folder / "registration"), "--verify-canonical",
                        "--source-opacity", str(confidence),
                        "--sampling-positions", str(sampling_positions)], check=True)


if __name__ == "__main__":
    main()
