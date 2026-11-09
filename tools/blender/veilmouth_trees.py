"""
Veilmouth's street and garden trees, grown in Blender with MTree and written for Unity (VEILMOUTH_CITY_PLAN.md §27).

Run headless, from the repository root:

    blender -b --factory-startup -noaudio --python-exit-code 1 --python tools/blender/veilmouth_trees.py -- \
        --out unity/Assets/Art/Trees [--sheet <png>] [--only StreetHornbeamA] [--cards-only] [--seeds 8]

Two Blender add-ons must be installed: MTree (Modular Tree 5.6, GPL-3) grows the branches, and Easy Tree only lends its
CC0 textures from ambientCG (Bark001, LeafSet024 for the beech, LeafSet014 for the hornbeam). Neither's code is shipped.

1. Cluster card (§27.5 step 1). A flat spray of photographed leaves on short shoots, as a beech or a hornbeam carries
   them, facing the viewer, rendered by Cycles into a card of colour with alpha and of normals (OpenGL, +Y up). The
   card's corners are cut where it is empty, so the card is an octagon.
2. Skeleton (steps 2-3). MTree's trunk and three orders of branches; the limbs' lengths follow a crown envelope.
3. Leaves (step 4). One card on each point of a blue-noise set over the thinnest branches, kept in the crown's outer
   shell with a few inside, each pointing outward along its branch. The twigs' bark under the cards is removed.
4. Bark decimated to a budget; the cards' normals turned outward from the crown's middle, so a crown shades as a
   whole; wind weights in the vertex colour (linear), read by GameDev/Slice/Tree:
     R  sway: (height / tree height) ^ 1.8, the same for bark and cards, so a card never leaves its branch;
     G  flutter: 0 on the bark, 0 at a card's foot to 1 at its top;
     B  phase: one random value for each card, from its foot.
5. LOD1: about 45 % of the cards, each 1.3 times larger, on bark decimated further.
6. FBX: one file per tree, objects <Name>_LOD0 and <Name>_LOD1, metres, +Y up, materials by name only.

Every size and count here is an authoring default unless it says measured.
"""

import argparse
import importlib
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Matrix, Vector

# --------------------------------------------------------------------------------------------- the trees

# A kind of tree: its trunk and three orders of branches as MTree's inputs (§27.5), its cards and its bark budget.
# A street tree keeps a leader to 0.8 of its height with many short limbs round it, an upright oval; a garden tree's
# trunk ends halfway up and its limbs rise to make the crown's top, a broad dome (decurrent). Tried on 2026-11-09 and
# dropped: a trunk to the top with drooping limbs (reads as a conifer), and a street tree whose few long limbs make
# its top (the top leans to one side). Lengths are metres before the tree is scaled to its height, and a limb's length
# is then fitted to the tree's silhouette (grow); radii are the trunk's in metres and, for a branch, its share of its
# parent's radius where it starts. The crown's envelope is the shape of the kind's list nearest the tree's silhouette;
# MTree measures it from the top: Conical and Hemispherical are widest at their foot, InverseConical at its top
# (measured on the skeletons).
TRUNK = dict(length=0.55, radius=0.19, end_radius=0.05, shape=0.7, up=0.7, randomness=0.3)  # length: share of height

B2 = dict(start=0.15, end=0.95, length=1.2, density=5.0, angle=50, randomness=0.25, flatness=0.2, up=0.2,
          gravity=3.0, stiffness=0.2, split=0.3, split_angle=25, brk=0.03, radius=0.5, resolution=2)
B3 = dict(start=0.2, end=1.0, length=0.35, density=8.0, angle=50, randomness=0.35, flatness=0.2, up=0.1,
          gravity=2.0, stiffness=0.1, split=0.0, split_angle=25, brk=0.05, radius=0.5, resolution=1.5)

KINDS = {
    "street": dict(
        trunk=dict(radius=0.19, length=0.8, end_radius=0.025),
        b1=dict(start=0.3, end=1.0, length=2.4, density=4.5, angle=48, randomness=0.15, flatness=0.1, up=0.4,
                gravity=2.0, stiffness=0.5, split=0.4, split_angle=25, brk=0.0, radius=0.45, resolution=3,
                spread=10.0),
        b2=dict(B2, length=0.9, density=6.0), b3=dict(B3, length=0.3), shapes=("Spherical", "TendFlame"),
        cards=330, card=0.8, bark_tris=2600),
    "garden": dict(
        trunk=dict(radius=0.22, length=0.5),
        b1=dict(start=0.4, end=1.0, length=6.0, density=2.5, angle=45, randomness=0.15, flatness=0.2, up=0.5,
                gravity=2.0, stiffness=0.5, split=0.5, split_angle=25, brk=0.0, radius=0.45, resolution=3,
                spread=5.0),
        b2=dict(B2, length=1.5), b3=B3, shapes=("Spherical", "Hemispherical", "Conical"),
        cards=610, card=1.0, bark_tris=3400),
}

# Each tree's silhouette to keep (measured on the Easy Tree versions of 2026-11-09): the crown's foot and top, m, and
# its radius in eight bands from foot to top (the 90th percentile of the leaves' distance from the trunk), m. The
# garden beech was rebuilt as a plain round crown, so it keeps only its size.
TREES = [
    dict(name="StreetHornbeamA", kind="street", leaf="Hornbeam", height=9.0, seed=11,
         target=(2.12, 9.0, [0.74, 0.94, 1.38, 1.58, 1.63, 1.45, 1.63, 1.63])),
    dict(name="StreetHornbeamB", kind="street", leaf="Hornbeam", height=8.6, seed=37,
         target=(2.19, 8.6, [0.70, 1.00, 1.38, 1.35, 1.52, 1.81, 1.85, 1.61])),
    dict(name="StreetBeechA", kind="street", leaf="Beech", height=8.4, seed=5,
         target=(2.69, 8.4, [0.74, 1.33, 1.89, 1.85, 1.78, 1.91, 1.87, 1.45])),
    dict(name="StreetBeechB", kind="street", leaf="Beech", height=8.0, seed=71,
         target=(2.47, 8.0, [0.63, 1.52, 1.72, 1.67, 1.79, 1.91, 1.53, 1.22])),
    dict(name="GardenBeechA", kind="garden", leaf="Beech", height=10.0, seed=23,
         target=(2.9, 10.0, [2.4, 3.0, 3.2, 3.2, 3.1, 2.8, 2.4, 1.7])),
    dict(name="GardenHornbeamB", kind="garden", leaf="Hornbeam", height=9.6, seed=89,
         target=(3.20, 9.6, [3.17, 3.05, 2.98, 2.89, 2.81, 2.61, 2.39, 1.98])),
]

