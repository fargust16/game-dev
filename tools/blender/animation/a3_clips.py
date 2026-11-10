"""
A3 clips made in Blender on the Mixamo armature (ANIMATION_PIPELINE_PLAN.md §5, A3; owner, 2026-11-10).

Mixamo had no Mining, Chopping, Hammering or Surrender, so the agent makes them here. The script is the clips'
source: it builds every frame from the owner's Mixamo downloads and exports one FBX a clip, so no .blend is kept.

- The armature and the neutral body are Breathing Idle's first frame; the grips are Great Sword Idle's hands (both
  hands on one handle) and Sword And Shield Idle's (a hammer and tongs); the kneel is Kneeling Idle's.
- Each clip is a few key poses (where the hands are, how far the spine bends and turns, how low the hips go),
  joined by smooth curves. Arms and legs reach their targets by two-bone IK whose elbow and knee bend about their
  own hinge, measured from Great Sword Idle's bent elbows and Kneeling Idle's knees, so Unity reads the bends as
  muscles. The feet stay where they stood.
- Positions are metres in the character's own frame: x to its right, y forward, z up, from the ground between its
  feet, for a reference body whose shoulders stand 1.40 m high, scaled to the armature's own size (the Mixamo
  files' body stands about 1 m). A positive turn (yaw) is to the character's right. They are authoring defaults,
  checked in the preview sheets and in Unity's rig check.

Run (Blender in the background, never on an open file):
  blender -b --factory-startup --python tools/blender/animation/a3_clips.py -- <Mixamo folder> <export folder> <preview folder or ->
"""

import json
import math
import os
import sys

import bpy
import numpy as np
from mathutils import Matrix, Quaternion, Vector

FPS = 30
BODY = "human_body_3d_model@"

argv = sys.argv[sys.argv.index("--") + 1:]
MIXAMO, OUT, PREVIEW = argv[0], argv[1], argv[2] if len(argv) > 2 else "-"

scene = bpy.context.scene
scene.render.fps = FPS


def update():
    bpy.context.view_layer.update()


# ----------------------------------------------------------------------------------------------- sources

def load(name):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=f"{MIXAMO}/{BODY}{name}.fbx")
    return [o for o in bpy.data.objects if o not in before and o.type == "ARMATURE"][0]


def first_frame(arm):
    return int(arm.animation_data.action.frame_range[0])


def capture(arm):
    """A source's first frame: every bone's local pose, and every bone's world matrix (the armature object moves too)."""
    scene.frame_set(first_frame(arm))
    update()
    w = arm.matrix_world.copy()
    return {
        "basis": {pb.name: pb.matrix_basis.copy() for pb in arm.pose.bones},
        "world": {pb.name: w @ pb.matrix for pb in arm.pose.bones},
    }


sources = {}
for name in ("Breathing Idle", "Great Sword Idle", "Sword And Shield Idle", "Kneeling Idle"):
    arm = load(name)
    sources[name] = capture(arm)
    bpy.data.objects.remove(arm, do_unlink=True)
IDLE, GREAT, SWORD, KNEEL = (sources[n] for n in ("Breathing Idle", "Great Sword Idle", "Sword And Shield Idle", "Kneeling Idle"))

# The working armature: Breathing Idle's own, its animation cleared; the object keeps its imported transform.
work = load("Breathing Idle")
scene.frame_set(first_frame(work))
update()
OBJECT = work.matrix_world.copy()
work.animation_data_clear()
work.matrix_world = OBJECT
for pb in work.pose.bones:
    pb.rotation_mode = "QUATERNION"
update()

SIDES = ("Left", "Right")
SPINE = ("Spine", "Chest", "UpperChest")
NECK = ("Neck", "Head")
ARM_PARTS = ("UpperArm", "LowerArm", "Hand", "Thumb", "Index", "Middle", "Ring", "Pinky")
ARM_GRIP = tuple(n for n in IDLE["basis"] if any(n.startswith(f"{s}_{part}") for s in SIDES for part in ARM_PARTS))


def bone_world(name):
    return work.matrix_world @ work.pose.bones[name].matrix


def head(name):
    return bone_world(name).translation.copy()


def set_world(name, m):
    work.pose.bones[name].matrix = work.matrix_world.inverted() @ m
    update()


