"""Export more real Gaussian centres while retaining the existing BubbleWorld frame.

Requires numpy. The reference MSPC must be the original lower-density scene.
Example: python resample_nature_cloud.py scan.ply reference.bytes output.bytes --count 500000
"""
import argparse
import math
import struct
from pathlib import Path

import numpy as np

RECORD = np.dtype([("position", "<f4", 3), ("size", "<f2"), ("color", "u1", 4)])


def read_gaussians(path):
    with open(path, "rb") as stream:
        header = []
        while True:
            line = stream.readline()
            if not line:
                raise ValueError("Missing PLY end_header")
            header.append(line.decode("ascii").strip())
            if header[-1] == "end_header":
                break
        expected = ["x", "y", "z", "f_dc_0", "f_dc_1", "f_dc_2", "opacity",
                    "scale_0", "scale_1", "scale_2", "rot_0", "rot_1", "rot_2", "rot_3"]
        properties = [line.removeprefix("property float ") for line in header if line.startswith("property ")]
        if "format binary_little_endian 1.0" not in header or properties != expected:
            raise ValueError("Expected the supplied 14-float Gaussian PLY format")
        count = int(next(line.split()[-1] for line in header if line.startswith("element vertex ")))
        data = np.frombuffer(stream.read(), dtype="<f4")
        if data.size != count * 14:
            raise ValueError("Truncated or unexpected PLY payload")
        return data.reshape(count, 14)


def read_cloud(path):
    with open(path, "rb") as stream:
        magic, version, count, eye, radius = struct.unpack("<4siiff", stream.read(20))
        records = np.frombuffer(stream.read(), dtype=RECORD).copy()
    if magic != b"MSPC" or version != 1 or len(records) != count:
        raise ValueError("Invalid reference MSPC")
    return records, eye, radius


def color_keys(colors):
    colors = colors.astype(np.uint32)
    return (colors[:, 0] << 16) | (colors[:, 1] << 8) | colors[:, 2]


def recover_frame(source, colors, reference):
    # Unique RGB correspondences recover the original rotation/reflection, including floor alignment.
    source_key, si, sn = np.unique(color_keys(colors), return_index=True, return_counts=True)
    target_key, ti, tn = np.unique(color_keys(reference["color"][:, :3]), return_index=True, return_counts=True)
    _, i, j = np.intersect1d(source_key[sn == 1], target_key[tn == 1], return_indices=True)
    x = source[si[sn == 1][i], :3].astype(float)
    y = reference["position"][ti[tn == 1][j]].astype(float)
    if len(x) < 1000:
        raise ValueError("Not enough exact colour correspondences to preserve the scene frame")
    rng = np.random.default_rng(123)
    best = np.zeros(len(x), dtype=bool)
    for _ in range(100):
        pick = rng.choice(len(x), 3, replace=False)
        a, b = x[pick], y[pick]
        u, _, vt = np.linalg.svd((a - a.mean(0)).T @ (b - b.mean(0)))
        rotation = u @ vt
        offset = b.mean(0) - a.mean(0) @ rotation
        good = np.linalg.norm(x @ rotation + offset - y, axis=1) < .002
        if good.sum() > best.sum():
            best = good
    if best.sum() < 1000 or best.mean() < .9:
        raise ValueError("Reference does not reliably match this source scan")
    a, b = x[best], y[best]
    u, _, vt = np.linalg.svd((a - a.mean(0)).T @ (b - b.mean(0)))
    rotation = u @ vt
    offset = b.mean(0) - a.mean(0) @ rotation
    residual = np.linalg.norm(a @ rotation + offset - b, axis=1)
    print(f"Frame: {best.sum()} matches; max residual {residual.max():.8f} m")
    print("Row-vector transform:", rotation.tolist(), "offset:", offset.tolist())
    return source[:, :3].astype(float) @ rotation + offset


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("reference", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--count", type=int, default=500000)
    args = parser.parse_args()
    if args.reference.resolve() == args.output.resolve():
        raise ValueError("Keep the lower-density reference separate from the output")
    source = read_gaussians(args.source)
    reference, eye, max_radius = read_cloud(args.reference)
    if args.count <= len(reference):
        raise ValueError("Target count must exceed reference count")
    colors = np.rint(np.clip((.5 + .28209479177387814 * source[:, 3:6]) * 255, 0, 255)).astype(np.uint8)
    positions = recover_frame(source, colors, reference)
    radii = np.linalg.norm(positions, axis=1)

    # Exclude centres already present; find exact matches by sorting X then checking neighbouring candidates.
    order = np.argsort(positions[:, 0])
    lookup = np.searchsorted(positions[order, 0], reference["position"][:, 0])
    nearest = np.zeros(len(reference), dtype=int)
    distance = np.full(len(reference), np.inf)
    for delta in range(-24, 25):
        candidate = order[np.clip(lookup + delta, 0, len(order) - 1)]
        error = np.linalg.norm(positions[candidate] - reference["position"], axis=1)
        better = error < distance
        nearest[better], distance[better] = candidate[better], error[better]
    if distance.max() > .0002:
        raise ValueError(f"Could not locate every reference centre: max error {distance.max()}")
    eligible = np.isfinite(source).all(axis=1) & (source[:, 6] >= math.log(.08 / .92)) & (radii <= max_radius)
    eligible[nearest] = False
    candidates = np.flatnonzero(eligible)
    needed = args.count - len(reference)
    if len(candidates) < needed:
        raise ValueError("Not enough additional genuine source centres")
    # Mild near-field/opacity weighting keeps detail where the viewer can inspect it.
    opacity = 1 / (1 + np.exp(-np.clip(source[candidates, 6], -80, 80)))
    weights = opacity / np.sqrt(1 + radii[candidates] / 12)
    rng = np.random.default_rng(20261027)
    chosen = rng.choice(candidates, needed, replace=False, p=weights / weights.sum())
    added = np.zeros(needed, dtype=RECORD)
    added["position"] = positions[chosen]
    # Same Gaussian-to-quad footprint convention as the original export, at the new sampling density.
    sizes = np.exp(source[chosen, 7:10]).max(axis=1) * 1.68
    sizes = np.minimum(np.maximum(sizes, np.maximum(.025, radii[chosen] * .0045)),
                       np.minimum(.9, .035 + radii[chosen] * .012))
    density_scale = math.sqrt(len(reference) / args.count)
    added["size"] = sizes * density_scale
    added["color"][:, :3] = colors[chosen]
    added["color"][:, 3] = 255
    reference["size"] = reference["size"].astype(float) * density_scale
    combined = np.concatenate([reference, added])
    combined = combined[np.argsort(np.linalg.norm(combined["position"], axis=1), kind="stable")]
    header = struct.pack("<4siiff", b"MSPC", 1, len(combined), eye, float(np.linalg.norm(combined["position"], axis=1).max()))
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_bytes(header + combined.tobytes())
    print(f"Exported {len(combined)} points: {len(reference)} retained + {needed} new source centres")
    print(f"Eye height {eye:.8f} m; footprint multiplier {density_scale:.5f}; output {args.output}")


if __name__ == "__main__":
    main()
