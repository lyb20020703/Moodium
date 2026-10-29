"""Register a Hunyuan Gaussian PLY (raw source frame) to its equirect panorama.

Full search: every 'up' direction on the sphere x every yaw, with and without a mirror (handedness).
Score = mean correlation between globally normalised splat colours and panorama colours at the
projected direction (camera at the PLY origin), using the Unity sphere convention
u = 0.5 + atan2(x, z) / 2pi, v(top) = 0.5 - asin(y) / pi.

usage: register.py in.ply pano.png out_prefix
"""
import argparse, json, math, struct
import numpy as np
from PIL import Image

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument("source")
parser.add_argument("panorama")
parser.add_argument("output_prefix")
parser.add_argument("--verify-canonical", action="store_true")
parser.add_argument("--source-opacity", help="Original Gaussian confidence in exported record order; MSPC alpha is opaque")
parser.add_argument("--sampling-positions", help="Fixed source-frame positions for rotation-invariant direction sampling; scores still use exported positions")
args = parser.parse_args()
ply, pano_path, out = args.source, args.panorama, args.output_prefix

def read_ply(path):
    with open(path, "rb") as f:
        head = b""
        while not head.endswith(b"end_header\n"):
            line = f.readline()
            if not line: raise ValueError("Missing PLY end_header")
            head += line
        if b"format binary_little_endian 1.0" not in head:
            raise ValueError("Expected binary Gaussian PLY")
        n = int([l for l in head.split(b"\n") if l.startswith(b"element vertex")][0].split()[-1])
        props = [l.split()[-1].decode() for l in head.split(b"\n") if l.startswith(b"property")]
        data = np.fromfile(f, dtype=np.float32, count=n * len(props)).reshape(n, len(props))
    return {p: data[:, i] for i, p in enumerate(props)}

def load_pano(path, W, H):
    return np.asarray(Image.open(path).convert("RGB").resize((W, H), Image.Resampling.BOX), dtype=np.float64)

if ply.endswith(".bytes"):
    with open(ply, "rb") as f:
        magic, version, count, eye, maximum = struct.unpack("<4siiff", f.read(20))
        if magic != b"MSPC" or version != 1: raise ValueError("Invalid MSPC")
        records = np.frombuffer(f.read(), dtype=np.dtype([("p", "<f4", 3), ("s", "<f2"), ("c", "u1", 4)]))
        if len(records) != count: raise ValueError("Invalid MSPC count")
    P, C = records["p"].astype(np.float64), records["c"][:, :3].astype(np.float64)
    op = np.load(args.source_opacity) if args.source_opacity else records["c"][:, 3] / 255.
    if len(op) != count: raise ValueError("Opacity sidecar count mismatch")
else:
    d = read_ply(ply)
    P = np.stack([d["x"], d["y"], d["z"]], 1).astype(np.float64)
    C = np.clip(0.5 + 0.28209479 * np.stack([d["f_dc_0"], d["f_dc_1"], d["f_dc_2"]], 1), 0, 1) * 255
    op = 1 / (1 + np.exp(-np.clip(d["opacity"], -80, 80)))
r = np.linalg.norm(P, axis=1)
print("points", len(P), "radius median %.2f  p95 %.2f  max %.2f" % (np.median(r), np.percentile(r, 95), r.max()))
print("centroid", np.round(P.mean(0), 3), " origin-to-centroid %.2f m" % np.linalg.norm(P.mean(0)))

# Even sample over directions (one per direction cell), far-ish and opaque points only.
ok = np.where((op > 0.35) & (r > 1.5))[0]
sampling = np.load(args.sampling_positions) if args.sampling_positions else P
if sampling.shape != P.shape: raise ValueError("Sampling positions shape mismatch")
D = sampling[ok] / np.linalg.norm(sampling[ok], axis=1)[:, None]
cell = np.floor((D + 1) * 40).astype(np.int64)
key = cell[:, 0] * 6561 + cell[:, 1] * 81 + cell[:, 2]
rng = np.random.default_rng(42)
perm = rng.permutation(len(ok))
_, first = np.unique(key[perm], return_index=True)
sel = ok[perm[first]]
print("direction-spread sample", len(sel))

