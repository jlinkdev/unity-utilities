"""Bake neutral in-place locomotion takes on the character's deform rig."""
from math import cos, pi, radians, sin, sqrt
from pathlib import Path

import bpy
from mathutils import Matrix, Quaternion, Vector

HERE = Path(__file__).resolve().parent
OUTPUT = HERE.parents[1] / "Assets/Humanoid/Animations/Character Motion.fbx"
TAU = 2 * pi
UP = Vector((0, 0, 1))
X = Vector((1, 0, 0))


class Pose:
    def __init__(self, rig):
        self.rig = rig
        self.rotation = {}
        self.position = {}
        self.rest = {b.name: b.matrix_local.to_quaternion() for b in rig.data.bones}

    def parent_delta(self, bone):
        if bone.parent is None:
            return Quaternion()
        name = bone.parent.name
        return self.rotation[name] @ self.rest[name].inverted()

    def place(self, bone, delta=None, direction=None, offset=None):
        parent = self.parent_delta(bone)
        rotation = parent @ self.rest[bone.name]
        if direction is not None:
            axis = parent @ (bone.tail_local - bone.head_local)
            delta = axis.rotation_difference(direction.normalized())
        target = rotation if delta is None else delta @ rotation
        pose = self.rig.pose.bones[bone.name]
        pose.rotation_quaternion = rotation.inverted() @ target
        if bone.parent:
            pos = self.position[bone.parent.name] + parent @ (bone.head_local - bone.parent.head_local)
        else:
            pos = bone.head_local.copy()
        if offset is not None:
            pose.location = rotation.inverted() @ offset
            pos += offset
        self.rotation[bone.name] = target
        self.position[bone.name] = pos

    def leg(self, side, hip, ankle, pitch, toe_out, toe_roll):
        bones = self.rig.data.bones
        thigh, shin, foot = [bones[name + side] for name in ("thigh", "calf", "foot")]
        delta = ankle - hip
        axis = delta.normalized()
        length = min(delta.length, thigh.length + shin.length - .0001)
        along = (thigh.length**2 - shin.length**2 + length**2) / (2 * length)
        front = Vector((.045 if side == "_l" else -.045, -1, 0))
        pole = (front - axis * front.dot(axis)).normalized()
        knee = hip + axis * along + pole * sqrt(max(0, thigh.length**2 - along**2))
        self.place(thigh, direction=knee-hip)
        self.place(shin, direction=hip + length*axis - knee)
        desired = Quaternion(UP, toe_out) @ Quaternion(X, pitch) @ self.rest[foot.name]
        current = self.parent_delta(foot) @ self.rest[foot.name]
        self.place(foot, delta=desired @ current.inverted())
        self.place(bones["ball"+side], delta=Quaternion(Quaternion(UP, toe_out) @ X, -toe_roll))

    def hand(self, side, sign, forearm_angle, swing=None, wrist_roll=0, curl_scale=1):
        bones = self.rig.data.bones
        wrist = bones["hand"+side]
        middle = bones["middle_01"+side].head_local
        across = bones["index_01"+side].head_local - bones["pinky_01"+side].head_local
        def basis(along, across):
            along = along.normalized()
            normal = along.cross(across).normalized()
            return Matrix((along, normal.cross(along), normal)).transposed().to_quaternion()
        fingers = Vector((0,-sin(forearm_angle),-cos(forearm_angle)))
        thumb = Vector((0,-cos(forearm_angle),sin(forearm_angle)))
        thumb = Quaternion(fingers, wrist_roll) @ thumb
        orientation = basis(fingers,thumb) @ basis(middle-wrist.head_local,across).inverted()
        self.place(wrist,delta=orientation @ self.parent_delta(wrist).inverted())
        if swing is not None:
            # Set the finger directions in the palm frame. The knuckles in this rest
            # mesh fan apart, so simply adding the same curl to each joint leaves gaps.
            inward = sign * fingers.cross(thumb)
            for digit, angles, spread in (
                ("index", (8, 22, 32), .018),
                ("middle", (12, 28, 39), .001),
                ("ring", (16, 36, 47), -.010),
                ("pinky", (21, 43, 56), -.045),
            ):
                for joint, angle in enumerate(angles, start=1):
                    bend = radians(angle * curl_scale + joint * swing)
                    direction = fingers*cos(bend) + inward*sin(bend) + thumb*spread
                    self.place(bones[f"{digit}_0{joint}{side}"], direction=direction)
            # Leave the thumb open from its base, with its tip resting forward.
            for joint, (along, toward_index, pad) in enumerate(
                ((.77, .59, .20), (.97, .23, .10), (.99, -.06, .16)), start=1
            ):
                self.place(bones[f"thumb_0{joint}{side}"],
                           direction=fingers*along + thumb*toward_index + inward*pad)
            return
        for digit in ("index","middle","ring","pinky"):
            for joint,flex in enumerate((.75,1,.50),start=1):
                self.place(bones[f"{digit}_0{joint}{side}"],delta=Quaternion(thumb,-sign*radians(10)*flex))


