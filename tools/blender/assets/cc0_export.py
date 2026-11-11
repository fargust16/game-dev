"""CC0 asset libraries (owner, 2026-11-11): Blender's remote libraries rileyb3d, Charge, Poly Haven Lite and ambientCG
Lite, every asset CC0. This script reads one downloaded .blend in the background and either lists its assets or exports
one asset for Unity: its meshes joined, its materials baked to a base map, a tangent normal map and a URP metallic map
(R metallic, A smoothness), thinned to a triangle budget after the bake, set on its foot, and written as FBX in metres
with a JSON note of what it holds.

Run (Blender in the background, never on an open file; -Y keeps the file's scripts from running):
  blender -b --factory-startup -noaudio -Y --python-exit-code 1 --python tools/blender/assets/cc0_export.py -- \
      list <file.blend> <out.json>
  blender ... -- export <file.blend> <asset name> <out folder> <Unity name> [max triangles] [texture size] [preview.png]
"""
import json
import math
import os
import sys

import bpy
import numpy as np
from mathutils import Vector


def args():
    argv = sys.argv
    return argv[argv.index("--") + 1:] if "--" in argv else []


def open_blend(path):
    bpy.ops.wm.open_mainfile(filepath=path, load_ui=False, use_scripts=False)


def triangles(objects):
    deps = bpy.context.evaluated_depsgraph_get()
    total = 0
    for o in objects:
        if o.type not in {"MESH", "CURVE", "SURFACE", "META", "FONT"}:
            continue
        ev = o.evaluated_get(deps)
        try:
            mesh = ev.to_mesh()
        except RuntimeError:
            continue
        if mesh is not None:
            mesh.calc_loop_triangles()
            total += len(mesh.loop_triangles)
            ev.to_mesh_clear()
    return total


def bounds(objects):
    lo = Vector((math.inf,) * 3)
    hi = Vector((-math.inf,) * 3)
    for o in objects:
        if o.type != "MESH":
            continue
        for c in o.bound_box:
            w = o.matrix_world @ Vector(c)
            lo = Vector(map(min, lo, w))
            hi = Vector(map(max, hi, w))
    return lo, hi


def asset_objects(asset):
    if isinstance(asset, bpy.types.Collection):
        return list(asset.all_objects)
    if isinstance(asset, bpy.types.Object):
        return [asset] + list(asset.children_recursive)
    return []


def list_assets(out):
    rows = []
    for kind, items in (("COLLECTION", bpy.data.collections), ("OBJECT", bpy.data.objects), ("MATERIAL", bpy.data.materials)):
        for a in items:
            if a.asset_data is None:
                continue
            row = {"kind": kind, "name": a.name}
            objs = asset_objects(a)
            if objs:
                row["objects"] = len(objs)
                row["triangles"] = triangles(objs)
                lo, hi = bounds(objs)
                if lo.x < math.inf:
                    row["size"] = [round(hi.x - lo.x, 3), round(hi.y - lo.y, 3), round(hi.z - lo.z, 3)]
                mats = {s.material.name for o in objs for s in getattr(o, "material_slots", []) if s.material}
                row["materials"] = sorted(mats)
            rows.append(row)
    with open(out, "w", encoding="utf-8") as f:
        json.dump(rows, f, indent=1)
    print(f"[cc0] {len(rows)} assets listed in {out}")


def find_asset(name):
    for items in (bpy.data.collections, bpy.data.objects):
        a = items.get(name)
        if a is not None and a.asset_data is not None:
            return a
    for items in (bpy.data.collections, bpy.data.objects):
        a = items.get(name)
        if a is not None:
            return a
    raise SystemExit(f"[cc0] no asset named {name!r}")


