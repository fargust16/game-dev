"""
Veilmouth's street and garden trees, made in Blender from Easy Tree and written for Unity (VEILMOUTH_CITY_PLAN.md §27).

Run headless, from the repository root:

    blender -b --factory-startup -noaudio --python-exit-code 1 --python tools/blender/veilmouth_trees.py -- \
        --out unity/Assets/Art/Trees [--preview <folder>] [--only StreetHornbeamA]

What it does, per leaf kind and per tree:

1. Twig card. Easy Tree's own twig ("Make Twig & Leaf": real leaves on a stem) is rendered front-on by Cycles into a
   512 x 1024 card: its albedo with alpha, and its normals in the card's frame (OpenGL, +Y up), so a card of a few
   triangles shows a twig of seven to nine shaded leaves.
2. Tree. Easy Tree's generator grows the branches and puts its twigs where it always does, but each twig is the card
   (two crossed quads) through the generator's own "Custom Twig and Leaf" input, so a tree's leaves cost a few thousand
   triangles instead of a hundred thousand.
3. Bark decimated to a budget; the cards' normals turned outward from the crown's middle, so a crown shades as a whole.
4. Wind weights in the vertex colour (linear), read by GameDev/Slice/Tree:
     R  sway: (height / tree height) ^ 1.8, the same for bark and cards, so a twig never leaves its branch;
     G  flutter: 0 on the bark, 0 at a card's base to 1 at its tip;
     B  phase: one random value for each twig, from its base point.
5. LOD1: about 45 % of the twigs, each 1.3 times larger, on bark decimated further.
6. FBX: one file per tree, objects <Name>_LOD0 and <Name>_LOD1, metres, +Y up, materials by name only (the textures
   are written beside it and the Unity project's own materials are linked on import).

Every size and count here is an authoring default.
"""

import argparse
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Vector

# --------------------------------------------------------------------------------------------- the trees

# Easy Tree's generator inputs by name, then this project's own: height in metres, the kind of leaf, and the budget.
# Thin trees for streets of three storeys: a clear trunk to about 2.6 m, crowns 3-5 m across (authoring defaults).
TREES = [
    dict(name="StreetHornbeamA", leaf="Hornbeam", height=9.0, seed=11, branches="Two Branches", trunk=2, treetop=5,
         levels=4, length=5, angle=0.3, jitter=0.12, gravity=0.2, thickness=1.5, min_height=6, density=3.0,
         scale=(0.75, 1.0), bark_tris=2600),
    dict(name="StreetHornbeamB", leaf="Hornbeam", height=8.6, seed=37, branches="Two Branches", trunk=2, treetop=5,
         levels=4, length=5, angle=0.33, jitter=0.14, gravity=0.25, thickness=1.5, min_height=6, density=3.0,
         scale=(0.75, 1.0), bark_tris=2600),
    dict(name="StreetBeechA", leaf="Beech", height=8.4, seed=5, branches="Two Branches", trunk=3, treetop=4,
         levels=4, length=5, angle=0.36, jitter=0.14, gravity=0.3, thickness=1.6, min_height=5, density=2.8,
         scale=(0.75, 1.05), bark_tris=2600),
    dict(name="StreetBeechB", leaf="Beech", height=8.0, seed=71, branches="Two Branches", trunk=3, treetop=4,
         levels=4, length=5, angle=0.38, jitter=0.16, gravity=0.3, thickness=1.6, min_height=5, density=2.8,
         scale=(0.75, 1.05), bark_tris=2600),
    dict(name="GardenBeechA", leaf="Beech", height=10.0, seed=23, branches="Two Branches", trunk=3, treetop=3,
         levels=5, length=7, angle=0.55, jitter=0.18, gravity=0.5, thickness=1.9, min_height=6, density=2.4,
         scale=(0.8, 1.1), bark_tris=3400),
    dict(name="GardenHornbeamB", leaf="Hornbeam", height=9.6, seed=89, branches="Three Branches", trunk=3, treetop=3,
         levels=4, length=7, angle=0.5, jitter=0.2, gravity=0.45, thickness=1.8, min_height=6, density=1.5,
         scale=(0.95, 1.25), bark_tris=3400),
]

# The season of each kind of leaf on Easy Tree's scale (0 spring, 0.5 summer, 1 fall).
TWIGS = {
    "Beech": dict(season=0.5),
    "Hornbeam": dict(season=0.45),
}

# A card's spray: Easy Tree's twigs (leaves on a stem 0.1 m a leaf) fanned from one base: leaves, tilt from upright in
# the card's plane and spin about the stem, degrees, and scale.
SPRAY = [(8, 0, 0, 1.0), (6, -38, 70, 0.9), (6, 36, 150, 0.9), (5, -68, 220, 0.78), (5, 66, 300, 0.78)]