def smooth(t):
    return t*t*(3-2*t)


def foot_target(phase, speed, period, stance, gait):
    stride = speed * period
    landing, toe_angle, heel_angle, clearance = {
        "Walk": (.36, 37, -13, .095), "Jog": (.30, 41, -8, .32), "Sprint": (.38, 47, -2, .49)
    }[gait]
    toe_pitch = radians(toe_angle)
    heel_pitch = radians(heel_angle)
    if phase < stance:
        u = phase / stance
        forward = landing - stride * phase
        pitch = heel_pitch * (1-smooth(min(1, 4*u))) + toe_pitch * smooth(max(0, (u-.45)/.55))
        lift = 0
        toe_roll = max(pitch,0)
    else:
        u = (phase-stance)/(1-stance)
        # Match ground speed at lift-off and touchdown. Recover the heel early in swing.
        carry = u*(1-u)**6 - (1-u)*u**6
        forward = landing - stride*stance + stride*stance*smooth(u) - stride*(1-stance)*carry
        pitch = toe_pitch * (1-smooth(min(1,u/.77))) + heel_pitch * smooth(max(0,(u-.65)/.35))
        lift = clearance * (sin(pi*u)**2 * (1.55-1.1*u) + 1.4*u*(1-u)**4)
        toe_roll = max(pitch,0)*(1-smooth(min(1,u/.25)))
    # As the heel rises, roll the sole about the ball of the foot.
    roll = max(pitch, 0)
    return forward + .125*(1-cos(roll)) + .063*sin(roll), .075 + .125*sin(roll) + .063*(cos(roll)-1) + lift, pitch, toe_roll


