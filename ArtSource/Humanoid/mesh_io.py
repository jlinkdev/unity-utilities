from pathlib import Path

import bpy

HERE = Path(__file__).resolve().parent
ASSETS = HERE.parents[1] / "Assets/Humanoid"


def activate(obj):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def normalize_skin(body, rig):
    bones = set(rig.data.bones.keys())
    for group in list(body.vertex_groups):
        if group.name not in bones:
            body.vertex_groups.remove(group)
    for vertex in body.data.vertices:
        influences = sorted(((g.weight, g.group) for g in vertex.groups), reverse=True)
        total = sum(weight for weight, _ in influences[:4])
        if total <= 0:
            raise ValueError(f"{body.name}: no skin weight on vertex {vertex.index}")
        for i, (weight, group) in enumerate(influences):
            if i < 4 and weight > 0:
                body.vertex_groups[group].add([vertex.index], weight / total, 'REPLACE')
            else:
                body.vertex_groups[group].remove([vertex.index])


def save(path):
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=str(path), compress=True)


def export(body, rig, motion=False):
    activate(body)
    rig.select_set(True)
    stem = "Joint Sweep" if motion else body.name
    if stem == "Offset Planes":
        stem = "Humanoid"
    if motion:
        body.select_set(False)
        bpy.context.view_layer.objects.active = rig
    folder = ASSETS / "Animations" if motion else ASSETS / "Models" / stem
    folder.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=str(folder / (stem + ".fbx")), use_selection=True,
                             object_types={'MESH', 'ARMATURE'}, add_leaf_bones=False,
                             bake_anim=motion, bake_anim_use_all_actions=False, bake_anim_use_nla_strips=False,
                             bake_anim_simplify_factor=0, use_armature_deform_only=False,
                             axis_forward='-Z', axis_up='Y', mesh_smooth_type='FACE',
                             apply_scale_options='FBX_SCALE_UNITS')
