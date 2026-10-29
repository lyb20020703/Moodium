"""
Convert a 3D Gaussian Splat PLY (x,y,z, f_dc_0..2, opacity, scale_0..2, rot_0..3) into a compact point cloud for
Unity / RealityKit, plus an equirectangular panorama rendered from the scan centre.

- Finds the ground plane (RANSAC) to level the scan and measure the camera (eye) height.
- Keeps an evenly spread subset (one point per distance-scaled voxel, then the most visible ones).
- Writes points.bytes (little endian):
    char[4] "MSPC", int32 version=1, int32 count, float32 eyeHeight, float32 maxRadius
    count * { float32 x, y, z (metres, Unity frame: +Y up, +Z = chosen forward), float16 size, uint8 r, g, b, a }
  sorted by distance from the scan centre (near first), so the runtime can cut shells by index.
- Writes panorama.ppm (equirect, u = 0.5 + atan2(x, z) / 2pi, v = 0.5 - asin(y / r) / pi), holes filled.

Pure Python (no numpy). Usage:
  python3 splat_convert.py <in.ply> <out_dir> [--points 160000] [--up x|y|z|-x|-y|-z] [--pano 2048] [--eye 1.4]
--eye moves the scan centre up (or down) along the levelled up axis so it sits this many metres above the ground.
  Use it when the scan camera was close to the ground: every point quad faces the centre, and in the headset the
  viewer's eyes are at standing height, so the centre should be too.
"""
import math, os, random, struct, sys

def parse_args(argv):
    args = {"points": 160000, "up": "x", "pano": 2048, "seed": 7, "eye": 0.0}
    pos = []
    i = 0
    while i < len(argv):
        a = argv[i]
        if a.startswith("--"):
            args[a[2:]] = argv[i + 1]; i += 2
        else:
            pos.append(a); i += 1
    args["in"], args["out"] = pos[0], pos[1]
    args["points"] = int(args["points"]); args["pano"] = int(args["pano"]); args["seed"] = int(args["seed"])
    args["eye"] = float(args["eye"])
    return args

def read_ply(path):
    data = open(path, "rb").read()
    end = data.index(b"end_header\n") + len(b"end_header\n")
    header = data[:end].decode("ascii", "replace").splitlines()
    count, props = 0, []
    for line in header:
        p = line.split()
        if p[:2] == ["element", "vertex"]: count = int(p[2])
        elif p[:1] == ["property"] and count and len(p) == 3: props.append((p[2], p[1]))
    fmt = "<" + "".join({"float": "f", "double": "d", "uchar": "B"}[t] for _, t in props)
    names = [n for n, _ in props]
    size = struct.calcsize(fmt)
    rows = struct.iter_unpack(fmt, data[end:end + size * count])
    return names, rows, count

def vec_norm(v):
    l = math.sqrt(sum(c * c for c in v)) or 1.0
    return [c / l for c in v]

def cross(a, b):
    return [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]]

def dot(a, b):
    return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]

def ransac_plane(points, iters=400, tol=.06, seed=1, expected_up=None):
    rnd = random.Random(seed)
    best, best_in = None, -1
    for _ in range(iters):
        p0, p1, p2 = rnd.sample(points, 3)
        n = cross([p1[k] - p0[k] for k in range(3)], [p2[k] - p0[k] for k in range(3)])
        if dot(n, n) < 1e-12: continue
        n = vec_norm(n); d = -dot(n, p0)
        if expected_up is not None and abs(dot(n, expected_up)) < .85: continue
        inl = sum(1 for p in points if abs(dot(n, p) + d) < tol)
        if inl > best_in: best, best_in = (n, d), inl
    if best is None: raise ValueError('No ground plane consistent with the supplied up direction')
    return best, best_in

