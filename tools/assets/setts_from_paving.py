"""Veilmouth's carriageway setts from Modular Victorian City's coursed paving (reference plan R3 step 3).

Run headless: blender -b --factory-startup --python tools/assets/setts_from_paving.py

Reads the pack's `Paving stones_Base_color.png` and `Paving stones_Mixed_AO.png` and writes two project textures
under `unity/Assets/Art/Streets/`:

- `Setts_Victorian_d.png`: the stones greyed toward the city's cool sett grey (R3 step 3: a mean of 0.34 in sRGB,
  saturation at most about 15 %), the moss in the joints replaced by soot and grit, and the AO's cavities darkened in.
  Its alpha is the smoothness URP's TerrainLit reads from the diffuse: worn stone tops 0.50, joints 0.18, so the
  setts catch the gas lamps and a low sun as the reference's damp streets do.
- `Setts_Victorian_n.png`: a tangent-space normal (OpenGL, +Y up) from the AO, the stones raised between their joints.

Every filter wraps round the edges, so both stay tileable. The numbers are authoring defaults.
"""
import bpy
import numpy as np

SRC = r"D:/Hobby/game-dev/game-dev/unity/Assets/Modular Victorian City/Materials/Textures/"
OUT = r"D:/Hobby/game-dev/game-dev/unity/Assets/Art/Streets/"

TARGET_MEAN = 0.34      # sRGB, the carriageway's cool grey (R3 step 3)
STONE_SAT = 0.15        # the most saturation a stone keeps
JOINT = np.array([0.165, 0.170, 0.178])  # soot and grit between the stones, sRGB
COOL = np.array([0.96, 1.0, 1.08])       # a cool cast over the whole
SMOOTH_STONE, SMOOTH_JOINT = 0.50, 0.18
NORMAL_STRENGTH = 3.0
PREVIEW = None  # a folder for an opaque preview of the colour, or None


def load(name):
    im = bpy.data.images.load(SRC + name)
    w, h = im.size
    px = np.array(im.pixels[:], dtype=np.float32).reshape(h, w, 4)
    return px


def blur(a, r):
    """A wrapped box blur of radius r, three passes (near Gaussian)."""
    for _ in range(3):
        acc = np.zeros_like(a)
        for d in range(-r, r + 1):
            acc += np.roll(a, d, axis=0)
        a = acc / (2 * r + 1)
        acc = np.zeros_like(a)
        for d in range(-r, r + 1):
            acc += np.roll(a, d, axis=1)
        a = acc / (2 * r + 1)
    return a


def smoothstep(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def save(px, name, srgb, folder=None):
    h, w, _ = px.shape
    im = bpy.data.images.new(name, w, h, alpha=True)
    im.colorspace_settings.name = "sRGB" if srgb else "Non-Color"
    im.pixels = np.clip(px, 0.0, 1.0).ravel()
    im.filepath_raw = (folder or OUT) + name + ".png"
    im.file_format = "PNG"
    im.save()


base = load("Paving stones_Base_color.png")[..., :3]
ao = load("Paving stones_Mixed_AO.png")[..., 0]

# The stones by the AO: white tops, grey joints.
stone = smoothstep(0.78, 0.92, blur(ao, 1))

# Moss: yellow-green and saturated, wherever it grows.
mx, mn = base.max(axis=2), base.min(axis=2)
sat = np.where(mx > 1e-4, (mx - mn) / np.maximum(mx, 1e-4), 0.0)
green = (base[..., 1] > base[..., 0] * 1.02) & (base[..., 1] > base[..., 2] * 1.08)
moss = blur(np.where(green, smoothstep(0.12, 0.30, sat), 0.0).astype(np.float32), 1)

lum = base @ np.array([0.2126, 0.7152, 0.0722], dtype=np.float32)
# Stones keep their own light and dark, a little of their warmth, and none of the moss.
grey = lum[..., None]
stone_col = grey + (base - grey) * np.minimum(1.0, STONE_SAT / np.maximum(sat, 1e-4))[..., None]
stone_col = stone_col * (1.0 - moss[..., None]) + grey * 0.8 * moss[..., None]
# Joints: soot with the grit's own variation.
grit = (lum / max(lum.mean(), 1e-4))[..., None]
joint_col = JOINT * (0.6 + 0.4 * grit)
col = stone_col * stone[..., None] + joint_col * (1.0 - stone[..., None])
col *= (0.75 + 0.25 * ao)[..., None]

# The stones' value so the whole meets the target mean.
stone_mean = (col * stone[..., None]).sum(axis=(0, 1)) / max(stone.sum(), 1.0)
joint_share = 1.0 - stone.mean()
want_stone = (TARGET_MEAN - JOINT.mean() * joint_share) / max(stone.mean(), 1e-4)
scale = want_stone / max(stone_mean.mean(), 1e-4)
col = col * (1.0 + (scale - 1.0) * stone[..., None])
col *= COOL
col *= TARGET_MEAN / max(float((col @ np.array([0.2126, 0.7152, 0.0722], dtype=np.float32)).mean()), 1e-4)
print("stone share %.2f, scale %.3f, mean sRGB %s" % (stone.mean(), scale, col.reshape(-1, 3).mean(axis=0)))

smooth = SMOOTH_JOINT + (SMOOTH_STONE - SMOOTH_JOINT) * stone
smooth *= 0.9 + 0.1 * blur(ao, 2) / max(ao.max(), 1e-4)
save(np.dstack([col, smooth]), "Setts_Victorian_d", True)
if PREVIEW:
    save(np.dstack([col, np.ones_like(smooth)]), "setts_preview", True, PREVIEW)

# Normal: the AO as height, raised stones, rounded edges.
height = blur(ao, 2) * 0.8 + blur(stone, 3) * 0.2
du = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) * 0.5
dv = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) * 0.5
n = np.dstack([-du * NORMAL_STRENGTH * 4.0, -dv * NORMAL_STRENGTH * 4.0, np.ones_like(du)])
n /= np.linalg.norm(n, axis=2, keepdims=True)
save(np.dstack([n * 0.5 + 0.5, np.ones_like(du)]), "Setts_Victorian_n", False)
print("written", OUT)