# The leaves: the ambientCG atlas, its grid of leaves (columns, rows), and a leaf's length with its stalk, m.
LEAVES = {
    "Beech": dict(atlas="LeafSet024", grid=(3, 3), length=0.10, step=0.042),
    "Hornbeam": dict(atlas="LeafSet014", grid=(3, 2), length=0.11, step=0.034),
}

# A spray: the main shoot's length, m, and its side shoots in pairs: where on the main shoot (share of its length),
# their angle from it, degrees, and their length, m; leaves every step m (LEAVES) along a shoot, left and right in turn.
# The side shoots are longest in the middle, so a spray is an oval: a pointed spray made the crown's outline spiky.
SPRAY_MAIN = 0.50
SPRAY_SIDES = [(0.06, 62, 0.12), (0.19, 56, 0.18), (0.32, 50, 0.21), (0.46, 45, 0.21), (0.60, 40, 0.17), (0.74, 34, 0.11)]
LEAF_ANGLE = 50.0  # degrees between a leaf and its shoot

MAX_RADIUS = 0.015  # m: cards sit on branches thinner than this (measured on the skeletons: the third order and the
                    # second order's tips)
BARK_CUT = 0.012    # m: bark thinner than this is removed: the cards stand where it was
BARK_ENVELOPE = 1.15  # cards stay within the crown's own smoothed outline, and branches thinner than 2 cm within it
                      # times this, so no bare twig sticks out
SHELL = 1.2         # m: the outer shell the cards keep to
INNER_KEEP = 0.15   # the share of the cards set deeper in the crown, for depth
CANDIDATES = 400.0  # candidate points per m² of thin bark

CARD_SIZE = (1024, 1024)  # px
LOD1_KEEP = 0.45
LOD1_GROW = 1.3
LOD1_BARK = 0.35
LOD1_CUT = 0.025  # m: LOD1's bark keeps branches from this radius, under its larger cards (decimating LOD0's bark to
                  # 35 % stalled at 60-80 % on its many cut twigs: measured 2026-11-09)
SWAY_POWER = 1.8
BARK = "VeilmouthBark"
CARD_TEXTURE = "Tree_Twig_{leaf}"


def leaves_material(leaf):
    return f"VeilmouthLeaves_{leaf}"


# --------------------------------------------------------------------------------------------- add-ons

def addon(suffix):
    import addon_utils
    for module in addon_utils.modules():
        if module.__name__.endswith(suffix):
            return module
    raise RuntimeError(f"the Blender add-on '{suffix}' is not installed")


def mtree():
    """MTree's C++ module and its mesh builder, from the installed extension."""
    import addon_utils
    module = addon("modular_tree")
    addon_utils.enable(module.__name__, default_set=False, persistent=False)
    wrapper = importlib.import_module(module.__name__ + ".python_classes.m_tree_wrapper")
    mesh_utils = importlib.import_module(module.__name__ + ".python_classes.mesh_utils")
    return wrapper.lazy_m_tree, mesh_utils.create_mesh_from_cpp


def textures_folder():
    """Easy Tree's CC0 textures: ambientCG's bark and leaf sets."""
    return os.path.join(os.path.dirname(addon("easy_tree").__file__), "assets", "textures")


def copy_bark(textures, textures_out):
    """ambientCG Bark001 at half its size, 512 x 1024, as the project's bark."""
    for source, target in (("Bark001_2K-JPG_Color.jpg", "Tree_Bark.jpg"), ("Bark001_2K-JPG_NormalGL.jpg", "Tree_Bark_Normal.jpg")):
        image = bpy.data.images.load(os.path.join(textures, source))
        if target.endswith("_Normal.jpg"):
            image.colorspace_settings.name = 'Non-Color'
        image.scale(image.size[0] // 2, image.size[1] // 2)
        image.file_format = 'JPEG'
        image.save(filepath=os.path.join(textures_out, target), quality=90)
        bpy.data.images.remove(image)


def apply_modifiers(obj):
    with bpy.context.temp_override(object=obj, active_object=obj, selected_objects=[obj], selected_editable_objects=[obj]):
        for mod in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=mod.name)


def new_object(name, mesh=None):
    obj = bpy.data.objects.new(name, mesh or bpy.data.meshes.new(name))
    bpy.context.scene.collection.objects.link(obj)
    return obj


def remove(obj):
    mesh = obj.data if obj.type == 'MESH' else None
    bpy.data.objects.remove(obj, do_unlink=True)
    if mesh is not None and mesh.users == 0:
        bpy.data.meshes.remove(mesh)


def material(name):
    return bpy.data.materials.get(name) or bpy.data.materials.new(name)


# --------------------------------------------------------------------------------------------- the cluster card

def leaf_material(leaf, textures):
    """A photographed leaf: colour, alpha from the opacity map, the normal map, and a per-leaf brightness."""
    spec = LEAVES[leaf]
    mat = material(f"Leaf_{leaf}")
    mat.use_nodes = True
    nodes, links = mat.node_tree.nodes, mat.node_tree.links
    bsdf = next(n for n in nodes if n.bl_idname == 'ShaderNodeBsdfPrincipled')

    def image(kind, data):
        tex = nodes.new('ShaderNodeTexImage')
        tex.image = bpy.data.images.load(os.path.join(textures, f"{spec['atlas']}_1K-JPG_{kind}.jpg"), check_existing=True)
        if data:
            tex.image.colorspace_settings.name = 'Non-Color'
        return tex

    colour = image("Color", False)
    shade = nodes.new('ShaderNodeAttribute')
    shade.attribute_type = 'GEOMETRY'
    shade.attribute_name = "shade"
    tint = nodes.new('ShaderNodeVectorMath')
    tint.operation = 'SCALE'
    links.new(colour.outputs["Color"], tint.inputs[0])
    links.new(shade.outputs["Fac"], tint.inputs["Scale"])
    links.new(tint.outputs[0], bsdf.inputs["Base Color"])
    links.new(image("Opacity", True).outputs["Color"], bsdf.inputs["Alpha"])
    nmap = nodes.new('ShaderNodeNormalMap')
    links.new(image("NormalGL", True).outputs["Color"], nmap.inputs["Color"])
    links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])
    return mat


def stem_material():
    mat = material("Leaf_Stem")
    mat.use_nodes = True
    bsdf = next(n for n in mat.node_tree.nodes if n.bl_idname == 'ShaderNodeBsdfPrincipled')
    bsdf.inputs["Base Color"].default_value = (0.20, 0.14, 0.10, 1.0)
    return mat


