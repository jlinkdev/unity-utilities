"""Bake a standalone skeletal FBX clip for Unity Humanoid retargeting."""
from pathlib import Path
import sys
import bpy
from mathutils import Quaternion, Vector

HERE = Path(__file__).resolve().parent
sys.dont_write_bytecode = True
sys.path.insert(0, str(HERE))
from mesh_io import export


def bake_motion(body, rig):
    scene = bpy.context.scene
    scene.render.fps = 30
    scene.frame_start, scene.frame_end = 1, 193
    rig.animation_data_create()
    rig.animation_data.action = bpy.data.actions.new("Joint Sweep")
    for bone in rig.pose.bones:
        bone.rotation_mode = 'QUATERNION'
        bone.rotation_quaternion = Quaternion()
        bone.keyframe_insert(data_path="rotation_quaternion", frame=0, group=bone.name)
    def swing(name, direction):
        bone = rig.pose.bones[name]
        local = bone.matrix.to_quaternion()
        axis = (bone.tail - bone.head).normalized()
        delta = axis.rotation_difference(Vector(direction).normalized())
        bone.rotation_quaternion = bone.rotation_quaternion @ local.inverted() @ delta @ local
        bpy.context.view_layer.update()

    for frame, phase in ((1,0), (49,1), (97,0), (145,-1), (193,0)):
        scene.frame_set(frame)
        for bone in rig.pose.bones:
            bone.rotation_mode = 'QUATERNION'
            bone.rotation_quaternion = Quaternion()
        for name, angle in (("spine_02", .08), ("spine_03", .10), ("neck_01", -.21), ("head", -.10)):
            rig.pose.bones[name].rotation_quaternion = Quaternion((0,1,0), phase * angle)
        bpy.context.view_layer.update()
        for side, sign in (("l",1), ("r",-1)):
            lifted_arm = phase == sign
            lifted_leg = phase == -sign
            def aim(name, direction):
                swing(name + "_" + side, direction)
            aim("upperarm", (sign*.81,-.38,.32) if lifted_arm else (sign*.20,.02,-1))
            aim("lowerarm", (sign*.25,-.84,.67) if lifted_arm else (sign*.085,-.23,-1))
            aim("hand", (sign*.10,-.91,.40) if lifted_arm else (sign*.06,-.22,-1))
            aim("thigh", (sign*.07,-.80,-.65) if lifted_leg else (sign*.06,0,-1))
            aim("calf", (sign*.04,.46,-.83) if lifted_leg else (sign*.02,.055,-1))
            aim("foot", (sign*.07,-.93,-.18) if lifted_leg else (sign*.045,-.9,-.46))
        for bone in rig.pose.bones:
            bone.keyframe_insert(data_path="rotation_quaternion", frame=frame, group=bone.name)
    scene.frame_set(0)
    export(body, rig, motion=True)


if __name__ == "__main__":
    bpy.ops.wm.open_mainfile(filepath=str(HERE / "Anatomical Base.blend"))
    body = bpy.data.objects["Anatomical Base"]
    bake_motion(body, body.parent)