def main():
    a = parse_args(sys.argv[1:])
    os.makedirs(a["out"], exist_ok=True)
    names, rows, count = read_ply(a["in"])
    ix = {n: i for i, n in enumerate(names)}
    C0 = 0.28209479177387814
    raw = []
    for r in rows:
        o = 1 / (1 + math.exp(-r[ix["opacity"]]))
        if o < .25: continue
        s = math.exp(max(r[ix["scale_0"]], r[ix["scale_1"]], r[ix["scale_2"]]))
        col = tuple(min(255, max(0, int(round((.5 + C0 * r[ix["f_dc_%d" % k]]) * 255)))) for k in range(3))
        raw.append((r[ix["x"]], r[ix["y"]], r[ix["z"]], s, o, col))
    print("splats", count, "opaque enough", len(raw))

    # Initial up axis, then level with the ground plane under the camera.
    ax = a["up"]; sign = -1.0 if ax.startswith("-") else 1.0; ax = ax[-1]
    up0 = [0.0, 0.0, 0.0]; up0["xyz".index(ax)] = sign
    rnd = random.Random(a["seed"])
    cand = []
    for p in raw:
        v = p[:3]; h = dot(v, up0)
        horiz = math.sqrt(max(0.0, dot(v, v) - h * h))
        if -3.0 < h < -.6 and horiz < 6.0 and p[4] > .6: cand.append(v)
    if len(cand) > 4000: cand = rnd.sample(cand, 4000)
    (n, d), inl = ransac_plane(cand, seed=a["seed"], expected_up=up0)
    if dot(n, up0) < 0: n = [-c for c in n]; d = -d
    up = n; eye = d  # plane: dot(up, p) + d = 0  -> ground is at height -d below the origin
    print("ground inliers", inl, "/", len(cand), "up", [round(c, 3) for c in up], "eye height %.2f m" % eye)

    # Forward = the horizontal direction with the most near content (what the scan was "about").
    ref = [1.0, 0.0, 0.0] if abs(up[0]) < .9 else [0.0, 0.0, 1.0]
    right0 = vec_norm(cross(up, ref)); fwd0 = vec_norm(cross(right0, up))
    bins = [0.0] * 16
    for p in raw:
        v = p[:3]; h = dot(v, up)
        r2 = dot(v, v) - h * h
        if 1.0 < r2 < 64.0 and h > -eye + .2:
            ang = math.atan2(dot(v, right0), dot(v, fwd0)); bins[int((ang + math.pi) / (2 * math.pi) * 16) % 16] += p[4]
    best = max(range(16), key=lambda i: bins[i])
    yaw = (best + .5) / 16 * 2 * math.pi - math.pi
    fwd = vec_norm([math.cos(yaw) * fwd0[k] + math.sin(yaw) * right0[k] for k in range(3)])
    right = vec_norm(cross(up, fwd))  # Unity is left-handed: x = right, y = up, z = forward

    shift = 0.0
    if a["eye"] > 0:
        shift = a["eye"] - eye
        print("raising the centre by %.2f m to eye height %.2f m" % (shift, a["eye"]))
        eye = a["eye"]

    def to_unity(v):
        return (dot(v, right), dot(v, up) - shift, dot(v, fwd))

    pts = []
    max_r = 0.0
    for x, y, z, s, o, col in raw:
        u = to_unity((x, y, z))
        r = math.sqrt(u[0] ** 2 + u[1] ** 2 + u[2] ** 2)
        if r < .25: continue
        max_r = max(max_r, r)
        pts.append((u, r, s, o, col))

    # Even spread: one point per voxel whose size grows with distance (about 0.35 degrees).
    vox = {}
    for p in pts:
        u, r, s, o, col = p
        cell = max(.008, r * .0055)
        key = (int(math.floor(u[0] / cell)), int(math.floor(u[1] / cell)), int(math.floor(u[2] / cell)), int(cell * 1000))
        imp = o * math.sqrt(min(s, .3))
        if key not in vox or vox[key][0] < imp: vox[key] = (imp, p)
    kept = [v[1] for v in vox.values()]
    print("after voxel spread", len(kept))
    if len(kept) > a["points"]:
        kept.sort(key=lambda p: -(p[3] * math.sqrt(min(p[2], .3))))
        kept = kept[:a["points"]]
    fill = math.sqrt(max(1.0, len(raw) / max(1, len(kept))))
    kept.sort(key=lambda p: p[1])

    out = open(os.path.join(a["out"], "points.bytes"), "wb")
    out.write(b"MSPC" + struct.pack("<iiff", 1, len(kept), eye, max_r))
    for u, r, s, o, col in kept:
        # Near points stay small (they are seen up close); far points grow to keep the shell closed.
        size = min(.035 + r * .012, max(r * .0045, 1.05 * s * min(fill, 1.6)))
        out.write(struct.pack("<fffeBBBB", u[0], u[1], u[2], size, col[0], col[1], col[2], int(o * 255)))
    out.close()
    print("points", len(kept), "fill x%.2f" % fill, "max radius %.1f m" % max_r)

    # Colour map: point i -> texel (i % S, i // S), row 0 at the top of the image.
    S = 1
    while S * S < len(kept): S *= 2
    atlas = bytearray(S * S * 3)
    for i, (u, r, s, o, col) in enumerate(kept):
        atlas[i * 3:i * 3 + 3] = bytes(col)
    with open(os.path.join(a["out"], "point_colors.ppm"), "wb") as f:
        f.write(b"P6 %d %d 255\n" % (S, S)); f.write(bytes(atlas))
    print("colour map", S, "x", S)

    # Equirect panorama from the centre (far to near), then fill holes from coarser levels.
    W = a["pano"]; H = W // 2
    acc = [None] * (W * H)
    order = sorted(pts, key=lambda p: -p[1])
    for u, r, s, o, col in order:
        lon = math.atan2(u[0], u[2]); lat = math.asin(max(-1.0, min(1.0, u[1] / r)))
        px = int((.5 + lon / (2 * math.pi)) * W) % W; py = int((.5 - lat / math.pi) * H)
        if not 0 <= py < H: continue
        rad = max(1, min(4, int(1.6 * s / r * W / (2 * math.pi))))
        for dy in range(-rad + 1, rad):
            yy = py + dy
            if not 0 <= yy < H: continue
            for dx in range(-rad + 1, rad):
                i = yy * W + (px + dx) % W
                c = acc[i]
                if c is None: acc[i] = [col[0], col[1], col[2]]
                else:
                    c[0] += (col[0] - c[0]) * o; c[1] += (col[1] - c[1]) * o; c[2] += (col[2] - c[2]) * o
    # pull-push hole filling
    levels = [(W, H, acc)]
    while levels[-1][0] > 8:
        w, h, img = levels[-1]
        nw, nh = w // 2, h // 2
        nimg = [None] * (nw * nh)
        for y in range(nh):
            for x in range(nw):
                s = [0.0, 0.0, 0.0]; n = 0
                for dy in (0, 1):
                    for dx in (0, 1):
                        c = img[(2 * y + dy) * w + 2 * x + dx]
                        if c is not None: s[0] += c[0]; s[1] += c[1]; s[2] += c[2]; n += 1
                if n: nimg[y * nw + x] = [s[0] / n, s[1] / n, s[2] / n]
        levels.append((nw, nh, nimg))
    for li in range(len(levels) - 2, -1, -1):
        w, h, img = levels[li]
        cw, ch, coarse = levels[li + 1]
        for y in range(h):
            for x in range(w):
                i = y * w + x
                if img[i] is None:
                    c = coarse[min(ch - 1, y // 2) * cw + min(cw - 1, x // 2)]
                    img[i] = list(c) if c is not None else [40, 40, 48]
    with open(os.path.join(a["out"], "panorama.ppm"), "wb") as f:
        f.write(b"P6 %d %d 255\n" % (W, H))
        f.write(bytes(int(max(0, min(255, round(v)))) for c in acc for v in c))
    with open(os.path.join(a["out"], "frame.txt"), "w") as f:
        f.write("up %s\nforward %s\nright %s\neye_height %.3f\ncentre_raised %.3f\nmax_radius %.2f\npoints %d\n" %
                (up, fwd, right, eye, shift, max_r, len(kept)))
    print("panorama", W, "x", H)

if __name__ == "__main__":
    main()