def prep(W, H, count):
    pano = load_pano(pano_path, W, H)
    pn = (pano - pano.reshape(-1, 3).mean(0)) / (pano.reshape(-1, 3).std(0) + 1e-6)
    idx = sel if len(sel) <= count else rng.choice(sel, count, replace=False)
    Dn = P[idx] / r[idx, None]
    Cn = (C[idx] - C[idx].mean(0)) / (C[idx].std(0) + 1e-6)
    return pn, Dn, Cn

def basis(up):
    a = np.array([1.0, 0, 0]) if abs(up[0]) < .9 else np.array([0, 1.0, 0])
    b1 = np.cross(up, a); b1 /= np.linalg.norm(b1)
    b2 = np.cross(up, b1)
    return b1, b2

def scores_for_up(up, pn, Dn, Cn):
    """Scores for every yaw shift, for (no mirror, mirror). Azimuth measured in the plane ⟂ up."""
    H, W, _ = pn.shape
    y = Dn @ up
    b1, b2 = basis(up)
    az = np.arctan2(Dn @ b1, Dn @ b2)                  # angle in plane
    vi = np.clip(((0.5 - np.arcsin(np.clip(y, -1, 1)) / math.pi) * H).astype(int), 0, H - 1)
    rows = pn[vi]                                       # N, W, 3
    A = np.einsum("nwc,nc->nw", rows, Cn) / 3           # N, W
    res = []
    for sign in (1, -1):
        ui = np.mod(((0.5 + sign * az / (2 * math.pi)) * W).astype(int), W)
        cols = np.mod(ui[:, None] + np.arange(W)[None, :], W)
        res.append(np.take_along_axis(A, cols, 1).mean(0))
    return res  # list of arrays over shift s: point lands at column ui + s

def fib(n):
    i = np.arange(n) + .5
    phi = np.arccos(1 - 2 * i / n); th = math.pi * (1 + 5 ** .5) * i
    return np.stack([np.cos(th) * np.sin(phi), np.cos(phi), np.sin(th) * np.sin(phi)], 1)

def rotation(up, shift, sign, W):
    """3x3 T with Unity = T @ raw. Unity y = up; azimuth so that u = 0.5 + sign*az/2pi + shift/W."""
    b1, b2 = basis(up)
    # Unity azimuth atan2(x, z) = 2pi(u - 0.5) = sign*az + 2pi*shift/W  =>  rotate (b2, b1) plane by delta
    delta = 2 * math.pi * shift / W
    # raw dir: az = atan2(d.b1, d.b2). Want Unity (x, z) with atan2(x, z) = sign*az + delta.
    zb = b2 * math.cos(delta) - sign * b1 * math.sin(delta)
    xb = sign * b1 * math.cos(delta) + b2 * math.sin(delta)
    return np.stack([xb, up, zb])

# Coarse over the whole sphere.
pn, Dn, Cn = prep(256, 128, 5000)
best = []
for iteration, up in enumerate(fib(3000)):
    if iteration % 300 == 0: print("global search", iteration, "/3000", flush=True)
    for sign_i, s in enumerate(scores_for_up(up, pn, Dn, Cn)):
        k = int(np.argmax(s))
        best.append((float(s[k]), up, k, (1, -1)[sign_i]))
best.sort(key=lambda t: -t[0])

def ang(a, b): return math.degrees(math.acos(np.clip(a @ b, -1, 1)))
distinct = []
for cand in best:
    if all(ang(cand[1], c[1]) > 15 or cand[3] != c[3] for c in distinct):
        distinct.append(cand)
    if len(distinct) == 6: break
print("coarse top distinct candidates (score, up, mirror):")
for s, up, k, sg in distinct:
    print("  %.4f  up=%s  mirror=%s" % (s, np.round(up, 3), sg == -1))

# Refine the best: local up search with a bigger sample and finer panorama.
s0, up0, k0, sg0 = distinct[0]
pn, Dn, Cn = prep(512, 256, 30000)
W = 512
bestf = (-9, None, None)
for step, span in ((1.0, 4.0), (0.25, 1.0)):
    b1, b2 = basis(up0)
    for a1 in np.arange(-span, span + 1e-9, step):
        for a2 in np.arange(-span, span + 1e-9, step):
            u = up0 + math.radians(a1) * b1 + math.radians(a2) * b2; u /= np.linalg.norm(u)
            s = scores_for_up(u, pn, Dn, Cn)[0 if sg0 == 1 else 1]
            k = int(np.argmax(s))
            if s[k] > bestf[0]: bestf = (float(s[k]), u, k)
    up0 = bestf[1]