def rotate_world(name, q):
    """Turns a bone about its own head by the world rotation q."""
    m = bone_world(name)
    set_world(name, Matrix.Translation(m.translation) @ (q.to_matrix() @ m.to_3x3()).to_4x4())


def apply_base(base, grip=None):
    """A source frame on the working armature, the arms and hands from the grip if one is given."""
    for pb in work.pose.bones:
        source = grip if (grip is not None and pb.name in ARM_GRIP) else base
        pb.matrix_basis = source["basis"][pb.name]
    update()
    set_world("Hips", base["world"]["Hips"])


# ----------------------------------------------------------------------------------------------- the character's frame

apply_base(IDLE)
GROUND = min(head(f"{s}_ToesEnd").z for s in SIDES)
ORIGIN = (head("Left_Foot") + head("Right_Foot")) / 2
ORIGIN.z = GROUND
RIGHT = head("Right_UpperArm") - head("Left_UpperArm")
RIGHT.z = 0
RIGHT.normalize()
UP = Vector((0, 0, 1))
FORWARD = UP.cross(RIGHT).normalized()
if (head("Left_Toes") - head("Left_Foot")).dot(FORWARD) < 0:  # the toes point forward; if not, the shoulders were read the wrong way round
    RIGHT, FORWARD = -RIGHT, -FORWARD
PITCH_AXIS = UP.cross(FORWARD)  # a positive turn about it bends the upper body forward
K = ((head("Left_UpperArm").z + head("Right_UpperArm").z) / 2 - GROUND) / 1.40  # the armature's size against the reference body


def turn(degrees):
    """A turn about the vertical, positive to the character's right."""
    return Quaternion(UP, math.radians(-degrees))


def at(x, y, z):
    """A point in the character's frame (metres from the ground between its feet) in the world."""
    return ORIGIN + (RIGHT * x + FORWARD * y + UP * z) * K


def direction(x, y, z):
    return (RIGHT * x + FORWARD * y + UP * z).normalized()


def local(p):
    d = (p - ORIGIN) / K
    return [round(d.dot(RIGHT), 3), round(d.dot(FORWARD), 3), round(d.dot(UP), 3)]


def hinge(pose, upper, lower, end):
    """The elbow's or knee's hinge in the upper bone's own frame, from a pose where it is clearly bent."""
    a, b, c = (pose["world"][n].translation for n in (upper, lower, end))
    axis = (b - a).cross(c - b).normalized()
    return pose["world"][upper].to_3x3().normalized().inverted() @ axis


HINGES = {}
for s in SIDES:
    HINGES[f"{s}_UpperArm"] = hinge(GREAT, f"{s}_UpperArm", f"{s}_LowerArm", f"{s}_Hand")
    HINGES[f"{s}_UpperLeg"] = hinge(KNEEL, f"{s}_UpperLeg", f"{s}_LowerLeg", f"{s}_Foot")

MEASURES = {
    "size against the reference body": round(K, 4),
    "object scale": [round(v, 4) for v in OBJECT.to_scale()],
    "shoulders": local((head("Left_UpperArm") + head("Right_UpperArm")) / 2),
    "right shoulder": local(head("Right_UpperArm")),
    "hips": local(head("Hips")),
    "head": local(head("Head")),
    "right hand": local(head("Right_Hand")),
    "upper arm": round((head("Right_LowerArm") - head("Right_UpperArm")).length / K, 3),
    "forearm": round((head("Right_Hand") - head("Right_LowerArm")).length / K, 3),
    "upper leg": round((head("Right_LowerLeg") - head("Right_UpperLeg")).length / K, 3),
    "lower leg": round((head("Right_Foot") - head("Right_LowerLeg")).length / K, 3),
}
print("[a3] measures", json.dumps(MEASURES))


# ----------------------------------------------------------------------------------------------- IK