def isolate(asset):
    """A fresh scene holding only the asset's objects, instances made real, everything a mesh."""
    scene = bpy.data.scenes.new("cc0")
    bpy.context.window.scene = scene
    keep = asset_objects(asset)
    for o in keep:
        if o.name not in scene.collection.objects:
            scene.collection.objects.link(o)
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action="DESELECT")
    for o in scene.objects:
        if o.instance_type == "COLLECTION" and o.instance_collection is not None:
            o.select_set(True)
    if bpy.context.selected_objects:
        bpy.context.view_layer.objects.active = bpy.context.selected_objects[0]
        bpy.ops.object.duplicates_make_real(use_base_parent=False, use_hierarchy=False)
    bpy.ops.object.select_all(action="DESELECT")
    # A rig's bone shapes are meshes too (Poly Haven's ship keeps them in its asset): never part of the model.
    widgets = {pb.custom_shape for a in bpy.data.objects if a.type == "ARMATURE" for pb in a.pose.bones if pb.custom_shape}
    meshes = []
    for o in list(scene.objects):
        if o.type in {"CURVE", "SURFACE", "META", "FONT", "MESH"} and not o.hide_render and o not in widgets:
            o.select_set(True)
            meshes.append(o)
    if not meshes:
        raise SystemExit("[cc0] the asset holds no mesh")
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.make_single_user(object=True, obdata=True, material=False)
    bpy.ops.object.convert(target="MESH")
    for o in list(scene.objects):
        if not o.select_get():
            scene.collection.objects.unlink(o)
    bpy.ops.object.parent_clear(type="CLEAR_KEEP_TRANSFORM")
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    if not obj.data.uv_layers:
        bpy.ops.object.mode_set(mode="EDIT")
        bpy.ops.mesh.select_all(action="SELECT")
        bpy.ops.uv.smart_project(island_margin=0.01)
        bpy.ops.object.mode_set(mode="OBJECT")
    while len(obj.data.uv_layers) > 1:
        active = obj.data.uv_layers.active_index
        obj.data.uv_layers.remove(obj.data.uv_layers[1 if active == 0 else 0])
    return scene, obj


def gpu_cycles(scene):
    scene.render.engine = "CYCLES"
    try:
        prefs = bpy.context.preferences.addons["cycles"].preferences
        for kind in ("OPTIX", "CUDA", "HIP"):
            try:
                prefs.compute_device_type = kind
                prefs.get_devices()
                if any(d.type == kind for d in prefs.devices):
                    for d in prefs.devices:
                        d.use = d.type == kind
                    scene.cycles.device = "GPU"
                    return kind
            except TypeError:
                continue
    except KeyError:
        pass
    scene.cycles.device = "CPU"
    return "CPU"


def principled(mat):
    if not mat or not mat.use_nodes:
        return None
    out = next((n for n in mat.node_tree.nodes if n.type == "OUTPUT_MATERIAL" and n.is_active_output), None)
    if out and out.inputs["Surface"].is_linked:
        node = out.inputs["Surface"].links[0].from_node
        if node.type == "BSDF_PRINCIPLED":
            return node
    return next((n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)


def bake(obj, scene, size, stem, out_dir):
    """Each material baked to its own images on the mesh's UVs; returns the note of each."""
    scene.cycles.samples = 4
    scene.render.bake.margin = 8
    scene.render.bake.use_clear = True
    notes = []
    mats = [s.material for s in obj.material_slots]
    images = {}
    for i, mat in enumerate(mats):
        if mat is None:
            continue
        mat.name = f"{stem}_{i}"
        if not mat.use_nodes:
            mat.use_nodes = True
        for kind in ("base", "normal", "rough", "metal", "alpha"):
            img = bpy.data.images.new(f"{mat.name}_{kind}", size, size, alpha=True, float_buffer=False)
            img.colorspace_settings.name = "sRGB" if kind == "base" else "Non-Color"
            images[(i, kind)] = img

    def target(kind):
        for i, mat in enumerate(mats):
            if mat is None:
                continue
            nt = mat.node_tree
            node = nt.nodes.get("cc0_target") or nt.nodes.new("ShaderNodeTexImage")
            node.name = "cc0_target"
            node.image = images[(i, kind)]
            nt.nodes.active = node
            node.select = True

    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    target("base")
    bpy.ops.object.bake(type="DIFFUSE", pass_filter={"COLOR"}, use_clear=True)
    target("rough")
    bpy.ops.object.bake(type="ROUGHNESS", use_clear=True)
    target("normal")
    bpy.ops.object.bake(type="NORMAL", normal_space="TANGENT", use_clear=True)

    # Metallic and alpha through an emission shader wired in their place, then the material set back.
    for kind, socket in (("metal", "Metallic"), ("alpha", "Alpha")):
        restore = []
        for i, mat in enumerate(mats):
            p = principled(mat)
            if p is None:
                continue
            nt = mat.node_tree
            out = next((n for n in nt.nodes if n.type == "OUTPUT_MATERIAL" and n.is_active_output), None)
            if out is None:
                continue
            old = out.inputs["Surface"].links[0].from_socket if out.inputs["Surface"].is_linked else None
            em = nt.nodes.new("ShaderNodeEmission")
            src = p.inputs[socket]
            if src.is_linked:
                nt.links.new(src.links[0].from_socket, em.inputs["Color"])
            else:
                v = src.default_value
                em.inputs["Color"].default_value = (v, v, v, 1.0)
            nt.links.new(em.outputs["Emission"], out.inputs["Surface"])
            restore.append((nt, out, old, em))
        target(kind)
        bpy.ops.object.bake(type="EMIT", use_clear=True)
        for nt, out, old, em in restore:
            nt.nodes.remove(em)
            if old is not None:
                nt.links.new(old, out.inputs["Surface"])

    for i, mat in enumerate(mats):
        if mat is None:
            continue
        base = np.array(images[(i, "base")].pixels[:], dtype=np.float32).reshape(-1, 4)
        alpha = np.array(images[(i, "alpha")].pixels[:], dtype=np.float32).reshape(-1, 4)[:, 0]
        rough = np.array(images[(i, "rough")].pixels[:], dtype=np.float32).reshape(-1, 4)[:, 0]
        metal = np.array(images[(i, "metal")].pixels[:], dtype=np.float32).reshape(-1, 4)[:, 0]
        # Cut-out only where the material's own alpha is driven, and counted over the pixels the bake covered (the
        # normal bake's own alpha: geometry only, where the base bake already carries the material's transparency),
        # not over the empty sheet round the UV islands.
        p = principled(mat)
        driven = p is not None and (p.inputs["Alpha"].is_linked or p.inputs["Alpha"].default_value < 0.999)
        covered = np.array(images[(i, "normal")].pixels[:], dtype=np.float32).reshape(-1, 4)[:, 3] > 0.5
        cut = bool(driven and covered.any() and (alpha[covered] < 0.5).mean() > 0.01)
        base[:, 3] = alpha if cut else 1.0
        images[(i, "base")].pixels[:] = base.ravel()
        mask = np.zeros_like(base)
        mask[:, 0] = metal
        mask[:, 3] = 1.0 - rough
        images[(i, "metal")].pixels[:] = mask.ravel()
        note = {"material": mat.name, "alpha_cut": cut, "metallic_mean": round(float(metal.mean()), 3),
                "smoothness_mean": round(float(1.0 - rough.mean()), 3)}
        for kind in ("base", "normal", "metal"):
            img = images[(i, kind)]
            path = os.path.join(out_dir, f"{mat.name}_{kind}.png")
            img.filepath_raw = path
            img.file_format = "PNG"
            img.save()
            note[kind] = os.path.basename(path)
        notes.append(note)
    return notes


def foot(obj):
    lo, hi = bounds([obj])
    pivot = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z))
    obj.data.transform(__import__("mathutils").Matrix.Translation(-pivot))
    obj.location = (0, 0, 0)
    obj.data.update()