def spray_object(leaf, textures, rng):
    """
    A flat spray in the card's plane (X across, Z up, the viewer at -Y): a main shoot with side shoots in pairs, each
    with leaves alternately left and right, the leaves in front of the shoots and each a little turned and tilted.
    """
    spec = LEAVES[leaf]
    cols, rows = spec["grid"]
    cell_w, cell_h = 1.0 / cols, 1.0 / rows
    leaf_h = spec["length"] / 0.92           # the cell's height in metres: the leaf with its stalk fills 92 % of it
    leaf_w = leaf_h * (cell_w / cell_h)      # the atlases are square, so a cell's shape is its grid's
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    shade = bm.faces.layers.float.new("shade")

    def shoot_points(start, angle, length):
        """A shoot as a gently curving line: its points every 2 cm."""
        points, heading = [Vector(start)], math.radians(angle)
        steps = max(2, int(length / 0.02))
        for _ in range(steps):
            heading += math.radians(rng.uniform(-2.0, 2.0)) - 0.002 * math.copysign(1.0, heading - math.pi / 2)
            points.append(points[-1] + Vector((math.cos(heading), math.sin(heading))) * (length / steps))
        return points

    def stem(points, radius):
        for a, b in zip(points, points[1:]):
            d = (b - a).normalized()
            side = Vector((-d.y, d.x)) * radius
            quad = [bm.verts.new((p.x, 0.003, p.y)) for p in (a - side, a + side, b + side, b - side)]
            face = bm.faces.new(quad)
            face.material_index = 1
            face[shade] = 1.0

    def add_leaf(base, direction, size):
        """A leaf on its stalk's foot at base, pointing along direction (in the card's plane), with its atlas cell."""
        c, r = rng.randrange(cols), rng.randrange(rows)
        u0, v0 = c * cell_w, 1.0 - (r + 1) * cell_h
        w, h = leaf_w * size, leaf_h * size
        heading = math.atan2(direction.y, direction.x) - math.pi / 2
        roll = math.radians(rng.uniform(-22.0, 22.0))   # about the leaf's midrib
        pitch = math.radians(rng.uniform(-18.0, 18.0))  # about its width
        local = Matrix.Rotation(heading, 4, 'Y').inverted() @ Matrix.Rotation(pitch, 4, 'X') @ Matrix.Rotation(roll, 4, 'Z')
        depth = -0.004 - rng.uniform(0.0, 0.03)
        corners = [(-w / 2, 0.0), (w / 2, 0.0), (w / 2, h), (-w / 2, h)]
        verts = []
        for x, z in corners:
            p = local @ Vector((x, 0.0, z - 0.02 * h))
            verts.append(bm.verts.new((base.x + p.x, depth + p.y, base.y + p.z)))
        face = bm.faces.new(verts)
        face.material_index = 0
        face[shade] = rng.uniform(0.82, 1.08)
        for loop, (cu, cv) in zip(face.loops, [(0, 0), (1, 0), (1, 1), (0, 1)]):
            # 1 % in from the cell's sides, so no sliver of the next leaf shows.
            loop[uv].uv = (u0 + (0.01 + 0.98 * cu) * cell_w, v0 + (0.01 + 0.98 * cv) * cell_h)

    def leaves_along(points, length, first):
        """Leaves every step m along a shoot, alternately left and right, largest in its middle."""
        side, s = 1 if first else -1, 0.025
        while s < length:
            k = min(len(points) - 2, int(s / length * (len(points) - 1)))
            a, b = points[k], points[k + 1]
            d = (b - a).normalized()
            out = Vector((-d.y, d.x)) * side
            direction = (d * math.cos(math.radians(LEAF_ANGLE)) + out * math.sin(math.radians(LEAF_ANGLE))).normalized()
            size = (0.78 + 0.32 * math.sin(math.pi * min(1.0, s / length))) * rng.uniform(0.9, 1.08)
            add_leaf(a + (b - a) * 0.5, direction, size)
            side, s = -side, s + spec["step"] * rng.uniform(0.85, 1.15)
        tip = points[-1]
        add_leaf(tip, (points[-1] - points[-2]).normalized(), 0.9)

    main = shoot_points((0.0, 0.0), 90.0 + rng.uniform(-3.0, 3.0), SPRAY_MAIN)
    stem(main, 0.0025)
    leaves_along(main[4:], SPRAY_MAIN * (1.0 - 4 / (len(main) - 1)), True)
    for where, angle, length in SPRAY_SIDES:
        for side in (1, -1):
            at = where + (0.04 if side < 0 else 0.0)
            k = min(len(main) - 1, int(at * (len(main) - 1)))
            heading = 90.0 - side * angle + rng.uniform(-4.0, 4.0)
            points = shoot_points(main[k], heading, length * rng.uniform(0.9, 1.1))
            stem(points, 0.0017)
            leaves_along(points, length, side > 0)
    mesh = bpy.data.meshes.new(f"Spray_{leaf}")
    bm.to_mesh(mesh)
    bm.free()
    mesh.materials.append(leaf_material(leaf, textures))
    mesh.materials.append(stem_material())
    return new_object(f"Spray_{leaf}", mesh)


def unlit(material, normals):
    """A copy of the material that emits its base colour, or its normal in camera space, through its own alpha."""
    copy = material.copy()
    copy.name = f"{material.name}_{'normal' if normals else 'albedo'}"
    nodes, links = copy.node_tree.nodes, copy.node_tree.links
    bsdf = next(n for n in nodes if n.bl_idname == 'ShaderNodeBsdfPrincipled')
    output = next(n for n in nodes if n.bl_idname == 'ShaderNodeOutputMaterial' and n.is_active_output)
    emit = nodes.new('ShaderNodeEmission')
    if normals:
        # Cycles already turns a back face's normal toward the ray, so a leaf seen from behind needs no flip
        # (measured: flipping it again gave a mean z of 0.45 instead of above 0.6).
        to_camera = nodes.new('ShaderNodeVectorTransform')
        to_camera.vector_type = 'NORMAL'
        to_camera.convert_from = 'WORLD'
        to_camera.convert_to = 'CAMERA'
        if bsdf.inputs["Normal"].is_linked:
            links.new(bsdf.inputs["Normal"].links[0].from_socket, to_camera.inputs[0])
        else:
            links.new(nodes.new('ShaderNodeNewGeometry').outputs["Normal"], to_camera.inputs[0])
        encode = nodes.new('ShaderNodeVectorMath')
        encode.operation = 'MULTIPLY_ADD'
        links.new(to_camera.outputs[0], encode.inputs[0])
        # Cycles' camera space is X right, Y up, Z away from the viewer (measured on a sphere), so Z turns over.
        encode.inputs[1].default_value = (0.5, 0.5, -0.5)
        encode.inputs[2].default_value = (0.5, 0.5, 0.5)
        links.new(encode.outputs[0], emit.inputs["Color"])
    else:
        base = bsdf.inputs["Base Color"]
        if base.is_linked:
            links.new(base.links[0].from_socket, emit.inputs["Color"])
        else:
            emit.inputs["Color"].default_value = base.default_value
    alpha = bsdf.inputs["Alpha"]
    mix = nodes.new('ShaderNodeMixShader')
    clear = nodes.new('ShaderNodeBsdfTransparent')
    if alpha.is_linked:
        links.new(alpha.links[0].from_socket, mix.inputs[0])
    else:
        mix.inputs[0].default_value = alpha.default_value
    links.new(clear.outputs[0], mix.inputs[1])
    links.new(emit.outputs[0], mix.inputs[2])
    links.new(mix.outputs[0], output.inputs["Surface"])
    return copy