CARD_SIZE = (1024, 1024)  # px, a twig's card
LOD1_KEEP = 0.45         # the share of twigs LOD1 keeps
LOD1_GROW = 1.3          # how much larger they grow
LOD1_BARK = 0.35         # LOD1's bark against LOD0's
SWAY_POWER = 1.8
BARK = "VeilmouthBark"
CARD_TEXTURE = "Tree_Twig_{leaf}"


def leaves_material(leaf):
    return f"VeilmouthLeaves_{leaf}"


# --------------------------------------------------------------------------------------------- Easy Tree

def copy_bark(bark_textures, textures_out):
    """Easy Tree's bark (ambientCG Bark001, CC0) at half its size, 512 x 1024, as the project's bark."""
    for source, target in (("Bark001_2K-JPG_Color.jpg", "Tree_Bark.jpg"), ("Bark001_2K-JPG_NormalGL.jpg", "Tree_Bark_Normal.jpg")):
        image = bpy.data.images.load(os.path.join(bark_textures, source))
        if target.endswith("_Normal.jpg"):
            image.colorspace_settings.name = 'Non-Color'
        image.scale(image.size[0] // 2, image.size[1] // 2)
        image.file_format = 'JPEG'
        image.save(filepath=os.path.join(textures_out, target), quality=90)
        bpy.data.images.remove(image)


def easy_tree_assets():
    import addon_utils
    for module in addon_utils.modules():
        if module.__name__.endswith("easy_tree"):
            folder = os.path.dirname(module.__file__)
            return os.path.join(folder, "assets", "assets.blend"), os.path.join(folder, "assets", "textures")
    raise RuntimeError("Easy Tree is not installed (extensions.blender.org/add-ons/easy-tree)")


def append_generator(blend):
    if "Simple Tree Generator" not in bpy.data.node_groups:
        with bpy.data.libraries.load(blend, link=False) as (src, dst):
            dst.node_groups = ["Simple Tree Generator"]
    return bpy.data.node_groups["Simple Tree Generator"]


def socket_id(group, name):
    for item in group.interface.items_tree:
        if item.item_type == 'SOCKET' and item.in_out == 'INPUT' and " ".join(item.name.split()) == " ".join(name.split()):
            return item.identifier
    raise KeyError(f"{group.name} has no input '{name}'")


def set_input(mod, name, value):
    """A Geometry Nodes modifier's input by its name: modifier.properties.inputs in Blender 5.2, ID properties before."""
    identifier = socket_id(mod.node_group, name)
    inputs = getattr(getattr(mod, "properties", None), "inputs", None)
    if inputs is None:
        mod[identifier] = value
    else:
        getattr(inputs, identifier).value = value


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


# --------------------------------------------------------------------------------------------- twig cards

def twig_object(leaf, generator):
    """
    A spray of Easy Tree's twigs fanned from one base in the card's plane, realised, with their own leaf and bark
    materials: one twig of Easy Tree is a stem of a metre with nine leaves, too sparse to fill a card.
    """
    group = bpy.data.node_groups.new(f"Twig_{leaf}", 'GeometryNodeTree')
    group.interface.new_socket("Geometry", in_out='OUTPUT', socket_type='NodeSocketGeometry')
    out = group.nodes.new('NodeGroupOutput')
    join = group.nodes.new('GeometryNodeJoinGeometry')
    for count, tilt, spin, scale in SPRAY:
        twig = group.nodes.new('GeometryNodeGroup')
        twig.node_tree = bpy.data.node_groups["Make Twig & Leaf"]
        twig.inputs["Leaf Type"].default_value = leaf
        twig.inputs["Count"].default_value = count
        twig.inputs["Season"].default_value = TWIGS[leaf]["season"]
        twig.inputs["Twig Material"].default_value = bpy.data.materials["Bark"]
        # Spun about its own stem first, so other leaves face the card, then tilted in the card's plane.
        turn = group.nodes.new('GeometryNodeTransform')
        turn.inputs["Rotation"].default_value = (0.0, 0.0, math.radians(spin))
        turn.inputs["Scale"].default_value = (scale, scale, scale)
        lean = group.nodes.new('GeometryNodeTransform')
        lean.inputs["Rotation"].default_value = (0.0, math.radians(tilt), 0.0)
        group.links.new(twig.outputs[0], turn.inputs["Geometry"])
        group.links.new(turn.outputs[0], lean.inputs["Geometry"])
        group.links.new(lean.outputs[0], join.inputs[0])
    realise = group.nodes.new('GeometryNodeRealizeInstances')
    group.links.new(join.outputs[0], realise.inputs[0])
    group.links.new(realise.outputs[0], out.inputs[0])
    obj = new_object(f"Twig_{leaf}")
    mod = obj.modifiers.new("Twig", 'NODES')
    mod.node_group = group
    return obj


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
        # The leaf's own normal map where the material has one (a Normal Map node gives a world normal), so the
        # veins show; else the smooth geometry's normal.
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
    """Renders the twig front-on (looking along +Y): X across the card, Z up it."""
    scene = bpy.context.scene
    originals = list(obj.data.materials)
    swapped = {}
    for slot in obj.material_slots:
        if slot.material is not None and slot.material.name not in swapped:
            swapped[slot.material.name] = unlit(slot.material, normals)
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
    for slot, material in zip(obj.material_slots, originals):
        slot.material = material
    for material in swapped.values():
        bpy.data.materials.remove(material)


def bake_card(leaf, generator, textures_out):
    """The twig card of one kind of leaf: its two textures, and the quad size in metres (width, height) they cover."""
    obj = twig_object(leaf, generator)
    apply_modifiers(obj)  # the twig's materials come with its mesh, so the renders can swap them
    for poly in obj.data.polygons:
        poly.use_smooth = True
    xs = [v.co.x for v in obj.data.vertices]
    zs = [v.co.z for v in obj.data.vertices]
    half = max(abs(min(xs)), abs(max(xs))) * 1.03
    z0 = min(0.0, min(zs)) - 0.01
    z1 = max(zs) * 1.02
    aspect = CARD_SIZE[0] / CARD_SIZE[1]
    scale = max(z1 - z0, 2.0 * half / aspect)
    frame = dict(scale=scale, mid_z=z0 + scale / 2.0)
    base = os.path.join(textures_out, CARD_TEXTURE.format(leaf=leaf))
    render_card(obj, False, base + ".png", frame)
    render_card(obj, True, base + "_Normal.png", frame)
    check_normals(base + "_Normal.png")
    remove(obj)
    width, height = scale * aspect, scale
    print(f"VEILMOUTH card {leaf}: {width:.3f} x {height:.3f} m, base {-z0:.3f} m below the frame's foot")
    return width, height, z0


def check_normals(path):
    """A card's normals face the viewer: their mean Z, where a leaf is, stays well above 0.5 (the flat value)."""
    image = bpy.data.images.load(path)
    px = list(image.pixels)
    zs = [px[i + 2] for i in range(0, len(px), 4) if px[i + 3] > 0.5]
    mean = sum(zs) / max(1, len(zs))
    bpy.data.images.remove(image)
    print(f"VEILMOUTH normals {os.path.basename(path)}: mean z {mean:.3f} over {len(zs)} px")
    if mean < 0.6:
        raise RuntimeError(f"{path}: the card's normals face away (mean z {mean:.3f})")


def card_object(leaf, width, height, z0):
    """Two crossed quads standing on the twig's base, UV 0-1 over the rendered frame, in the kind's leaf material."""
    mesh = bpy.data.meshes.new(f"Card_{leaf}")
    w = width / 2.0
    verts = [(-w, 0, z0), (w, 0, z0), (w, 0, z0 + height), (-w, 0, z0 + height),
             (0, -w, z0), (0, w, z0), (0, w, z0 + height), (0, -w, z0 + height)]
    faces = [(0, 1, 2, 3), (4, 5, 6, 7)]
    mesh.from_pydata(verts, [], faces)
    uv = mesh.uv_layers.new(name="UVMap")
    corners = [(0, 0), (1, 0), (1, 1), (0, 1)]
    for poly in mesh.polygons:
        for k, loop in enumerate(poly.loop_indices):
            uv.data[loop].uv = corners[k]
    mesh.materials.append(material(leaves_material(leaf)))
    return new_object(f"Card_{leaf}", mesh)


def material(name):
    return bpy.data.materials.get(name) or bpy.data.materials.new(name)


# --------------------------------------------------------------------------------------------- one tree

def grow(spec, generator, card):
    """
    The tree as one mesh: Easy Tree's generator inside a group of this script's own, its inputs set on the node and
    its twigs realised before the modifier is applied (an applied modifier keeps only the mesh, not instances).
    """
    group = bpy.data.node_groups.new(f"Grow_{spec['name']}", 'GeometryNodeTree')
    group.interface.new_socket("Geometry", in_out='OUTPUT', socket_type='NodeSocketGeometry')
    out = group.nodes.new('NodeGroupOutput')
    tree = group.nodes.new('GeometryNodeGroup')
    tree.node_tree = generator
    realise = group.nodes.new('GeometryNodeRealizeInstances')
    group.links.new(tree.outputs[0], realise.inputs[0])
    group.links.new(realise.outputs[0], out.inputs[0])
    sockets = {" ".join(s.name.split()): s for s in tree.inputs}
    for name, value in (("N Branches per Level", spec["branches"]), ("Trunk Height", spec["trunk"]),
                        ("Treetop Height", spec["treetop"]), ("Num Levels", spec["levels"]),
                        ("Branch Length", spec["length"]), ("Random Angle", spec["angle"]),
                        ("Random Jitter", spec["jitter"]), ("Gravity", spec["gravity"]),
                        ("Thickness", spec["thickness"]), ("Bark Material", bpy.data.materials["Bark"]),
                        ("Seed", spec["seed"]), ("Add Leaves", True),
                        ("Min Height", spec["min_height"]), ("Leaf Density", spec["density"]),
                        ("Leaf Min Scale", spec["scale"][0]), ("Leaf Max Scale", spec["scale"][1]),
                        ("Visible in Viewport", True), ("Custom Twig and Leaf", True), ("Custom Leaf", card),
                        ("Wind", False)):
        sockets[name].default_value = value
    obj = new_object(spec["name"])
    obj.modifiers.new("Easy Tree", 'NODES').node_group = group
    apply_modifiers(obj)
    return obj


def split(obj, bark_name):
    """The applied tree as bark and leaves, each a bmesh."""
    names = [m.name if m is not None else None for m in obj.data.materials]
    if "Bark" not in names:
        raise RuntimeError(f"{obj.name}: no bark among its materials {names}, {len(obj.data.polygons)} faces")
    bark_index = names.index("Bark")
    bark, leaves = bmesh.new(), bmesh.new()
    for bm, keep_bark in ((bark, True), (leaves, False)):
        bm.from_mesh(obj.data)
        bmesh.ops.delete(bm, geom=[f for f in bm.faces if (f.material_index == bark_index) != keep_bark], context='FACES')
    return bark, leaves


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


def card_faces(leaves):
    """Each card face with its base point (the midpoint of its two corners at v = 0) and its height along the card."""
    uv = leaves.loops.layers.uv.active
    cards = []
    for face in leaves.faces:
        bottom = [l.vert.co for l in face.loops if l[uv].uv.y < 0.5]
        top = [l.vert.co for l in face.loops if l[uv].uv.y >= 0.5]
        base = sum(bottom, Vector()) / len(bottom)
        cards.append((face, base, (sum(top, Vector()) / len(top) - base).length))
    return cards


def twig_hash(point):
    """A stable value in [0, 1) for a twig, from its base point (millimetres)."""
    key = (round(point.x * 1000), round(point.y * 1000), round(point.z * 1000))
    return random.Random(hash(key) & 0xFFFFFFFF).random()


def thin(leaves, keep, grow_by):
    """LOD1's leaves: the twigs whose hash falls under keep, each grown about its base."""
    drop = []
    for face, base, _ in card_faces(leaves):
        if twig_hash(base) >= keep:
            drop.append(face)
            continue
        for vert in face.verts:
            vert.co = base + (vert.co - base) * grow_by
    bmesh.ops.delete(leaves, geom=drop, context='FACES')


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
            bottom = [mesh.vertices[mesh.loops[l].vertex_index].co for l in poly.loop_indices if uv[l].uv.y < 0.5]
            base = sum(bottom, Vector()) / len(bottom)
            phase = twig_hash(base)
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
          f"{len(leaf_polys) // 2} twigs, {obj.dimensions.x:.2f} x {obj.dimensions.y:.2f} x {obj.dimensions.z:.2f} m")
    return obj