def preview(obj, scene, path):
    scene.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in {e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items} else "BLENDER_EEVEE"
    lo, hi = bounds([obj])
    centre = (lo + hi) / 2
    radius = max((hi - lo).length / 2, 0.05)
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    scene.collection.objects.link(cam)
    cam.data.lens = 50
    d = radius * 3.2
    cam.location = centre + Vector((d * 0.7, -d * 0.7, d * 0.45))
    cam.rotation_euler = (centre - cam.location).to_track_quat("-Z", "Y").to_euler()
    scene.camera = cam
    sun = bpy.data.objects.new("sun", bpy.data.lights.new("sun", "SUN"))
    sun.data.energy = 3.5
    sun.rotation_euler = (math.radians(50), 0, math.radians(30))
    scene.collection.objects.link(sun)
    world = bpy.data.worlds.new("w")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.8
    scene.world = world
    scene.render.resolution_x = scene.render.resolution_y = 512
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def glassless(obj):
    """
    Faces of glass taken out (a material named glass, or mostly transmissive): a bake gives glass an opaque colour, which
    covered a watch's dial and blacked a pair of spectacles' lenses. Their frames and the dial show through the gap.
    """
    glass = set()
    for i, slot in enumerate(obj.material_slots):
        p = principled(slot.material)
        t = p.inputs.get("Transmission Weight") if p is not None else None
        if slot.material is not None and ("glass" in slot.material.name.lower() or (t is not None and not t.is_linked and t.default_value >= 0.5)):
            glass.add(i)
    if not glass:
        return 0
    import bmesh
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    gone = [f for f in bm.faces if f.material_index in glass]
    bmesh.ops.delete(bm, geom=gone, context="FACES")
    bm.to_mesh(obj.data)
    bm.free()
    for i in sorted(glass, reverse=True):
        obj.active_material_index = i
        bpy.ops.object.material_slot_remove()
    return len(gone)


