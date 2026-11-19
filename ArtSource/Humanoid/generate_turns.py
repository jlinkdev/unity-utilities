"""Bake planted step turns in the rotating frame used by the Unity motor."""
import sys
from math import pi, radians, sin
from pathlib import Path

from mathutils import Quaternion, Vector

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from generate_locomotion import OUTPUT, UP, X, bake_takes, frame_pose, smooth


def pulse(phase, start, peak, end):
    if phase <= start or phase >= end:
        return 0
    if phase < peak:
        return smooth((phase - start) / (peak - start))
    return smooth((end - phase) / (end - peak))


def turn_upper_body(pose, phase, degrees):
    lead = 1 if degrees > 0 else -1
    strength = .35 + .65 * (abs(degrees) - 45) / 135
    fore_aft = Vector((0, 1, 0))

    def rotate(name, rotation):
        # Express the offset in the idle joint's axes so it inherits the moving
        # chest and shoulder. Fingers keep their relaxed articulation this way.
        idle = pose.rotation[name]
        joint = pose.rig.pose.bones[name]
        joint.rotation_quaternion = joint.rotation_quaternion @ (idle.inverted() @ rotation @ idle)

    wind = pulse(phase, 0, .30, .79)
    settle = pulse(phase, .42, .76, 1)
    twist = lead * radians(strength * (16 * wind - 4 * settle))
    bank = lead * radians(strength * (3 * wind - 2 * settle))
    for name, share in (("spine_01", .30), ("spine_02", .40), ("spine_03", .30)):
        rotate(name, Quaternion(UP, twist * share) @ Quaternion(fore_aft, bank * share))
    look = pulse(phase, 0, .23, .83)
    rotate("neck_01", Quaternion(UP, lead * radians((6 + 6 * strength) * look)))

    # The outside shoulder comes forward. The arms gather as the turn picks
    # up speed and swing back toward neutral as the feet finish stepping.
    carry = pulse(phase, .06, .43, .86)
    follow = pulse(phase, .49, .79, 1)
    elbow = pulse(phase, .11, .53, .98)
    for side, sign in (("_l", 1), ("_r", -1)):
        inside = sign == lead
        swing = (10 if inside else -14) * carry + (-8 if inside else 7) * follow
        open_arm = (8 if inside else 4) * carry + (3 if inside else 5) * follow
        rotate("clavicle" + side,
               Quaternion(UP, lead * radians(strength * (4 * wind - 2 * settle))) @
               Quaternion(fore_aft, -sign * radians(strength * (2 if inside else 3) * carry)))
        rotate("upperarm" + side, Quaternion(X, radians(strength * swing)) @
               Quaternion(fore_aft, -sign * radians(strength * open_arm)))
        rotate("lowerarm" + side, Quaternion(X, -radians(strength * (9 if inside else 15) * elbow)))
        rotate("hand" + side, Quaternion(X, radians(strength * (4 * elbow - 3 * follow))))


def turn_pose(rig, phase, degrees, _period, _name):
    pose = frame_pose(rig, 0, 0, 1, "Relaxed Idle")
    angle = radians(degrees)
    facing = angle * smooth(phase)
    into_body = Quaternion(UP, -facing)
    pairs = 1 if abs(degrees) <= 90 else 2
    count = 2 * pairs
    step = min(int(phase * count), count - 1)
    progress = phase * count - step
    lift = min(1, max(0, (progress - .10) / .80))
    airborne = sin(pi * lift) ** 2
    lead = 1 if degrees > 0 else -1
    moving = lead if step % 2 == 0 else -lead
    ankles = {}
    for side, sign in (("_l", 1), ("_r", -1)):
        completed = (step + (sign == lead)) // 2
        foot_angle = angle * (completed + (smooth(lift) if sign == moving else 0)) / pairs
        contact = Quaternion(UP, foot_angle) @ Vector((sign*.114, -.015-sign*.018, .073))
        ankle = into_body @ contact
        if sign == moving:
            ankle.x += sign*.018*airborne
            ankle.z += .059*airborne
        ankles[side] = (ankle, foot_angle - facing + sign*radians(5))

    # Load the supporting leg before the other shoe leaves the floor.
    support = ankles["_r" if moving == 1 else "_l"][0]
    weight = sin(pi * progress) ** 2
    offset = Vector((.26*support.x*weight, .20*support.y*weight, -.023*weight))
    pelvis = rig.data.bones["pelvis"]
    base_height = pose.position["pelvis"].z
    offset.z += base_height - pelvis.head_local.z
    pose.place(pelvis, offset=offset)
    for side, (ankle, toe_yaw) in ankles.items():
        hip = pose.position["pelvis"] + rig.data.bones["thigh"+side].head_local - pelvis.head_local
        pose.leg(side, hip, ankle, 0, toe_yaw, 0)

    turn_upper_body(pose, phase, degrees)
    return pose


if __name__ == "__main__":
    takes = [(f"Turn {side} {angle}", frames, sign * angle)
             for side, sign in (("Left", 1), ("Right", -1))
             for angle, frames in ((45, 25), (90, 34), (135, 46), (180, 52))]
    bake_takes(OUTPUT.with_name("Step Turns.fbx"), takes, turn_pose)
