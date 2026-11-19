"""Run in background Blender with MPFB installed. See README.md."""
from importlib import import_module
from pathlib import Path
import sys

import bpy

HERE = Path(__file__).resolve().parent
sys.dont_write_bytecode = True
sys.path.insert(0, str(HERE))
from mesh_io import activate, export, normalize_skin, save
from generate_motion import bake_motion


def generate():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    repo = next((repo for repo in bpy.context.preferences.extensions.repos
                 if (Path(repo.directory) / "mpfb" / "blender_manifest.toml").is_file()), None)
    if repo is None:
        raise RuntimeError("Install the MPFB Blender extension before generating the source.")
    module = f"bl_ext.{repo.module}.mpfb"
    bpy.ops.preferences.addon_enable(module=module)
    services = import_module(module + ".services")
    macro = services.TargetService.get_default_macro_info_dict()
    macro.update(gender=1.0, age=0.5, muscle=0.42, weight=0.48)
    body = services.HumanService.create_human(macro_detail_dict=macro)
    rig = services.HumanService.add_builtin_rig(body, "game_engine")
    body.name, rig.name = "Anatomical Base", "GameEngine"
    body.data.materials.clear()
    body.data.materials.append(bpy.data.materials.new("Studio Neutral"))
    save(HERE / "MPFB Source.blend")

    # Evaluate the helper mask and modeling targets in bind pose. Retain the
    # original deform-group indices on the object when replacing its mesh.
    arm = next(m for m in body.modifiers if m.type == 'ARMATURE')
    arm.show_viewport = False
    bpy.context.view_layer.update()
    deps = bpy.context.evaluated_depsgraph_get()
    visible = bpy.data.meshes.new_from_object(body.evaluated_get(deps), preserve_all_data_layers=True, depsgraph=deps)
    body.data = visible
    for modifier in list(body.modifiers):
        if modifier != arm:
            body.modifiers.remove(modifier)
    arm.show_viewport = True
    normalize_skin(body, rig)
    activate(body)
    save(HERE / "Anatomical Base.blend")
    export(body, rig)
    bake_motion(body, rig)


if __name__ == "__main__":
    generate()