def render_card(obj, normals, path, frame):
    """Renders the spray front-on (looking along +Y): X across the card, Z up it."""
    scene = bpy.context.scene
    originals = list(obj.data.materials)
    swapped = {m.name: unlit(m, normals) for m in originals if m is not None}
    for slot in obj.material_slots:
        if slot.material is not None:
            slot.material = swapped[slot.material.name]
    camera = scene.camera
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = frame["scale"]
    camera.location = (0.0, -10.0, frame["mid_z"])
    camera.rotation_euler = (math.radians(90.0), 0.0, 0.0)
    scene.render.resolution_x, scene.render.resolution_y = CARD_SIZE
    scene.render.resolution_percentage = 100
    scene.render.film_transparent = True
    scene.render.filter_size = 1.0
    scene.render.image_settings.file_format = 'PNG'
    scene.render.image_settings.color_mode = 'RGBA'
    scene.render.image_settings.color_depth = '8'
    scene.view_settings.view_transform = 'Raw' if normals else 'Standard'
    scene.view_settings.look = 'None'
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    for slot, original in zip(obj.material_slots, originals):
        slot.material = original
    for copy in swapped.values():
        bpy.data.materials.remove(copy)


def card_pixels(path):
    image = bpy.data.images.load(path)
    w, h = image.size
    px = list(image.pixels)
    bpy.data.images.remove(image)
    return w, h, px


def check_normals(path):
    """A card's normals face the viewer: their mean Z, where a leaf is, stays well above 0.5 (the flat value)."""
    w, h, px = card_pixels(path)
    zs = [px[i + 2] for i in range(0, len(px), 4) if px[i + 3] > 0.5]
    mean = sum(zs) / max(1, len(zs))
    print(f"VEILMOUTH normals {os.path.basename(path)}: mean z {mean:.3f} over {len(zs)} px")
    if mean < 0.6:
        raise RuntimeError(f"{path}: the card's normals face away (mean z {mean:.3f})")