def to_mesh(bm, name):
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    return mesh


def build_tree(spec, generator, cards):
    obj = grow(spec, generator, cards[spec["leaf"]])
    # The generator's tree to the asked height, its trunk's foot at the origin.
    top = max(v.co.z for v in obj.data.vertices)
    factor = spec["height"] / top
    for v in obj.data.vertices:
        v.co *= factor
    bark, leaves = split(obj, spec["name"])
    remove(obj)
    bark0 = decimate(bark, spec["bark_tris"], spec["name"] + "_b0")
    bark1 = decimate(bark, int(spec["bark_tris"] * LOD1_BARK), spec["name"] + "_b1")
    leaves1 = leaves.copy()
    thin(leaves1, LOD1_KEEP, LOD1_GROW)
    lod0 = assemble(spec["name"] + "_LOD0", bark0, leaves, spec["height"], spec["leaf"])
    lod1 = assemble(spec["name"] + "_LOD1", bark1, leaves1, spec["height"], spec["leaf"])
    for bm in (bark, leaves, bark0, bark1, leaves1):
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
    for leaf in TWIGS:
        base = os.path.join(textures_out, CARD_TEXTURE.format(leaf=leaf))
        principled(leaves_material(leaf), base + ".png", base + "_Normal.png", True)


def preview(objects, path):
    scene = bpy.context.scene
    for obj in scene.objects:
        if obj.type == 'MESH':
            obj.hide_render = obj not in objects
    height = max(o.dimensions.z for o in objects)
    camera = scene.camera
    camera.data.type = 'PERSP'
    camera.data.lens = 50
    camera.location = (height * 1.25, -height * 1.6, height * 0.45)
    direction = Vector((0, 0, height * 0.5)) - camera.location
    camera.rotation_euler = direction.to_track_quat('-Z', 'Y').to_euler()
    scene.render.resolution_x, scene.render.resolution_y = 600, 900
    scene.render.film_transparent = False
    scene.render.filter_size = 1.5
    scene.view_settings.view_transform = 'AgX'
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


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
    world.color = (0.45, 0.5, 0.58)
    if world.node_tree is not None:
        background = next((n for n in world.node_tree.nodes if n.bl_idname == 'ShaderNodeBackground'), None)
        if background is not None:
            background.inputs["Color"].default_value = (0.45, 0.5, 0.58, 1.0)
    scene.world = world
    print("VEILMOUTH view transforms", [i.identifier for i in scene.view_settings.bl_rna.properties["view_transform"].enum_items_static])
    return scene


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser()
    parser.add_argument("--out", required=True)
    parser.add_argument("--preview")
    parser.add_argument("--only")
    parser.add_argument("--sheet", help="one render of every tree's LOD0 side by side")
    args = parser.parse_args(argv)
    out = os.path.abspath(args.out)
    textures_out = os.path.join(out, "Textures")
    os.makedirs(textures_out, exist_ok=True)

    setup_scene()
    blend, bark_textures = easy_tree_assets()
    generator = append_generator(blend)
    copy_bark(bark_textures, textures_out)
    cards = {}
    for leaf in TWIGS:
        width, height, z0 = bake_card(leaf, generator, textures_out)
        cards[leaf] = card_object(leaf, width, height, z0)
        cards[leaf].hide_render = True
    if args.preview:
        preview_materials(textures_out, bark_textures)
        os.makedirs(args.preview, exist_ok=True)
    if args.sheet and not args.preview:
        preview_materials(textures_out, bark_textures)
    row = []
    for spec in TREES:
        if args.only and spec["name"] != args.only:
            continue
        lod0, lod1 = build_tree(spec, generator, cards)
        export([lod0, lod1], os.path.join(out, f"Tree_{spec['name']}.fbx"))
        if args.preview:
            preview([lod0], os.path.join(args.preview, f"{spec['name']}_LOD0.png"))
            preview([lod1], os.path.join(args.preview, f"{spec['name']}_LOD1.png"))
        remove(lod1)
        if args.sheet:
            lod0.location.x = 7.0 * len(row)
            row.append(lod0)
        else:
            remove(lod0)
    if row:
        sheet(row, args.sheet)
    print("VEILMOUTH done")


def sheet(objects, path):
    """Every tree's LOD0 in a row, seen from the side with a 1.8 m figure for scale."""
    scene = bpy.context.scene
    figure = new_object("Figure", bpy.data.meshes.new("Figure"))
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co = Vector((v.co.x * 0.45, v.co.y * 0.3, (v.co.z + 0.5) * 1.8))
    bm.to_mesh(figure.data)
    figure.location = (-3.5, 0.0, 0.0)
    for obj in scene.objects:
        if obj.type == 'MESH':
            obj.hide_render = obj not in objects and obj is not figure
    width = 7.0 * len(objects) + 7.0
    camera = scene.camera
    camera.data.type = 'ORTHO'
    camera.data.ortho_scale = width
    camera.location = (width / 2.0 - 7.0, -60.0, 5.0)
    camera.rotation_euler = (math.radians(90.0), 0.0, 0.0)
    scene.render.resolution_x, scene.render.resolution_y = 1600, int(1600 * 12.0 / width)
    scene.render.film_transparent = False
    scene.render.filter_size = 1.5
    scene.view_settings.view_transform = 'AgX'
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


main()
