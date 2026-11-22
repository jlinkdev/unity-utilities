"""Maintain the camera-pass source shipped with each independent URP utility."""
from pathlib import Path
import re
from string import Template


SCRIPTS = Path(__file__).resolve().parent
PACKAGES = SCRIPTS.parent / "Packages"
CAMERA_PASSES = {
    "blueprint-rendering": ("BlueprintRendererFeature", "BlueprintCameraPass"),
    "interactive-fog": ("FogRendererFeature", "FogCameraPass"),
    "object-highlighting": ("ObjectHighlightRendererFeature", "HighlightCameraPass"),
    "procedural-nebulae": ("NebulaRendererFeature", "NebulaCameraPass"),
    "volumetric-fog-and-rain": ("RainRendererFeature", "RainCameraPass"),
    "water-system": ("WaterRendererFeature", "WaterCameraPass"),
    "world-scanning": ("ScanRendererFeature", "ScanCameraPass"),
}


def camera_pass_sources():
    template = Template((SCRIPTS / "camera_pass.cs.template").read_text(encoding="utf-8"))
    for package, (feature, component) in CAMERA_PASSES.items():
        runtime = PACKAGES / f"com.jlinkdev.{package}" / "Runtime"
        source, = runtime.rglob(feature + ".cs")
        namespace = re.search(r"^namespace ([\w.]+)", source.read_text(encoding="utf-8"), re.M)[1]
        # RainDemoPipeline inherits the camera pass to support previously imported scenes.
        inherited = component == "RainCameraPass"
        yield source.with_name(component + ".cs"), template.substitute(
            namespace=namespace, component=component, feature=feature,
            modifier="" if inherited else "sealed ",
            enable_access="protected virtual" if inherited else "private",
            disable_access="protected" if inherited else "private",
        )


if __name__ == "__main__":
    for path, content in camera_pass_sources():
        if not path.exists() or path.read_text(encoding="utf-8") != content:
            path.write_text(content, encoding="utf-8")
            print(path.relative_to(PACKAGES))
