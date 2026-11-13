"""The 3D preview of one island concept, at true scale: the ground, the sea, every building as a box, the forest as cones.

blender -b --factory-startup -noaudio --python-exit-code 1 --python tools/slice/island/preview_blender.py -- <concept folder>
Reads preview.npz, views.json and texture.png as build.py wrote them; writes preview_<view>.png beside them.
Blender's x is the plan's px, its y is minus py, its z is metres over Sea Level. No vertical exaggeration.
"""
import json
import math
import os
import sys

import bpy
import numpy as np
from mathutils import Vector

out = sys.argv[sys.argv.index("--") + 1]
data = np.load(os.path.join(out, "preview.npz"))
H, boxes, trees, views = data["H"], data["boxes"], data["trees"], data["views"]
names = json.load(open(os.path.join(out, "views.json"), encoding="utf-8"))
SIZE = 1000.0
n = H.shape[0]

for obj in list(bpy.data.objects):
    bpy.data.objects.remove(obj, do_unlink=True)


def material(name, colour, rough=0.9, image=None, alpha=1.0):
    m = bpy.data.materials.new(name)
    try:
        m.use_nodes = True
    except Exception:
        pass
    bsdf = next(nd for nd in m.node_tree.nodes if nd.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Base Color"].default_value = (*colour, 1.0)
    bsdf.inputs["Roughness"].default_value = rough
    if alpha < 1.0:
        bsdf.inputs["Alpha"].default_value = alpha
    if image:
        tex = m.node_tree.nodes.new("ShaderNodeTexImage")
        tex.image = bpy.data.images.load(image)
        m.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    return m


def add(name, verts, faces, mat):
    mesh = bpy.data.meshes.new(name)
    mesh.from_pydata(verts, [], faces)
    mesh.update()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    mesh.materials.append(mat)
    return obj


# The ground: one quad for each cell of the height field, the plan as its texture.
ax = np.linspace(0.0, SIZE, n)
gx, gy = np.meshgrid(ax, ax)
verts = np.stack([gx.ravel(), -gy.ravel(), H.ravel()], 1)
i, j = np.meshgrid(np.arange(n - 1), np.arange(n - 1))
a = (j * n + i).ravel()
faces = np.stack([a, a + n, a + n + 1, a + 1], 1)
ground = add("Ground", verts.tolist(), faces.tolist(), material("Ground", (0.5, 0.5, 0.5), 0.95, os.path.join(out, "texture.png")))
mesh = ground.data
loops = np.zeros(len(mesh.loops), np.int32)
mesh.loops.foreach_get("vertex_index", loops)
uv = np.stack([verts[loops, 0] / SIZE, 1.0 + verts[loops, 1] / SIZE], 1).astype(np.float32)
mesh.uv_layers.new(name="plan").data.foreach_set("uv", uv.ravel())
for poly in mesh.polygons:
    poly.use_smooth = True

# The sea: one sheet at Sea Level, far past the terrain, as the ocean will lie.
sea = add("Sea", [(-3000, 3000, 0), (4000, 3000, 0), (4000, -4000, 0), (-3000, -4000, 0)], [(0, 1, 2, 3)],
          material("Sea", (0.03, 0.10, 0.14), 0.12, alpha=0.9))
# The sea bed beyond the terrain, so the water has one depth of colour to the horizon.
add("Sea floor", [(-3000, 3000, -16), (4000, 3000, -16), (4000, -4000, -16), (-3000, -4000, -16)], [(0, 1, 2, 3)],
    material("Floor", (0.16, 0.24, 0.30), 1.0))

# Buildings: a box on each plot, its height the family's; civic stone, works soot and the rest brick, slate on top.
kinds = [material("Brick", (0.33, 0.20, 0.16)), material("Stone", (0.50, 0.47, 0.42)), material("Soot", (0.16, 0.15, 0.15))]
slate = material("Slate", (0.13, 0.14, 0.16), 0.7)
for kind in range(3):
    v, f, top = [], [], []
    for b in boxes[boxes[:, 10] == kind]:
        q, base, height = b[:8].reshape(4, 2), float(b[8]), float(b[9])
        o = len(v)
        v += [(float(x), -float(y), base - 1.5) for x, y in q] + [(float(x), -float(y), base + height) for x, y in q]
        f += [(o, o + 1, o + 5, o + 4), (o + 1, o + 2, o + 6, o + 5), (o + 2, o + 3, o + 7, o + 6), (o + 3, o, o + 4, o + 7)]
        top.append((o + 4, o + 5, o + 6, o + 7))
    if v:
        obj = add("Buildings %d" % kind, v, f + top, kinds[kind])
        obj.data.materials.append(slate)
        for poly in obj.data.polygons[len(f):]:
            poly.material_index = 1

# The forest: a cone for each tree the plan marks, 9 m high.
v, f = [], []
rng = np.random.default_rng(5)
for x, y, z in trees:
    o = len(v)
    r, h = 3.2 + rng.random() * 1.4, 7.5 + rng.random() * 4.0
    x, y = float(x) + rng.random() * 5 - 2.5, float(y) + rng.random() * 5 - 2.5
    v += [(x - r, -y - r, z), (x + r, -y - r, z), (x + r, -y + r, z), (x - r, -y + r, z), (x, -y, z + h)]
    f += [(o, o + 1, o + 4), (o + 1, o + 2, o + 4), (o + 2, o + 3, o + 4), (o + 3, o, o + 4)]
if v:
    add("Forest", v, f, material("Leaves", (0.07, 0.15, 0.08)))

scene = bpy.context.scene
world = bpy.data.worlds.new("Sky")
scene.world = world
try:
    world.use_nodes = True
except Exception:
    pass
world.node_tree.nodes["Background"].inputs[0].default_value = (0.62, 0.70, 0.80, 1.0)
world.node_tree.nodes["Background"].inputs[1].default_value = 0.9
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
sun.data.energy = 3.2
sun.data.angle = math.radians(3.0)
sun.rotation_euler = (math.radians(58.0), 0.0, math.radians(215.0))
scene.collection.objects.link(sun)

scene.render.engine = "CYCLES"
scene.cycles.samples = 40
scene.cycles.use_denoising = True
scene.render.resolution_x, scene.render.resolution_y = 1600, 900
scene.render.image_settings.file_format = "PNG"

cam = bpy.data.objects.new("Camera", bpy.data.cameras.new("Camera"))
scene.collection.objects.link(cam)
scene.camera = cam
cam.data.clip_end = 6000.0
for name, view in zip(names, views):
    eye = Vector((float(view[0]), -float(view[1]), float(view[2])))
    look = Vector((float(view[3]), -float(view[4]), float(view[5])))
    cam.location = eye
    cam.rotation_euler = (look - eye).to_track_quat("-Z", "Y").to_euler()
    cam.data.lens = float(view[6])
    scene.render.filepath = os.path.join(out, "preview_%s.png" % name)
    bpy.ops.render.render(write_still=True)
    print("PREVIEW", scene.render.filepath, flush=True)
