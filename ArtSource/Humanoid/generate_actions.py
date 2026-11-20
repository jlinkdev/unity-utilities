"""Bake the playable Humanoid's curved strides and one-shot gestures."""
import sys
from math import cos, radians, sin
from pathlib import Path

from mathutils import Quaternion, Vector

sys.dont_write_bytecode = True
sys.path.insert(0, str(Path(__file__).resolve().parent))
from generate_locomotion import OUTPUT, UP, X, bake_takes, frame_pose, smooth

CONTACT_FINGERS = (
    ("index", (2, 5, 8), -.035), ("middle", (7, 14, 20), -.012),
    ("ring", (10, 19, 27), .022), ("pinky", (12, 23, 32), .068),
)

def ramp(phase, start, end):
    return smooth(max(0, min(1, (phase - start) / (end - start))))


def corner_pose(rig, phase, direction, period, name):
    gait = name.split()[0]
    return frame_pose(rig, phase, 5.5 if gait == "Sprint" else 3.2, period, gait, direction)


def reaching_hand(pose, phase, reach):
    bones = pose.rig.pose.bones
    digits = ("thumb", "index", "middle", "ring", "pinky")
    joints = ["hand_r"] + [f"{digit}_0{i}_r" for digit in digits for i in (1, 2, 3)]
    resting = [bones[name].rotation_quaternion.copy() for name in joints]
    # Turn the palm toward the control. Lead with the index, with the other
    # fingertips progressively softer. Ease the fingers open before contact.
    pose.hand("_r", -1, radians(9 + 79*reach), swing=0,
              wrist_roll=radians(-52*reach), finger_shape=CONTACT_FINGERS)
    opening = ramp(phase, .02, .32) * (1 - ramp(phase, .62, .96))
    for name, idle in zip(joints, resting):
        joint = bones[name]
        joint.rotation_quaternion = idle.slerp(joint.rotation_quaternion, opening)


def interact_pose(rig, phase, _parameter, _period, _name):
    pose = frame_pose(rig, 0, 0, 1, "Relaxed Idle")
    bones = rig.data.bones
    for joint in range(1, 4):
        pose.place(bones[f"thumb_0{joint}_r"])
    reach = ramp(phase, .04, .44) * (1 - ramp(phase, .56, .96))
    twist = 8*reach
    lean = 5*reach
    pelvis = bones["pelvis"]
    shift = Vector((-.012*reach, -.019*reach, -.036*reach))
    hip_yaw = .32*twist
    hip_delta = Quaternion(UP, radians(hip_yaw))
    shift.z += pose.position["pelvis"].z - pelvis.head_local.z
    pose.place(pelvis, delta=hip_delta, offset=shift)
    for side, sign in (("_l", 1), ("_r", -1)):
        ankle = Vector((sign*.114, -.015-sign*.018, .073))
        toe_yaw = sign*5
        hip = pose.position["pelvis"] + hip_delta @ (bones["thigh"+side].head_local-pelvis.head_local)
        pose.leg(side, hip, ankle, 0, radians(toe_yaw), 0)

    for bone, share in (("spine_01", .30), ("spine_02", .40), ("spine_03", .30)):
        pose.place(bones[bone], delta=Quaternion(UP, radians(.68*twist*share)) @
                   Quaternion(X, radians(lean*share)))
    pose.place(bones["neck_01"], delta=Quaternion(UP, radians(-.55*twist)) @
               Quaternion(X, radians(-.55*lean)))
    pose.place(bones["head"])

    for side, sign in (("_l", 1), ("_r", -1)):
        pose.place(bones["clavicle"+side], delta=Quaternion(UP, radians(-sign*4*reach)))
        if sign == -1:
            upper = -3 + 66*reach
            bend = 12 + 21*reach
            azimuth = 11*reach
        else:
            upper = -3 - 6*reach
            bend = 12 + 4*reach
            azimuth = 8*reach
        shoulder = pose.parent_delta(bones["upperarm"+side])
        swivel = shoulder @ Quaternion(UP, radians(azimuth))
        upper, lower = radians(upper), radians(upper + bend)
        pose.place(bones["upperarm"+side], direction=swivel @ Vector((sign*.18, -sin(upper), -cos(upper))))
        pose.place(bones["lowerarm"+side], direction=swivel @ Vector((sign*.035, -sin(lower), -cos(lower))))

    reaching_hand(pose, phase, reach)
    return pose


if __name__ == "__main__":
    turns = [(f"{gait} {side}", frames, sign)
             for gait, frames in (("Jog", 44), ("Sprint", 36))
             for side, sign in (("Left", 1), ("Right", -1))]
    if "--actions-only" not in sys.argv:
        bake_takes(OUTPUT.with_name("Running Turns.fbx"), turns, corner_pose)
    bake_takes(OUTPUT.with_name("Character Actions.fbx"),
               [("Interact", 52, 0)], interact_pose,
               authored_action="Sword Swipe")