def export(blend, name, out_dir, stem, max_tris, size, preview_path):
    open_blend(blend)
    asset = find_asset(name)
    scene, obj = isolate(asset)
    dropped = glassless(obj)
    if dropped:
        print(f"[cc0] {stem}: {dropped} faces of glass left out")
    os.makedirs(out_dir, exist_ok=True)
    before = triangles([obj])
    device = gpu_cycles(scene)
    notes = bake(obj, scene, size, stem, out_dir)
    # Rebind each material to its baked images so the preview shows what Unity will.
    for note in notes:
        mat = bpy.data.materials[note["material"]]
        nt = mat.node_tree
        nt.nodes.clear()
        out = nt.nodes.new("ShaderNodeOutputMaterial")
        p = nt.nodes.new("ShaderNodeBsdfPrincipled")
        nt.links.new(p.outputs[0], out.inputs[0])
        tb = nt.nodes.new("ShaderNodeTexImage")
        tb.image = bpy.data.images.load(os.path.join(out_dir, note["base"]))
        nt.links.new(tb.outputs["Color"], p.inputs["Base Color"])
    if before > max_tris:
        mod = obj.modifiers.new("thin", "DECIMATE")
        mod.ratio = max(max_tris / before, 0.02)
        mod.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    after = triangles([obj])
    foot(obj)
    lo, hi = bounds([obj])
    obj.name = stem
    obj.data.name = stem
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    fbx = os.path.join(out_dir, f"{stem}.fbx")
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True, mesh_smooth_type="FACE",
                             use_mesh_modifiers=True, add_leaf_bones=False, path_mode="STRIP", embed_textures=False,
                             use_tspace=True)
    note = {"name": stem, "asset": name, "source": os.path.basename(blend), "license": "CC0-1.0",
            "triangles_source": before, "triangles": after, "size_m": [round(hi.x - lo.x, 3), round(hi.y - lo.y, 3), round(hi.z - lo.z, 3)],
            "texture": size, "bake_device": device, "materials": notes}
    with open(os.path.join(out_dir, f"{stem}.json"), "w", encoding="utf-8") as f:
        json.dump(note, f, indent=1)
    if preview_path:
        preview(obj, scene, preview_path)
    print(f"[cc0] {stem}: {before} -> {after} triangles, {note['size_m']} m, {len(notes)} materials, baked on {device}")


def images(out_dir):
    """Every image of the file written to out_dir under its own name, and a note of the socket each one feeds."""
    os.makedirs(out_dir, exist_ok=True)
    feeds = {}
    for mat in bpy.data.materials:
        if not mat.use_nodes:
            continue
        for node in mat.node_tree.nodes:
            if node.type != "TEX_IMAGE" or node.image is None:
                continue
            seen, todo = set(), [(node, None)]
            while todo:
                n, via = todo.pop()
                for out in n.outputs:
                    for link in out.links:
                        to = link.to_node
                        key = (to.name, link.to_socket.name)
                        if key in seen:
                            continue
                        seen.add(key)
                        if to.type in {"BSDF_PRINCIPLED", "OUTPUT_MATERIAL", "DISPLACEMENT"}:
                            feeds.setdefault(node.image.name, []).append(f"{to.type}:{link.to_socket.name}" + (f" via {via}" if via else ""))
                        else:
                            todo.append((to, via or to.type))
    rows = []
    for img in bpy.data.images:
        if img.type != "IMAGE" or (img.packed_file is None and not img.filepath):
            continue
        ext = ".png" if img.file_format == "PNG" else ".jpg"
        path = os.path.join(out_dir, bpy.path.clean_name(img.name.rsplit(".", 1)[0]) + ext)
        if img.packed_file is not None:
            with open(path, "wb") as f:
                f.write(img.packed_file.data)
        rows.append({"image": img.name, "file": os.path.basename(path), "size": list(img.size),
                     "colorspace": img.colorspace_settings.name, "feeds": feeds.get(img.name, [])})
    with open(os.path.join(out_dir, "images.json"), "w", encoding="utf-8") as f:
        json.dump(rows, f, indent=1)
    print(f"[cc0] {len(rows)} images written to {out_dir}")