def ik(upper, lower, end, target, pole):
    """Two-bone IK: the upper bone turned so its hinge lies across the plane of the bend toward the pole, the
    lower bone bent about it to reach the target (straightened toward it where it is out of reach)."""
    s, e0, t0 = head(upper), head(lower), head(end)
    l1, l2 = (e0 - s).length, (t0 - e0).length
    to = target - s
    d = min(max(to.length, abs(l1 - l2) + 1e-4), l1 + l2 - 1e-4)
    u = to.normalized()
    v = pole - u * pole.dot(u)
    v = v.normalized() if v.length > 1e-6 else u.orthogonal().normalized()
    a = (l1 * l1 - l2 * l2 + d * d) / (2 * d)
    h = math.sqrt(max(l1 * l1 - a * a, 0.0))
    elbow = s + u * a + v * h
    n = v.cross(u).normalized()

    y = (e0 - s).normalized()
    c = bone_world(upper).to_3x3().normalized() @ HINGES[upper]
    c = (c - y * c.dot(y)).normalized()
    yd = (elbow - s).normalized()
    nd = (n - yd * n.dot(yd)).normalized()
    now = Matrix((y, c, y.cross(c))).transposed()
    want = Matrix((yd, nd, yd.cross(nd))).transposed()
    rotate_world(upper, (want @ now.transposed()).to_quaternion())

    e1 = head(lower)
    rotate_world(lower, (head(end) - e1).normalized().rotation_difference((s + u * d - e1).normalized()))


PALM_SIGN = {"Left": 1.0, "Right": 1.0}


def palm(side):
    """The palm's normal, signed so that it faces the thigh in the idle."""
    hand = head(f"{side}_Hand")
    n = (head(f"{side}_IndexProximal") - hand).cross(head(f"{side}_PinkyProximal") - hand).normalized()
    return n * PALM_SIGN[side]


apply_base(IDLE)
for s in SIDES:
    inward = RIGHT if s == "Left" else -RIGHT
    PALM_SIGN[s] = 1.0 if palm(s).dot(inward) > 0 else -1.0
REST_HAND = {s: head(f"{s}_Hand") for s in SIDES}
REST_POLE = {s: (head(f"{s}_LowerArm") - (head(f"{s}_UpperArm") + head(f"{s}_Hand")) / 2).normalized() for s in SIDES}


# ----------------------------------------------------------------------------------------------- curves

PARAMS = ("hips_y", "hips_z", "hips_yaw", "hips_pitch", "pitch", "yaw", "look",
          "lx", "ly", "lz", "rx", "ry", "rz", "lpx", "lpy", "lpz", "rpx", "rpy", "rpz")


def vector(key):
    return np.array([float(key.get(p, 0.0)) for p in PARAMS])


def curve(keys, frame, period):
    """Hermite curves through the key poses of a loop: Catmull-Rom tangents, none at a held key."""
    count = len(keys)
    times = [k["f"] for k in keys]
    values = [vector(k) for k in keys]

    def tangent(i):
        i %= count
        if keys[i].get("hold", False):
            return np.zeros(len(PARAMS))
        j, k = (i - 1) % count, (i + 1) % count
        tj = times[j] - (period if j >= i else 0)
        tk = times[k] + (period if k <= i else 0)
        return (values[k] - values[j]) / (tk - tj)

    frame %= period
    for i in range(count):
        t0, t1 = times[i], times[i + 1] if i + 1 < count else period
        if t0 <= frame <= t1:
            dt = t1 - t0
            s = (frame - t0) / dt
            h00, h10, h01, h11 = 2 * s**3 - 3 * s**2 + 1, s**3 - 2 * s**2 + s, -2 * s**3 + 3 * s**2, s**3 - s**2
            return h00 * values[i] + h10 * dt * tangent(i) + h01 * values[(i + 1) % count] + h11 * dt * tangent(i + 1)
    return values[0]


def ease(frame, start, end):
    s = min(max((frame - start) / (end - start), 0.0), 1.0)
    return s * s * (3 - 2 * s)


# ----------------------------------------------------------------------------------------------- one frame