def card_outline(path):
    """
    The card's coverage, its stems' share, and the octagon that holds every opaque pixel: each corner cut at 45 degrees
    as far as the card is empty there, less 1.5 % of the side (measured on the albedo's alpha).
    """
    w, h, px = card_pixels(path)
    opaque = stems = 0
    cut = [1.0, 1.0, 1.0, 1.0]  # bottom-left, bottom-right, top-right, top-left, as a share of the side
    for y in range(0, h, 2):
        for x in range(0, w, 2):
            i = 4 * (y * w + x)
            if px[i + 3] <= 0.5:
                continue
            opaque += 1
            stems += px[i] > px[i + 1]
            u, v = x / (w - 1), y / (h - 1)
            for k, d in enumerate((u + v, (1 - u) + v, (1 - u) + (1 - v), u + (1 - v))):
                cut[k] = min(cut[k], d)
    total = (w // 2) * (h // 2)
    cut = [max(0.0, c - 0.015) if c > 0.06 else 0.0 for c in cut]
    return opaque / total, stems / max(1, opaque), cut


def octagon(cut):
    """The card's outline in UV, counter-clockwise from the bottom edge, with a corner left whole where it is not cut."""
    bl, br, tr, tl = cut
    points = []
    for p in ((bl, 0.0), (1.0 - br, 0.0), (1.0, br), (1.0, 1.0 - tr), (1.0 - tr, 1.0), (tl, 1.0), (0.0, 1.0 - tl), (0.0, bl)):
        if not points or (abs(points[-1][0] - p[0]) > 1e-4 or abs(points[-1][1] - p[1]) > 1e-4):
            points.append(p)
    if abs(points[0][0] - points[-1][0]) < 1e-4 and abs(points[0][1] - points[-1][1]) < 1e-4:
        points.pop()
    return points


def bake_card(leaf, textures, textures_out):
    """The cluster card of one kind of leaf: its two textures and its outline, with its size and foot, m."""
    obj = spray_object(leaf, textures, random.Random(f"spray-{leaf}"))
    xs = [v.co.x for v in obj.data.vertices]
    zs = [v.co.z for v in obj.data.vertices]
    half = max(abs(min(xs)), abs(max(xs))) * 1.02
    z0 = min(0.0, min(zs)) - 0.01
    z1 = max(zs) * 1.02
    size = max(z1 - z0, 2.0 * half)
    frame = dict(scale=size, mid_z=z0 + size / 2.0)
    base = os.path.join(textures_out, CARD_TEXTURE.format(leaf=leaf))
    render_card(obj, False, base + ".png", frame)
    render_card(obj, True, base + "_Normal.png", frame)
    check_normals(base + "_Normal.png")
    remove(obj)
    coverage, stems, cut = card_outline(base + ".png")
    outline = octagon(cut)
    print(f"VEILMOUTH card {leaf}: {size:.3f} m square, foot {-z0:.3f} m above its bottom; coverage {coverage:.3f}, "
          f"stems {stems:.3f} of it; corners cut {[round(c, 3) for c in cut]}, {len(outline)} sides")
    return dict(size=size, z0=z0, outline=outline, coverage=coverage, stems=stems)


# --------------------------------------------------------------------------------------------- the skeleton

def const(m, value):
    return m.PropertyWrapper(m.ConstantProperty(float(value)))


def branch_function(m, p, seed):
    f = m.BranchFunction()
    f.seed = seed
    f.distribution.start, f.distribution.end = p["start"], p["end"]
    f.distribution.density = p["density"]
    f.distribution.phillotaxis = 137.5
    f.length = const(m, p["length"])
    f.start_angle = const(m, p["angle"])
    f.randomness = const(m, p["randomness"])
    f.start_radius = const(m, p["radius"])
    f.flatness = p["flatness"]
    f.gravity.up_attraction, f.gravity.strength, f.gravity.stiffness = p["up"], p["gravity"], p["stiffness"]
    f.split.probability, f.split.angle = p["split"], p["split_angle"]
    f.break_chance = p["brk"]
    f.resolution = p["resolution"]
    if "crown" in p:
        f.crown.shape = getattr(m.CrownShape, p["crown"])
        f.crown.angle_variation = p["spread"]
    return f


def skeleton(m, create_mesh, spec, seed, reach, shape):
    """The tree's branches as one mesh, scaled to the tree's height, its trunk's foot at the origin."""
    kind = KINDS[spec["kind"]]
    t = dict(TRUNK, **kind.get("trunk", {}))
    trunk = m.TrunkFunction()
    trunk.seed = seed
    trunk.length = spec["height"] * t["length"]
    trunk.start_radius = t["radius"]
    trunk.end_radius = t["end_radius"]
    trunk.shape = t["shape"]
    trunk.up_attraction = t["up"]
    trunk.randomness = t["randomness"]
    trunk.resolution = 3
    parent = trunk
    for k, key in enumerate(("b1", "b2", "b3")):
        if not kind.get(key):
            break
        p = dict(kind[key])
        if key == "b1":
            p["length"] *= reach
            p["crown"] = shape
        child = branch_function(m, p, seed + 1 + k)
        parent.add_child(child)
        parent = child
    tree = m.Tree()
    tree.set_trunk_function(trunk)
    tree.execute_functions()
    mesher = m.ManifoldMesher()
    mesher.radial_n_points = 6
    mesher.smooth_iterations = 1
    mesh = bpy.data.meshes.new(spec["name"])
    create_mesh(mesh, mesher.mesh_tree(tree))
    obj = new_object(spec["name"], mesh)
    top = max(v.co.z for v in mesh.vertices)
    factor = spec["height"] / top
    for v in mesh.vertices:
        v.co *= factor
    radius = mesh.attributes["radius"].data
    for item in radius:
        item.value *= factor
    return obj


def thin_points(obj):
    """The skeleton's thin branches: vertices thinner than MAX_RADIUS, the crown's points before any leaf."""
    me = obj.data
    radius = me.attributes["radius"].data
    return [me.vertices[i].co.copy() for i in range(len(me.vertices)) if radius[i].value < MAX_RADIUS]


def profile(points):
    base, top = min(p.z for p in points), max(p.z for p in points)
    bands = []
    for k in range(8):
        lo, hi = base + (top - base) * k / 8, base + (top - base) * (k + 1) / 8
        r = sorted(math.hypot(p.x, p.y) for p in points if lo <= p.z <= hi)
        bands.append(r[int(0.9 * (len(r) - 1))] if r else 0.0)
    return base, top, bands


def own_envelope(points, bands=8, sectors=12):
    """
    The crown's own outline round its trunk: the 90th percentile radius of its thin branches in 8 height bands and 12
    sectors, smoothed three times round and twice up, so a lobe stays and a single limb reaching out of it does not.
    """
    zs = [p.z for p in points]
    base, top = min(zs), max(zs)
    cells = [[[] for _ in range(sectors)] for _ in range(bands)]
    for p in points:
        k = min(bands - 1, int((p.z - base) / (top - base) * bands))
        s = int((math.atan2(p.y, p.x) + math.pi) / (2 * math.pi) * sectors) % sectors
        cells[k][s].append(math.hypot(p.x, p.y))
    grid = []
    for k in range(bands):
        band = sorted(r for cell in cells[k] for r in cell)
        whole = band[int(0.9 * (len(band) - 1))] if band else 0.0
        grid.append([sorted(c)[int(0.9 * (len(c) - 1))] if len(c) >= 5 else whole for c in cells[k]])
    for _ in range(3):
        grid = [[0.25 * row[s - 1] + 0.5 * row[s] + 0.25 * row[(s + 1) % sectors] for s in range(sectors)] for row in grid]
    for _ in range(2):
        grid = [[0.25 * grid[max(0, k - 1)][s] + 0.5 * grid[k][s] + 0.25 * grid[min(bands - 1, k + 1)][s]
                 for s in range(sectors)] for k in range(bands)]
    return base, top, grid


def own_radius(own, p):
    base, top, grid = own
    bands, sectors = len(grid), len(grid[0])
    t = max(0.0, min(bands - 1.0, (p.z - base) / (top - base) * bands - 0.5))
    a = ((math.atan2(p.y, p.x) + math.pi) / (2 * math.pi) * sectors - 0.5) % sectors
    k, s = min(bands - 2, int(t)), int(a)
    fk, fs = t - k, a - s
    row = lambda kk: grid[kk][s] * (1.0 - fs) + grid[kk][(s + 1) % sectors] * fs
    return row(k) * (1.0 - fk) + row(k + 1) * fk


def crown_limit(own):
    """Whether a point lies within the crown's own smoothed outline times slack (the silhouette chose the skeleton)."""
    def inside(p, slack):
        return math.hypot(p.x, p.y) <= own_radius(own, p) * slack and p.z <= own[1] + 0.3
    return inside


def silhouette_error(points, target):
    """
    How far a crown is from its silhouette: the bands' mean difference and the foot's, plus how far the upper half's
    middle stands off the trunk (a crown leaning to one side reads as a broken tree), each over the silhouette's size.
    """
    base, top, bands = profile(points)
    t_base, t_top, t_bands = target
    mean = sum(t_bands) / len(t_bands)
    upper = [p for p in points if p.z > (base + top) / 2.0]
    lean = math.hypot(sum(p.x for p in upper), sum(p.y for p in upper)) / max(1, len(upper))
    error = sum(abs(a - b) for a, b in zip(bands, t_bands)) / len(t_bands) / mean + abs(base - t_base) / t_top
    return error + lean / mean, bands


# --------------------------------------------------------------------------------------------- the leaves

def face_attribute(me, name, poly):
    data = me.attributes[name].data
    if me.attributes[name].data_type == 'FLOAT':
        return sum(data[v].value for v in poly.vertices) / len(poly.vertices)
    return sum((Vector(data[v].vector) for v in poly.vertices), Vector()) / len(poly.vertices)


def hull_depths(points, queries):
    """How deep each query point lies inside the convex hull of points, m (negative outside)."""
    bm = bmesh.new()
    for p in points:
        bm.verts.new(p)
    bmesh.ops.convex_hull(bm, input=bm.verts)
    planes = []
    for f in bm.faces:
        f.normal_update()
        planes.append((f.normal.copy(), f.calc_center_median().copy()))
    bm.free()
    return [min(-(n.dot(q - c)) for n, c in planes) for q in queries]


def blue_noise(candidates, spacing):
    """The candidates, in their given order, kept where no kept point stands within spacing (a grid of that size)."""
    grid, kept = {}, []
    for index, item in enumerate(candidates):
        p = item[0]
        key = (int(math.floor(p.x / spacing)), int(math.floor(p.y / spacing)), int(math.floor(p.z / spacing)))
        near = False
        for dx in (-1, 0, 1):
            for dy in (-1, 0, 1):
                for dz in (-1, 0, 1):
                    for q in grid.get((key[0] + dx, key[1] + dy, key[2] + dz), ()):
                        if (q - p).length < spacing:
                            near = True
                            break
                    if near:
                        break
                if near:
                    break
            if near:
                break
        if not near:
            grid.setdefault(key, []).append(p)
            kept.append(index)
    return kept


def crown_ellipsoid(points):
    """The crown as an ellipsoid round the trunk's axis: its middle, its half width (90th percentile) and half height."""
    zs = [p.z for p in points]
    centre = Vector((0.0, 0.0, (min(zs) + max(zs)) / 2.0))
    half_h = max(0.5, (max(zs) - min(zs)) / 2.0)
    half_w = max(0.5, sorted(math.hypot(p.x, p.y) for p in points)[int(0.9 * (len(points) - 1))])
    return centre, half_w, half_h


def outward(p, crown):
    """The crown's outward direction at p (the ellipsoid's gradient) and p carried out to the ellipsoid's surface."""
    centre, half_w, half_h = crown
    d = p - centre
    scaled = Vector((d.x / half_w, d.y / half_w, d.z / half_h))
    if scaled.length < 1e-6:
        return Vector((0.0, 0.0, 1.0)), centre + Vector((0.0, 0.0, half_h))
    gradient = Vector((d.x / half_w ** 2, d.y / half_w ** 2, d.z / half_h ** 2)).normalized()
    unit = scaled.normalized()
    return gradient, centre + Vector((unit.x * half_w, unit.y * half_w, unit.z * half_h))


def place_cards(obj, spec, inside, rng):
    """
    The cards over the crown: candidate points on the thin bark (area-weighted) within the silhouette; those in the
    outer shell thinned to blue noise, so the shell is covered evenly wherever twigs reach, and a few deeper ones the
    same way. The spacing is found for the kind's card count. Returns (point, branch direction) pairs and the crown's
    ellipsoid. (Thinning over the crown's surface instead - each point carried out to the ellipsoid - left a limb
    reaching past the ellipsoid with a few scattered cards: tried 2026-11-09.)
    """
    me = obj.data
    polys = [p for p in me.polygons if face_attribute(me, "radius", p) < MAX_RADIUS]
    areas = [p.area for p in polys]
    total = sum(areas)
    candidates = []
    for poly, area in zip(polys, areas):
        count = area * CANDIDATES
        n = int(count) + (1 if rng.random() < count - int(count) else 0)
        if n == 0:
            continue
        verts = [me.vertices[v].co for v in poly.vertices]
        tangent = face_attribute(me, "direction", poly).normalized()
        for _ in range(n):
            # A random point of the quad (as two triangles).
            a, b, c = (verts[0], verts[1], verts[2]) if rng.random() < 0.5 or len(verts) < 4 else (verts[0], verts[2], verts[3])
            r1, r2 = rng.random(), rng.random()
            if r1 + r2 > 1.0:
                r1, r2 = 1.0 - r1, 1.0 - r2
            point = a + (b - a) * r1 + (c - a) * r2
            if inside(point, 1.0):
                candidates.append((point, tangent))
    points = [p for p, _ in candidates]
    crown = crown_ellipsoid(points)
    depths = hull_depths(points, points)
    shell = [(p, t) for (p, t), d in zip(candidates, depths) if d < SHELL]
    inner = [(p, t) for (p, t), d in zip(candidates, depths) if d >= SHELL]
    rng.shuffle(shell)
    rng.shuffle(inner)
    want = KINDS[spec["kind"]]["cards"]
    want_inner = int(want * INNER_KEEP) if inner else 0

    def thinned(items, count):
        lo, hi = 0.02, 3.0
        for _ in range(20):
            mid = (lo + hi) / 2.0
            if len(blue_noise(items, mid)) > count:
                lo = mid
            else:
                hi = mid
        return [items[i] for i in blue_noise(items, hi)], hi

    outer, spacing = thinned(shell, want - want_inner)
    deep, deep_spacing = thinned(inner, want_inner) if want_inner else ([], 0.0)
    print(f"VEILMOUTH {spec['name']}: thin bark {total:.1f} m², {len(candidates)} candidates in the silhouette, "
          f"{len(shell)} in the shell; {len(outer)} cards {spacing:.3f} m apart there, {len(deep)} inside {deep_spacing:.3f} m apart")
    return outer + deep, crown


def card_mesh(spec, card, placements, crown, rng):
    """
    One octagon a card, its foot on its point, its face outward from the crown and its length along the crown's surface
    (up its branch, a little lifted and tilted out), so a card on the crown's edge lies along the outline instead of
    sticking out of it (a card along its branch and outward, as first tried, made the outline a fringe of fronds).
    """
    size = KINDS[spec["kind"]]["card"]
    bm = bmesh.new()
    uv = bm.loops.layers.uv.new("UVMap")
    for p, tangent in placements:
        out, _ = outward(p, crown)
        along = tangent * 0.5 + Vector((0.0, 0.0, 0.5))
        along = along - out * along.dot(out)
        if along.length < 1e-3:
            along = out.orthogonal()
        along.normalize()
        up = (along + out * math.tan(math.radians(rng.uniform(10.0, 30.0)))).normalized()
        facing = out - up * out.dot(up)
        # Turned up to 35 degrees about its length, so the cards on the crown's edge are not all seen edge-on.
        facing = (Matrix.Rotation(math.radians(rng.uniform(-35.0, 35.0)), 3, up) @ facing).normalized()
        spin = Matrix.Rotation(math.radians(rng.uniform(-25.0, 25.0)), 3, facing)
        up = spin @ up
        right = up.cross(facing).normalized()
        scale = size * rng.uniform(0.85, 1.15) / card["size"]
        verts = []
        for u, v in card["outline"]:
            x = (u - 0.5) * card["size"] * scale
            z = (card["z0"] + v * card["size"]) * scale
            verts.append(bm.verts.new(p + right * x + up * z))
        face = bm.faces.new(verts)
        for loop, (u, v) in zip(face.loops, card["outline"]):
            loop[uv].uv = (u, v)
    return bm


def bark_mesh(obj, inside, thinnest):
    """The skeleton's bark less the twigs thinner than thinnest, m, and the branches under 2 cm outside the crown."""
    me = obj.data
    bm = bmesh.new()
    bm.from_mesh(me)
    layer = bm.verts.layers.float.get("radius")

    def cut(face):
        radius = sum(v[layer] for v in face.verts) / len(face.verts)
        return radius < thinnest or (radius < 0.02 and not inside(face.calc_center_median(), BARK_ENVELOPE))
    drop = [f for f in bm.faces if cut(f)]
    bmesh.ops.delete(bm, geom=drop, context='FACES')
    loose = [v for v in bm.verts if not v.link_faces]
    bmesh.ops.delete(bm, geom=loose, context='VERTS')
    return bm


def decimate(bm, tris, name):
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    obj = new_object(name, mesh)
    count = sum(len(p.vertices) - 2 for p in mesh.polygons)
    if count > tris:
        mod = obj.modifiers.new("Decimate", 'DECIMATE')
        mod.ratio = tris / count
        mod.use_collapse_triangulate = True
        apply_modifiers(obj)
    out = bmesh.new()
    out.from_mesh(obj.data)
    remove(obj)
    return out


def card_foot(points_uv):
    """A card's foot: the middle of its corners on its lowest edge (v within 1 % of its least)."""
    least = min(v for _, v in points_uv)
    bottom = [p for p, v in points_uv if v <= least + 0.01]
    return sum(bottom, Vector()) / len(bottom)


def card_hash(point):
    """A stable value in [0, 1) for a card, from its foot (millimetres)."""
    key = (round(point.x * 1000), round(point.y * 1000), round(point.z * 1000))
    return random.Random(hash(key) & 0xFFFFFFFF).random()


def thin(leaves, keep, grow_by):
    """LOD1's leaves: the cards whose hash falls under keep, each grown about its foot."""
    uv = leaves.loops.layers.uv.active
    drop = []
    for face in leaves.faces:
        foot = card_foot([(l.vert.co.copy(), l[uv].uv.y) for l in face.loops])
        if card_hash(foot) >= keep:
            drop.append(face)
            continue
        for vert in face.verts:
            vert.co = foot + (vert.co - foot) * grow_by
    bmesh.ops.delete(leaves, geom=drop, context='FACES')


def to_mesh(bm, name):
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    return mesh


def assemble(name, bark, leaves, height, leaf):
    """One object: bark (material 0) and cards (material 1), with smooth bark, the crown's normals and the wind colour."""
    bm = bmesh.new()
    bm.from_mesh(to_mesh(bark, name + "_bark"))
    for f in bm.faces:
        f.material_index = 0
        f.smooth = True
    leaf_start = len(bm.faces)
    bm.from_mesh(to_mesh(leaves, name + "_leaves"))
    bm.faces.ensure_lookup_table()
    for f in bm.faces[leaf_start:]:
        f.material_index = 1
        f.smooth = True
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    for attribute in [a.name for a in mesh.attributes if a.name in ("radius", "stem_id", "hierarchy_depth",
                                                                   "branch_extent", "direction", "pivot_position",
                                                                   "vein_distance", "phyllotaxis_angle")]:
        mesh.attributes.remove(mesh.attributes[attribute])
    mesh.materials.append(material(BARK))
    mesh.materials.append(material(leaves_material(leaf)))
    obj = new_object(name, mesh)

    # The cards' normals: from the crown's middle, a little lifted, so a crown is lit as one body.
    uv = mesh.uv_layers["UVMap"].data
    leaf_polys = [p for p in mesh.polygons if p.material_index == 1]
    leaf_points = [mesh.vertices[v].co for p in leaf_polys for v in p.vertices]
    centre = sum(leaf_points, Vector()) / max(1, len(leaf_points))
    normals = [Vector(n.vector) for n in mesh.corner_normals]
    colours = mesh.color_attributes.new("Col", 'FLOAT_COLOR', 'CORNER')
    for poly in mesh.polygons:
        is_leaf = poly.material_index == 1
        if is_leaf:
            foot = card_foot([(mesh.vertices[mesh.loops[l].vertex_index].co.copy(), uv[l].uv.y) for l in poly.loop_indices])
            phase = card_hash(foot)
        for l in poly.loop_indices:
            co = mesh.vertices[mesh.loops[l].vertex_index].co
            sway = max(0.0, min(1.0, co.z / height)) ** SWAY_POWER
            if is_leaf:
                out = (co - centre)
                out.z = out.z * 0.8 + 0.35 * out.length
                normals[l] = out.normalized()
                colours.data[l].color = (sway, uv[l].uv.y, phase, 1.0)
            else:
                colours.data[l].color = (sway, 0.0, 0.0, 1.0)
    mesh.normals_split_custom_set(normals)
    tris = sum(len(p.vertices) - 2 for p in mesh.polygons)
    leaf_tris = sum(len(p.vertices) - 2 for p in leaf_polys)
    print(f"VEILMOUTH {name}: {tris} triangles ({tris - leaf_tris} bark, {leaf_tris} leaves), "
          f"{len(leaf_polys)} cards, {obj.dimensions.x:.2f} x {obj.dimensions.y:.2f} x {obj.dimensions.z:.2f} m")
    return obj


def fit(m, create_mesh, spec, seed, shape):
    """A skeleton of this seed and crown shape whose limbs are lengthened or shortened twice to the silhouette's width."""
    target = spec["target"][2]
    reach = 1.0
    for _ in range(2):
        obj = skeleton(m, create_mesh, spec, seed, reach, shape)
        _, _, bands = profile(thin_points(obj))
        remove(obj)
        reach *= max(0.5, min(1.5, sum(target) / max(1e-3, sum(bands))))
    obj = skeleton(m, create_mesh, spec, seed, reach, shape)
    error, bands = silhouette_error(thin_points(obj), spec["target"])
    return obj, error, reach, bands


def grow(m, create_mesh, spec, seeds):
    """The tree's skeleton: of its kind's crown shapes and its seed and the next seeds - 1, the nearest its silhouette."""
    best = None
    for shape in KINDS[spec["kind"]]["shapes"]:
        for seed in range(spec["seed"], spec["seed"] + seeds):
            obj, error, reach, bands = fit(m, create_mesh, spec, seed, shape)
            remove(obj)
            print(f"VEILMOUTH {spec['name']} {shape} seed {seed}: reach {reach:.2f}, silhouette error {error:.3f}")
            if best is None or error < best[0]:
                best = (error, seed, shape)
    _, seed, shape = best
    obj, error, reach, bands = fit(m, create_mesh, spec, seed, shape)
    print(f"VEILMOUTH {spec['name']}: {shape}, seed {seed}, reach {reach:.3f}, silhouette error {error:.3f}, "
          f"bands {[round(b, 2) for b in bands]} against {spec['target'][2]}")
    return obj


def build_tree(m, create_mesh, spec, cards, seeds):
    rng = random.Random(f"cards-{spec['name']}")
    obj = grow(m, create_mesh, spec, seeds)
    inside = crown_limit(own_envelope(thin_points(obj)))
    placements, crown = place_cards(obj, spec, inside, rng)
    leaves = card_mesh(spec, cards[spec["leaf"]], placements, crown, rng)
    bark = bark_mesh(obj, inside, BARK_CUT)
    bark_far = bark_mesh(obj, inside, LOD1_CUT)
    remove(obj)
    budget = KINDS[spec["kind"]]["bark_tris"]
    bark0 = decimate(bark, budget, spec["name"] + "_b0")
    bark1 = decimate(bark_far, int(budget * LOD1_BARK), spec["name"] + "_b1")
    leaves1 = leaves.copy()
    thin(leaves1, LOD1_KEEP, LOD1_GROW)
    lod0 = assemble(spec["name"] + "_LOD0", bark0, leaves, spec["height"], spec["leaf"])
    lod1 = assemble(spec["name"] + "_LOD1", bark1, leaves1, spec["height"], spec["leaf"])
    for bm in (bark, bark_far, leaves, bark0, bark1, leaves1):
        bm.free()
    return lod0, lod1


# --------------------------------------------------------------------------------------------- export and preview

def export(objects, path):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, object_types={'MESH'}, apply_unit_scale=True,
                             apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y',
                             bake_space_transform=True, use_mesh_modifiers=True, mesh_smooth_type='OFF',
                             use_tspace=False, colors_type='LINEAR', add_leaf_bones=False, bake_anim=False,
                             path_mode='STRIP', embed_textures=False)