sf, upf, kf = bestf
T = rotation(upf, kf, sg0, W)
print("refined score %.4f  up(raw)=%s  mirror=%s  det=%.3f" % (sf, np.round(upf, 4), sg0 == -1, np.linalg.det(T)))
print("T rows (Unity = T @ raw):\n", np.round(T, 5))

# Per-sector check: correlation in 8 azimuth sectors after alignment (all should be clearly positive).
pn2, Dn2, Cn2 = prep(512, 256, 60000)
U = Dn2 @ T.T
uu = 0.5 + np.arctan2(U[:, 0], U[:, 2]) / (2 * math.pi)
vv = 0.5 - np.arcsin(np.clip(U[:, 1], -1, 1)) / math.pi
pc = pn2[np.clip((vv * 256).astype(int), 0, 255), np.mod((uu * 512).astype(int), 512)]
corr = (pc * Cn2).sum(1) / 3
sect = np.floor(uu * 8).astype(int) % 8
print("sector correlation:", [round(float(corr[sect == i].mean()), 3) for i in range(8)])

# Ground check in the aligned frame: RANSAC plane among points below the eye within 6 m.
A = P @ T.T
h = A[:, 1]; hz = np.hypot(A[:, 0], A[:, 2])
cand = A[(h < -0.3) & (h > -4) & (hz < 6) & (op > 0.35)]
cand = cand[rng.choice(len(cand), min(8000, len(cand)), replace=False)]
bn, bi = None, -1
for _ in range(800):
    a, b, c = cand[rng.choice(len(cand), 3, replace=False)]
    n = np.cross(b - a, c - a); ln = np.linalg.norm(n)
    if ln < 1e-9: continue
    n /= ln
    if n[1] < 0: n = -n
    inl = int(np.sum(np.abs((cand - a) @ n) < 0.05))
    if inl > bi: bn, bi, ba = n, inl, a
print("ground plane tilt vs Unity up: %.2f deg  (inliers %d/%d)  eye height above it %.3f m" %
      (math.degrees(math.acos(bn[1])), bi, len(cand), float(-(ba @ bn))))

# Render the aligned splat centres as an equirect image next to the panorama.
Wr, Hr = 1024, 512
img = np.zeros((Hr, Wr, 3)); depth = np.full((Hr, Wr), 1e9)
o = np.argsort(-r)
Ua = A[o] / r[o, None]
ui = np.mod(((0.5 + np.arctan2(Ua[:, 0], Ua[:, 2]) / (2 * math.pi)) * Wr).astype(int), Wr)
vi = np.clip(((0.5 - np.arcsin(np.clip(Ua[:, 1], -1, 1)) / math.pi) * Hr).astype(int), 0, Hr - 1)
img[vi, ui] = C[o]
pano = load_pano(pano_path, Wr, Hr)
both = np.concatenate([img, pano], 0).astype(np.uint8)
Image.fromarray(both).save(out + "_aligned_vs_pano.png")
angle = math.degrees(math.acos(np.clip((np.trace(T) - 1) / 2, -1, 1))) if np.linalg.det(T) > 0 else 180.
gap = sf - distinct[1][0]
passed = angle < 1 and sf >= .55 and gap >= .15
json.dump(dict(score=sf, up_raw=upf.tolist(), mirror=bool(sg0 == -1), T_rows=T.tolist(),
               canonical_error_degrees=angle, score_gap=gap, canonical_pass=passed,
               panorama_resampling="Pillow BOX", source=ply,
               sampling_positions=args.sampling_positions, source_opacity=args.source_opacity,
               coarse=[(s, u.tolist(), bool(g == -1)) for s, u, _, g in distinct]), open(out + ".json", "w"), indent=1)
print("canonical error", angle, "score gap", gap, "pass", passed, flush=True)
if args.verify_canonical and not passed:
    raise SystemExit("Canonical registration gate failed; do not publish these assets")