def tool_frame(p, grip):
    """A frame of a tool's swing: the idle body, the grip's arms and hands, the hips, spine and head turned, the
    feet kept where they stood, and the hands on their targets."""
    apply_base(IDLE, grip)
    feet = {s: bone_world(f"{s}_Foot") for s in SIDES}

    hips = bone_world("Hips")
    moved = Matrix.Translation(hips.translation + (FORWARD * p["hips_y"] + UP * p["hips_z"]) * K)
    set_world("Hips", moved @ (turn(p["hips_yaw"]).to_matrix() @ hips.to_3x3()).to_4x4())
    rotate_world("Hips", Quaternion(PITCH_AXIS, math.radians(p["hips_pitch"])))
    for name in SPINE:
        rotate_world(name, turn(p["yaw"] / 3))
        rotate_world(name, Quaternion(PITCH_AXIS, math.radians(p["pitch"] / 3)))
    for name in NECK:
        rotate_world(name, turn(-(p["yaw"] + p["hips_yaw"]) / 4))
        rotate_world(name, Quaternion(PITCH_AXIS, math.radians(p["look"] / 2)))

    for s in SIDES:
        side = RIGHT if s == "Right" else -RIGHT
        ik(f"{s}_UpperLeg", f"{s}_LowerLeg", f"{s}_Foot", feet[s].translation, FORWARD + side * 0.15)
        m = bone_world(f"{s}_Foot")
        set_world(f"{s}_Foot", Matrix.Translation(m.translation) @ feet[s].to_3x3().to_4x4())

    ik("Left_UpperArm", "Left_LowerArm", "Left_Hand", at(p["lx"], p["ly"], p["lz"]), direction(p["lpx"], p["lpy"], p["lpz"]))
    ik("Right_UpperArm", "Right_LowerArm", "Right_Hand", at(p["rx"], p["ry"], p["rz"]), direction(p["rpx"], p["rpy"], p["rpz"]))
    for s, x, y, z in (("Left", p["lx"], p["ly"], p["lz"]), ("Right", p["rx"], p["ry"], p["rz"])):
        REACH[s] = {"shoulder": local(head(f"{s}_UpperArm")), "wrist": local(head(f"{s}_Hand")), "short by": round((head(f"{s}_Hand") - at(x, y, z)).length / K, 3)}


REACH = {}


def blend_pose(a, b, w):
    """Every bone's local pose between two source frames, the hips in the world."""
    for pb in work.pose.bones:
        qa, qb = a["basis"][pb.name].to_quaternion(), b["basis"][pb.name].to_quaternion()
        pb.matrix_basis = qa.slerp(qb, w).to_matrix().to_4x4()
    update()
    ha, hb = a["world"]["Hips"], b["world"]["Hips"]
    q = ha.to_quaternion().slerp(hb.to_quaternion(), w)
    set_world("Hips", Matrix.LocRotScale(ha.translation.lerp(hb.translation, w), q, ha.to_scale()))


def hands_up_target(s):
    out = RIGHT if s == "Right" else -RIGHT
    return head(f"{s}_UpperArm") + (out * 0.24 + FORWARD * 0.06 + UP * 0.30) * K, (out - UP * 0.5).normalized()


def forearm_twist(s, degrees):
    axis = (head(f"{s}_Hand") - head(f"{s}_LowerArm")).normalized()
    rotate_world(f"{s}_LowerArm", Quaternion(axis, math.radians(degrees)))


# The forearm's twist that turns each palm forward with the hands up, found once standing.
YIELD_TWIST = {}
apply_base(IDLE)
for s in SIDES:
    target, pole = hands_up_target(s)
    ik(f"{s}_UpperArm", f"{s}_LowerArm", f"{s}_Hand", target, pole)
    best = (-2.0, 0)
    for deg in range(-150, 151, 10):
        forearm_twist(s, deg)
        best = max(best, (palm(s).dot(FORWARD), deg))
        forearm_twist(s, -deg)
    YIELD_TWIST[s] = {"degrees": best[1], "palm forward": round(best[0], 3)}


# Kneeling: the hips go down first, then each foot steps back in a low arc to where Kneeling Idle has it, the right
# one first, its knee turning from forward to Kneeling Idle's own bend.
KNEE_POLE = {s: (KNEEL["world"][f"{s}_LowerLeg"].translation
                 - (KNEEL["world"][f"{s}_UpperLeg"].translation + KNEEL["world"][f"{s}_Foot"].translation) / 2).normalized() for s in SIDES}
STEP = {"Right": (22, 36), "Left": (32, 46)}


def bezier(a, b, c, t):
    return a * (1 - t) ** 2 + b * (2 * t * (1 - t)) + c * t * t