def preview_materials(textures_out, bark_textures):
    """Blender materials that look as the Unity ones will, for the preview renders only."""
    def principled(name, colour, normal, alpha):
        mat = material(name)
        mat.use_nodes = True
        nodes, links = mat.node_tree.nodes, mat.node_tree.links
        bsdf = next(n for n in nodes if n.bl_idname == 'ShaderNodeBsdfPrincipled')
        tex = nodes.new('ShaderNodeTexImage')
        tex.image = bpy.data.images.load(colour, check_existing=True)
        links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
        if alpha:
            links.new(tex.outputs["Alpha"], bsdf.inputs["Alpha"])
        ntex = nodes.new('ShaderNodeTexImage')
        ntex.image = bpy.data.images.load(normal, check_existing=True)
        ntex.image.colorspace_settings.name = 'Non-Color'
        nmap = nodes.new('ShaderNodeNormalMap')
        links.new(ntex.outputs["Color"], nmap.inputs["Color"])
        links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])
        bsdf.inputs["Roughness"].default_value = 0.8 if not alpha else 0.6
    principled(BARK, os.path.join(bark_textures, "Bark001_2K-JPG_Color.jpg"),
               os.path.join(bark_textures, "Bark001_2K-JPG_NormalGL.jpg"), False)
    for leaf in LEAVES:
        base = os.path.join(textures_out, CARD_TEXTURE.format(leaf=leaf))
        principled(leaves_material(leaf), base + ".png", base + "_Normal.png", True)