def decal(image_dir, out_dir, stem, size, preview_path="", upright=False):
    """
    An ambientCG decal (its images written by `images`) as a flat square of `size` metres lying on its foot, facing up:
    the colour with the opacity as its alpha, the OpenGL normal (Unity's own convention) and the URP metallic map,
    written as `export` writes a model, so the same import binds it.
    """
    def load(part):
        for name in sorted(os.listdir(image_dir)):
            if f"_{part}" in name and name.lower().endswith((".jpg", ".png")):
                img = bpy.data.images.load(os.path.join(image_dir, name))
                img.colorspace_settings.name = "sRGB" if part == "Color" else "Non-Color"
                return img
        return None

    colour, opacity, normal = load("Color"), load("Opacity"), load("NormalGL")
    metalness, roughness = load("Metalness"), load("Roughness")
    w, h = colour.size
    px = lambda img: np.array(img.pixels[:], dtype=np.float32).reshape(-1, 4) if img else None
    base = px(colour)
    base[:, 3] = px(opacity)[:, 0] if opacity else 1.0
    mask = np.zeros_like(base)
    mask[:, 0] = px(metalness)[:, 0] if metalness else 0.0
    mask[:, 3] = 1.0 - (px(roughness)[:, 0] if roughness else 0.6)
    os.makedirs(out_dir, exist_ok=True)
    mat_name = f"{stem}_0"
    files = {}
    for kind, data, space in (("base", base, "sRGB"), ("metal", mask, "Non-Color"), ("normal", px(normal), "Non-Color")):
        img = bpy.data.images.new(f"{mat_name}_{kind}", w, h, alpha=True)
        img.colorspace_settings.name = space
        img.pixels[:] = data.ravel()
        img.filepath_raw = os.path.join(out_dir, f"{mat_name}_{kind}.png")
        img.file_format = "PNG"
        img.save()
        files[kind] = os.path.basename(img.filepath_raw)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.mesh.primitive_plane_add(size=size)
    obj = bpy.context.view_layer.objects.active
    if upright:
        # A wall's stain: the square stood up on its lower edge, its face toward -Y (the kit's front after export).
        obj.rotation_euler = (math.radians(90), 0, 0)
        obj.location = (0, 0, size / 2)
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    obj.name = obj.data.name = stem
    mat = bpy.data.materials.new(mat_name)
    mat.use_nodes = True
    tex = mat.node_tree.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(os.path.join(out_dir, files["base"]))
    mat.node_tree.links.new(tex.outputs["Color"], mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
    obj.data.materials.append(mat)
    fbx = os.path.join(out_dir, f"{stem}.fbx")
    bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_UNITS",
                             axis_forward="-Z", axis_up="Y", bake_space_transform=True, mesh_smooth_type="FACE",
                             add_leaf_bones=False, path_mode="STRIP", embed_textures=False, use_tspace=True)
    cut = bool(opacity is not None)
    note = {"name": stem, "asset": os.path.basename(image_dir), "source": "ambientCG", "license": "CC0-1.0",
            "triangles_source": 2, "triangles": 2, "size_m": [size, 0.0, size] if upright else [size, size, 0.0], "texture": max(w, h), "bake_device": "none",
            "materials": [{"material": mat_name, "alpha_cut": cut, "metallic_mean": round(float(mask[:, 0].mean()), 3),
                           "smoothness_mean": round(float(mask[:, 3].mean()), 3), **files}]}
    with open(os.path.join(out_dir, f"{stem}.json"), "w", encoding="utf-8") as f:
        json.dump(note, f, indent=1)
    if preview_path:
        preview(obj, bpy.context.scene, preview_path)
    print(f"[cc0] decal {stem}: {size} m, {w}x{h}, cut {cut}")


def main():
    a = args()
    if not a:
        raise SystemExit(__doc__)
    if a[0] == "list":
        open_blend(a[1])
        list_assets(a[2])
    elif a[0] == "images":
        # An ambientCG material or decal: its packed images written out as they are, and which socket each feeds.
        open_blend(a[1])
        images(a[2])
    elif a[0] == "decal":
        decal(a[1], a[2], a[3], float(a[4]), a[5] if len(a) > 5 else "", len(a) > 6 and a[6] == "upright")
    elif a[0] == "look":
        # A quick look in the asset's own materials, nothing baked or written but the picture: for choosing.
        open_blend(a[1])
        scene, obj = isolate(find_asset(a[2]))
        preview(obj, scene, a[3])
    elif a[0] == "export":
        blend, name, out_dir, stem = a[1], a[2], a[3], a[4]
        max_tris = int(a[5]) if len(a) > 5 else 20000
        size = int(a[6]) if len(a) > 6 else 1024
        preview_path = a[7] if len(a) > 7 else ""
        export(blend, name, out_dir, stem, max_tris, size, preview_path)
    else:
        raise SystemExit(__doc__)


main()