def yield_frame(f):
    """Surrender: the hands come up in front and open beside the head, palms forward, then the body goes down on
    its knees and the head bows."""
    raise_w = ease(f, 4, 18)
    blend_pose(IDLE, KNEEL, ease(f, 16, 44))
    for s in SIDES:
        idle_foot, kneel_foot = IDLE["world"][f"{s}_Foot"], KNEEL["world"][f"{s}_Foot"]
        w = ease(f, *STEP[s])
        spot = idle_foot.translation.lerp(kneel_foot.translation, w) + UP * (0.08 * K * math.sin(math.pi * w))
        pole = FORWARD.lerp(KNEE_POLE[s], w).normalized()
        ik(f"{s}_UpperLeg", f"{s}_LowerLeg", f"{s}_Foot", spot, pole)
        m = bone_world(f"{s}_Foot")
        q = idle_foot.to_quaternion().slerp(kneel_foot.to_quaternion(), w)
        set_world(f"{s}_Foot", Matrix.LocRotScale(m.translation, q, idle_foot.to_scale()))
    for name in NECK:
        rotate_world(name, Quaternion(PITCH_AXIS, math.radians(12 * ease(f, 40, 60))))
    for s in SIDES:
        out = RIGHT if s == "Right" else -RIGHT
        up, up_pole = hands_up_target(s)
        ahead = head(f"{s}_UpperArm") + (out * 0.10 + FORWARD * 0.30 - UP * 0.12) * K
        target = bezier(REST_HAND[s], ahead, up, raise_w)
        pole = bezier(REST_POLE[s], (-UP + out * 0.4).normalized(), up_pole, raise_w).normalized()
        ik(f"{s}_UpperArm", f"{s}_LowerArm", f"{s}_Hand", target, pole)
        forearm_twist(s, YIELD_TWIST[s]["degrees"] * raise_w)


# ----------------------------------------------------------------------------------------------- the clips

# The arm reaches 0.50 m from the shoulder to the wrist (measured, reference metres), so every target stays within it.
# The body bends at the hips (hips_pitch) and along the spine (pitch); look bends the neck on top of both.

MINE = [  # the pick raised over the head, struck at the ground ahead, pulled free, lifted past the right shoulder
    dict(f=0, hold=True, pitch=-8, yaw=8, look=30,
         lx=0.0, ly=0.14, lz=1.62, rx=0.08, ry=0.0, rz=1.74, lpx=-0.7, lpy=0.5, lpz=0.0, rpx=0.7, rpy=0.3, rpz=0.0),
    dict(f=10, hold=True, hips_y=-0.08, hips_z=-0.10, hips_pitch=20, pitch=30, look=5,
         lx=-0.02, ly=0.40, lz=0.88, rx=0.02, ry=0.50, rz=0.80, lpx=-0.5, lpy=0.0, lpz=-1.0, rpx=0.5, rpy=0.0, rpz=-1.0),
    dict(f=16, hips_y=-0.06, hips_z=-0.08, hips_pitch=16, pitch=26, look=8,
         lx=-0.02, ly=0.38, lz=0.95, rx=0.02, ry=0.48, rz=0.88, lpx=-0.5, lpy=0.0, lpz=-1.0, rpx=0.5, rpy=0.0, rpz=-1.0),
    dict(f=32, hips_y=-0.02, hips_z=-0.02, hips_pitch=5, pitch=8, yaw=14, look=15,
         lx=0.08, ly=0.25, lz=1.30, rx=0.20, ry=0.18, rz=1.48, lpx=-0.2, lpy=0.6, lpz=-0.8, rpx=0.8, rpy=-0.2, rpz=-0.6),
]

CHOP = [  # the axe drawn back over the right shoulder, driven into a trunk ahead at the waist, a diagonal blow to the left
    dict(f=0, hold=True, hips_yaw=10, pitch=-5, yaw=30, look=10,
         lx=0.18, ly=0.05, lz=1.42, rx=0.30, ry=-0.08, rz=1.58, lpx=-0.3, lpy=0.8, lpz=-0.5, rpx=0.8, rpy=0.0, rpz=-0.6),
    dict(f=11, hold=True, hips_yaw=-8, hips_z=-0.04, hips_pitch=8, pitch=15, yaw=-20, look=12,
         lx=0.06, ly=0.30, lz=1.02, rx=0.0, ry=0.45, rz=1.02, lpx=-0.8, lpy=0.0, lpz=-0.6, rpx=0.3, rpy=0.2, rpz=-1.0),
    dict(f=17, hips_yaw=-6, hips_z=-0.03, hips_pitch=6, pitch=12, yaw=-12, look=12,
         lx=0.08, ly=0.28, lz=1.06, rx=0.04, ry=0.42, rz=1.08, lpx=-0.8, lpy=0.0, lpz=-0.6, rpx=0.3, rpy=0.2, rpz=-1.0),
    dict(f=30, hips_yaw=6, hips_z=-0.01, hips_pitch=2, yaw=18, look=10,
         lx=0.14, ly=0.18, lz=1.28, rx=0.25, ry=0.10, rz=1.45, lpx=-0.3, lpy=0.8, lpz=-0.5, rpx=0.8, rpy=0.0, rpz=-0.6),
]