def setup_scene():
    bpy.ops.wm.read_homefile(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = 32
    scene.cycles.use_denoising = False
    cam = bpy.data.objects.new("Camera", bpy.data.cameras.new("Camera"))
    scene.collection.objects.link(cam)
    scene.camera = cam
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", 'SUN'))
    sun.data.energy = 3.5
    sun.rotation_euler = (math.radians(50), math.radians(10), math.radians(-35))
    scene.collection.objects.link(sun)
    world = bpy.data.worlds.new("World")
    world.use_nodes = True
    background = next((n for n in world.node_tree.nodes if n.bl_idname == 'ShaderNodeBackground'), None)
    if background is not None:
        background.inputs["Color"].default_value = (0.45, 0.5, 0.58, 1.0)
    scene.world = world
    return scene


def sheet(objects, path, side=False):
    """Every tree's LOD0 in a row, seen from the side (or from 90 degrees round) with a 1.8 m figure for scale."""
    scene = bpy.context.scene
    figure = bpy.data.objects.get("Figure")
    if figure is None:
        figure = new_object("Figure", bpy.data.meshes.new("Figure"))
        bm = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0)
        for v in bm.verts:
            v.co = Vector((v.co.x * 0.45, v.co.y * 0.3, (v.co.z + 0.5) * 1.8))
        bm.to_mesh(figure.data)
        figure.location = (-4.5, 0.0, 0.0)
    for obj in scene.objects:
        if obj.type == 'MESH':
            obj.hide_render = obj not in objects and obj is not figure
    for obj in objects:
        obj.rotation_euler = (0.0, 0.0, math.radians(90.0) if side else 0.0)
    width = 9.0 * len(objects) + 9.0
    camera = scene.camera
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = width
    camera.location = (width / 2.0 - 9.0, -60.0, 5.5)
    camera.rotation_euler = (math.radians(90.0), 0.0, 0.0)
    scene.render.resolution_x, scene.render.resolution_y = 1800, int(1800 * 12.0 / width)
    scene.render.film_transparent = False
    scene.render.filter_size = 1.5
    scene.view_settings.view_transform = 'AgX'
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", required=True)
    parser.add_argument("--only", help="tree names, comma-separated")
    parser.add_argument("--sheet", help="renders of every tree's LOD0 side by side, front and side")
    parser.add_argument("--cards-only", action="store_true")
    parser.add_argument("--seeds", type=int, default=1, help="seeds tried for each tree, from its own")
    args = parser.parse_args(argv)
    out = os.path.abspath(args.out)
    textures_out = os.path.join(out, "Textures")
    os.makedirs(textures_out, exist_ok=True)

    setup_scene()
    textures = textures_folder()
    copy_bark(textures, textures_out)
    cards = {leaf: bake_card(leaf, textures, textures_out) for leaf in LEAVES}
    if args.cards_only:
        print("VEILMOUTH done")
        return
    m, create_mesh = mtree()
    if args.sheet:
        preview_materials(textures_out, textures)
    row = []
    for spec in TREES:
        if args.only and spec["name"] not in args.only.split(","):
            continue
        lod0, lod1 = build_tree(m, create_mesh, spec, cards, args.seeds)
        export([lod0, lod1], os.path.join(out, f"Tree_{spec['name']}.fbx"))
        remove(lod1)
        if args.sheet:
            lod0.location.x = 9.0 * len(row)
            row.append(lod0)
        else:
            remove(lod0)
    if row:
        sheet(row, args.sheet)
        sheet(row, args.sheet.replace(".png", "_side.png"), side=True)
    print("VEILMOUTH done")


if __name__ == "__main__":
    main()