def frame_pose(rig, phase, speed, period, gait):
    bones = rig.data.bones
    idle = speed == 0
    walk = gait == "Walk"
    sprint = gait == "Sprint"
    stride = cos(TAU*(phase-.04))
    breath = sin(TAU*phase)
    stance = .59 if walk else .30 if sprint else .38
    hip_yaw = radians(.6*breath if idle else -(6 if sprint else 4)*stride)
    lean = radians({"Relaxed Idle": 0, "Walk": 2, "Jog": 7, "Sprint": 12}[gait])
    p = Pose(rig)
    p.place(bones["Root"])
    targets = {}
    for side, sign in (("_l",1),("_r",-1)):
        local_phase = (phase + (0 if sign == 1 else .5)) % 1
        if idle:
            targets[side] = Vector((sign*.114, -.015-sign*.018, .073)), 0, 0
        else:
            fwd,z,pitch,toe_roll = foot_target(local_phase,speed,period,stance,gait)
            targets[side] = Vector((sign*(.089 if sprint else .093),-fwd,z)), pitch, toe_roll
    pelvis = bones["pelvis"]
    if idle:
        z = .918 + .0015*breath
    elif walk:
        z = .867 - .026*cos(2*TAU*phase)
    else:
        height, bounce = (.829, .033) if sprint else (.850, .025)
        z = height - bounce*cos(2*TAU*(phase-.13))
    # The supporting leg limits hip height. The recovering foot follows the pelvis.
    for side,(ankle,_,_) in targets.items():
        if not idle and (phase + (0 if side == "_l" else .5)) % 1 > stance:
            continue
        hip = pelvis.head_local + Quaternion(UP,hip_yaw) @ (bones["thigh"+side].head_local-pelvis.head_local)
        planar = (hip-ankle).xy.length
        reach = bones["thigh"+side].length + bones["calf"+side].length - .003
        z = min(z,ankle.z+sqrt(max(0,reach**2-planar**2))-(hip.z-pelvis.head_local.z))
    offset = Vector((.003*breath if idle else .014*sin(TAU*(phase+.03)),0,z-pelvis.head_local.z))
    pelvis_rotation = Quaternion(UP, hip_yaw)
    p.place(pelvis,delta=pelvis_rotation,offset=offset)
    for side,sign in (("_l",1),("_r",-1)):
        hip = p.position["pelvis"] + pelvis_rotation @ (bones["thigh"+side].head_local-pelvis.head_local)
        ankle,pitch,toe_roll=targets[side]
        p.leg(side,hip,ankle,pitch,sign*radians(5 if idle else 2),toe_roll)
    running = gait in ("Jog", "Sprint")
    settle = radians(1.2) * cos(2*TAU*(phase-.16)) if running else 0
    roll = radians(1.6) * sin(TAU*(phase+.03)) if running else 0
    side_axis = Vector((0, 1, 0))
    for name, angle in (("spine_01",.45),("spine_02",.35),("spine_03",.20)):
        pitch = lean + settle + (radians(.35)*breath if idle else 0)
        p.place(bones[name], delta=Quaternion(side_axis, roll*angle) @ Quaternion(X, pitch*angle)
                @ Quaternion(UP, -hip_yaw*1.8*angle))
    p.place(bones["neck_01"],delta=Quaternion(side_axis,-roll*.65) @ Quaternion(X,-(lean+settle)*.55))
    p.place(bones["head"],delta=Quaternion(X,-lean*.25))
    for side,sign in (("_l",1),("_r",-1)):
        if running:
            # The shoulder leads the change of direction; the elbow and wrist trail it.
            arm_phase = TAU*(phase - .040) + (pi if sign == 1 else 0)
            swing = cos(arm_phase)
            shoulder = cos(arm_phase + .19)
            protract = Quaternion(UP, -sign*radians(4.2 if sprint else 3.2)*shoulder)
            shrug = Quaternion(side_axis, -sign*radians(2)*shoulder)
            p.place(bones["clavicle"+side], delta=protract @ shrug)
            upper = radians(-8 + (39 if sprint else 27)*swing)
            elbow = radians((89 if sprint else 77) + 13*cos(arm_phase - .26))
            lower = upper + elbow
            p.place(bones["upperarm"+side],
                    direction=Vector((sign*(.13 - .05*swing), -sin(upper), -cos(upper))))
            p.place(bones["lowerarm"+side],
                    direction=Vector((-sign*(.065 + .11*swing), -sin(lower), -cos(lower))))
            wrist = lower + radians(4.5)*sin(arm_phase)
            wrist_roll = sign*radians(7 + 4*cos(arm_phase - .22))
            p.hand(side, sign, wrist, swing=cos(arm_phase-.30), wrist_roll=wrist_roll,
                   curl_scale=.72 if sprint else 1)
            continue
        p.place(bones["clavicle"+side])
        upper = radians(-3 + sign*breath*.6 if idle else -2-sign*17*stride)
        lower = upper + radians(12 if idle else 21)
        p.place(bones["upperarm"+side],direction=Vector((sign*(.18 if idle else .13),-sin(upper),-cos(upper))))
        p.place(bones["lowerarm"+side],direction=Vector((sign*.035,-sin(lower),-cos(lower))))
        p.hand(side,sign,lower)
    return p


def generate():
    bpy.ops.wm.open_mainfile(filepath=str(HERE/"Anatomical Base.blend"))
    scene = bpy.context.scene
    scene.render.fps = 60
    rig = bpy.data.objects["Anatomical Base"].parent
    rig.animation_data_clear()
    rig.animation_data_create()
    for bone in rig.pose.bones:
        bone.rotation_mode = 'QUATERNION'
        bone.matrix_basis = Matrix.Identity(4)
    for name,frames,speed in (("Relaxed Idle",168,0),("Walk",60,1.5),("Jog",44,3.2),("Sprint",36,5.5)):
        action = bpy.data.actions.new(name)
        rig.animation_data.action = action
        for f in range(frames+1):
            pose = frame_pose(rig,f/frames,speed,frames/60,name)
            for key in pose.rotation:
                bone = rig.pose.bones[key]
                bone.keyframe_insert(data_path="rotation_quaternion",frame=f+1,group=key)
            rig.pose.bones["pelvis"].keyframe_insert(data_path="location",frame=f+1,group="pelvis")
        track = rig.animation_data.nla_tracks.new()
        track.name = name
        strip = track.strips.new(name,1,action)
        strip.extrapolation = 'NOTHING'
        track.mute = True
        rig.animation_data.action = None
    # The FBX bind skeleton is the anatomical rest pose, independent of the last take.
    for bone in rig.pose.bones:
        bone.rotation_quaternion = Quaternion()
        bone.location = (0,0,0)
    bpy.context.view_layer.update()
    bpy.ops.object.select_all(action='DESELECT')
    rig.hide_set(False)
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    for track in rig.animation_data.nla_tracks:
        track.mute = False
    scene.frame_set(0)
    bpy.ops.export_scene.fbx(filepath=str(OUTPUT),use_selection=True,object_types={'ARMATURE'},
        add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=True,
        bake_anim_simplify_factor=0,axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS')


if __name__ == "__main__":
    generate()