HAMMER = [  # the hammer raised beside the head, elbow out and back, down on the anvil, a rebound; the left hand holds the tongs still
    dict(f=0, hold=True, hips_pitch=10, pitch=15, yaw=8, look=22,
         lx=-0.12, ly=0.40, lz=0.95, rx=0.22, ry=0.12, rz=1.60, lpx=-1.0, lpy=0.0, lpz=-0.5, rpx=0.6, rpy=-0.6, rpz=0.0),
    dict(f=8, hold=True, hips_pitch=12, pitch=18, yaw=-3, look=24,
         lx=-0.12, ly=0.40, lz=0.94, rx=0.10, ry=0.45, rz=0.98, lpx=-1.0, lpy=0.0, lpz=-0.5, rpx=0.5, rpy=-0.2, rpz=-0.8),
    dict(f=13, hips_pitch=11, pitch=17, yaw=-1, look=24,
         lx=-0.12, ly=0.40, lz=0.95, rx=0.14, ry=0.40, rz=1.12, lpx=-1.0, lpy=0.0, lpz=-0.5, rpx=0.6, rpy=-0.3, rpz=-0.6),
    dict(f=22, hips_pitch=10, pitch=16, yaw=5, look=23,
         lx=-0.12, ly=0.40, lz=0.95, rx=0.22, ry=0.25, rz=1.42, lpx=-1.0, lpy=0.0, lpz=-0.5, rpx=0.8, rpy=-0.4, rpz=-0.2),
]

CLIPS = [
    ("Mine", 44, lambda f: tool_frame(dict(zip(PARAMS, curve(MINE, f, 44))), GREAT)),
    ("Chop", 40, lambda f: tool_frame(dict(zip(PARAMS, curve(CHOP, f, 40))), GREAT)),
    ("Hammer", 30, lambda f: tool_frame(dict(zip(PARAMS, curve(HAMMER, f, 30))), SWORD)),
    ("Yield", 66, yield_frame),
]


def key_frames(pose, frames):
    work.animation_data_clear()
    previous = {}
    for f in range(frames + 1):
        pose(f)
        for pb in work.pose.bones:
            q = pb.rotation_quaternion.copy()
            if pb.name in previous and q.dot(previous[pb.name]) < 0:
                q.negate()
                pb.rotation_quaternion = q
            previous[pb.name] = q
            pb.keyframe_insert("rotation_quaternion", frame=f)
            if pb.name == "Hips":
                pb.keyframe_insert("location", frame=f)
    scene.frame_start, scene.frame_end = 0, frames


def export(name):
    work.animation_data.action.name = name
    bpy.ops.object.select_all(action="DESELECT")
    work.select_set(True)
    bpy.context.view_layer.objects.active = work
    os.makedirs(OUT, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(OUT, f"{name}.fbx"), use_selection=True, object_types={"ARMATURE"}, add_leaf_bones=False,
        armature_nodetype="NULL", primary_bone_axis="Y", secondary_bone_axis="X",
        bake_anim=True, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0)


# ----------------------------------------------------------------------------------------------- preview

# The previews are drawn, not rendered: every bone a line, the left side blue, the right red, the handle of a
# two-handed tool yellow, seen from the front, from the right and from above, in the reference body's metres.
DRAWN = [pb.name for pb in work.pose.bones if not (pb.name.endswith("End") or "Eye" in pb.name or pb.name == "Neck_Twist_A")]
PX = 110  # pixels a metre
TILE = (int(2.2 * PX), int(2.4 * PX))  # width, height
VIEWS = (  # name, the axis drawn across (index, sign), the axis drawn up, the window's left and top edges, the axis toward the eye
    ("front", (0, -1), (2, 1), -1.1, 2.25, 1),
    ("side", (1, 1), (2, 1), -1.0, 2.25, 0),
    ("top", (0, 1), (1, 1), -1.1, 1.5, 2),
)
BLUE, RED, GREY, YELLOW = (0.3, 0.5, 1.0, 1.0), (1.0, 0.35, 0.3, 1.0), (0.9, 0.9, 0.9, 1.0), (1.0, 0.85, 0.2, 1.0)


def frame_point(p):
    d = (p - ORIGIN) / K
    return np.array((d.dot(RIGHT), d.dot(FORWARD), d.dot(UP)))


def segments(tool):
    w = work.matrix_world
    out = []
    for name in DRAWN:
        pb = work.pose.bones[name]
        finger = any(part in name for part in ("Thumb", "Index", "Middle", "Ring", "Pinky"))
        colour = BLUE if name.startswith("Left") else RED if name.startswith("Right") else GREY
        radius = 7 if name == "Head" else 1 if finger else 4 if name in ("Hips",) + SPINE else 3
        out.append((frame_point(w @ pb.head), frame_point(w @ pb.tail), colour, radius))
    if tool:
        a, b = (frame_point(w @ work.pose.bones[f"{s}_MiddleProximal"].head) for s in SIDES)
        d = (b - a) / np.linalg.norm(b - a)
        out.append((a - d * 0.08, a + d * 0.85, YELLOW, 2))
    return out


def tile(view, segs):
    _, (ia, sa), (ib, sb), left, top, eye = view
    img = np.empty((TILE[1], TILE[0], 4), np.float32)
    img[:] = (0.16, 0.16, 0.17, 1.0)
    if eye != 2:
        img[int(top * PX):int(top * PX) + 2, :] = (0.45, 0.45, 0.45, 1.0)
    for p, q, colour, r in sorted(segs, key=lambda s: s[0][eye] + s[1][eye]):
        u0 = np.array(((p[ia] * sa - left) * PX, (top - p[ib] * sb) * PX))
        u1 = np.array(((q[ia] * sa - left) * PX, (top - q[ib] * sb) * PX))
        for t in np.linspace(0.0, 1.0, int(np.abs(u1 - u0).max()) + 2):
            x, y = (u0 + (u1 - u0) * t).astype(int)
            img[max(y - r, 0):max(y + r + 1, 0), max(x - r, 0):max(x + r + 1, 0)] = colour
    img[:, -1] = img[-1, :] = (0.3, 0.3, 0.3, 1.0)
    return img


def preview(name, frames, tool):
    picks = [round(i * frames / 7) for i in range(8)]
    shots = []
    for f in picks:
        scene.frame_set(f)
        update()
        shots.append(segments(tool))
    sheet = np.concatenate([np.concatenate([tile(view, segs) for segs in shots], axis=1) for view in VIEWS], axis=0)
    sheet = np.ascontiguousarray(sheet[::-1])  # Blender's pixels run bottom-up
    out = bpy.data.images.new(f"{name}_sheet", sheet.shape[1], sheet.shape[0], alpha=True)
    out.pixels.foreach_set(sheet.ravel())
    out.filepath_raw = os.path.join(PREVIEW, f"a3_{name}.png")
    out.file_format = "PNG"
    out.save()
    return picks


report = {"measures": MEASURES, "palm twist": YIELD_TWIST, "clips": {}}
KEYS = {"Mine": MINE, "Chop": CHOP, "Hammer": HAMMER}
for name, frames, pose in CLIPS:
    key_frames(pose, frames)
    export(name)
    report["clips"][name] = {"frames": frames + 1, "seconds": round(frames / FPS, 2)}
    if name in KEYS:  # where each hand ends at each key pose, and how far short of its target (reference metres)
        reach = {}
        for k in KEYS[name]:
            pose(k["f"])
            reach[k["f"]] = {s: dict(v) for s, v in REACH.items()}
        report["clips"][name]["reach"] = reach
    if PREVIEW != "-":
        os.makedirs(PREVIEW, exist_ok=True)
        report["clips"][name]["preview frames"] = preview(name, frames, name in ("Mine", "Chop"))
if PREVIEW != "-":
    with open(os.path.join(PREVIEW, "a3_report.json"), "w", encoding="utf-8") as fh:
        json.dump(report, fh, indent=1)
print("[a3] done", json.dumps(report))
