# Procedural humanoid studies

Open [Humanoid Review](<../../Assets/Humanoid/Scenes/Humanoid Review.unity>) in Unity and press **Play**. The four figures share one inspection material and one retargeted animation. Select a figure to frame it, or choose **All figures** for the lineup. Drag outside the toolbar to orbit, scroll to zoom, scrub the slider to hold a pose, and use **Bind pose** to inspect the exported A-pose. The character prefabs can be used independently in other development scenes. [Prefabs](../../Assets/Humanoid/Prefabs)

![Standing comparison, from left to right anatomical base, reduction, sculpted planes and offset planes](<Standing comparison.png>)

## Playable character

Open [Character Playground](<../../Assets/Humanoid/Scenes/Character Playground.unity>), press **Play**, and focus the Game view. WASD walks and steers in world space. Hold Shift to jog or Ctrl to sprint. E interacts and Space plays a sword swipe. Both actions pause travel, finish once, and return to locomotion. The sword swipe animates the character's hand and arm.

Drop the [Humanoid prefab](<../../Assets/Humanoid/Prefabs/Humanoid.prefab>) onto a floor with a collider to use it elsewhere. It uses the [Humanoid model](<../../Assets/Humanoid/Models/Humanoid/Humanoid.fbx>), shown as Offset Planes in the mesh study. For scripted control, disable `HumanoidKeyboardInput` and call `SetMovement(worldDirection, gait)` on `HumanoidCharacterController`. Movement persists until changed. Pass `Vector3.zero` to stop. Call `Interact()` or `SwordSwipe()` for a one-shot action.

The [Animator Controller](<../../Assets/Humanoid/Animations/Humanoid Locomotion.controller>) blends the [authored idle, walk, jog, and sprint](<../../Assets/Humanoid/Animations/Character Motion.fbx>) using horizontal speed in meters per second. The feet move backward during contact at the controller's walk, jog, and sprint speeds of 1.5, 3.2, and 5.5 m/s. All three moving cycles begin on left-foot contact so the footsteps stay in phase when blending. The capsule handles travel, gravity, and collision. Interact and Sword Swipe use Quaternius's original clips. [Included CC0 license](<../../Assets/Humanoid/Animations/License.txt>)

To rebuild the four locomotion takes from the anatomical rig, run `blender --background --factory-startup --python-exit-code 1 --python ArtSource/Humanoid/generate_locomotion.py` from the repository root. Keep the FBX's Unity metadata with it. It stores the character's avatar reference pose and loop settings. [Locomotion comparison](<Locomotion comparison.gif>)

## Mesh treatments

| Figure | Unity triangles | Treatment |
| --- | ---: | --- |
| Anatomical Base | 26,756 | MPFB adult male with its GameEngine skeleton and original body surface. The Blender source has 13,378 quads. |
| Reduction Study | 3,000 | Local anatomical relaxation and curvature-sensitive edge collapse. The chest and belly follow the original surface most closely. |
| Sculpted Planes | 2,400 | Eight-sided outlines through the hips, ribs and sternum define a broad front plane. Adjacent triangles are then clustered and their shared vertices fitted to the cluster planes. |
| Offset Planes | 2,150 | Six-sided, twisting trunk sections follow the rib-to-waist taper. Broad hip facets meet near the midline so each thigh can lift independently. |

All three variants replace the face with a continuous jaw-to-crown surface, simplify the forefoot around the existing toe joint, and retain articulated fingers. Original deform groups survive reduction. The new head and forefoot vertices receive explicit neck/head and foot/toe weights. The exporter limits each vertex to four normalized influences. Flat normals make the modeled planes legible under a common neutral light. The material is a review choice.

**Sculpted Planes** is the closest match to the reference across the chest and waist. The jaw-to-neck transition is still abrupt, and narrow facets bunch at the bent knees. [Motion comparison](<Motion comparison.png>) · [Bind comparison](<Bind comparison.png>)

Offset Planes turns diagonals that bridge the thighs toward the midline and blends thigh rotation into the pelvis over a 0.265 m band. At peak knee lift the existing planes form a short angular fold. Median bind-pose triangle area across the front hip is 31.5 cm², compared with 29.6 cm² in the original mesh and 5.6 cm² in the subdivided study. These measurements use face centers 0.79–0.96 m above the floor, within 0.18 m of the midline and forward of Blender Y = −0.035 m. Sampling the Unity sweep at 30 fps found no self-intersections within the hip bend over 193 poses. The lowered hand briefly clips the outer thigh during the sweep. [Three treatments at the same knee lift](<Offset hip comparison.png>)

The playable model grades upper-thigh weights from the inner groin to the outer hip. Below the saddle, each inner thigh follows its own leg and the pelvis. Across 49 sampled poses per gait, the maximum upper-thigh and hip edge stretch fell from 1.98× to 1.48× bind length in the jog and from 2.17× to 1.64× in the sprint. A separate 193-pose check per clip found no intersecting nonadjacent faces in that region during the jog, sprint, or joint sweep.

Offset Planes now fits the outer corners of its rotating sections to the target trunk width. At 1.075 m above the floor, its bind-pose waist measures 26.1 cm across and 19.0 cm deep. The previous section measured 32.1 cm across and 18.6 cm deep. [Earlier and revised silhouettes](<Offset waist comparison.png>)

## Regenerate

Tested with Blender 5.2.2, MPFB 2.0.18 and Unity 6000.3.21f1 using the host project's URP configuration. The editable generator asset retains the MPFB modeling targets, helper mask and rig. The baked anatomical file supplies a clean bind-pose input to the variant script. The study file contains all four skinned figures arranged side by side. [MPFB source](<MPFB Source.blend>) · [Baked base](<Anatomical Base.blend>) · [Editable studies](<Humanoid Studies.blend>)

1. To iterate on the existing base, edit [treatments.json](treatments.json) and run the command below from the repository root. Blender's bundled Python provides NumPy. The checked-in baked base supplies everything needed for this step.

   ```sh
   blender --background --factory-startup --python-exit-code 1 --python ArtSource/Humanoid/generate_variants.py
   ```

2. To regenerate the original body or motion, install the MPFB extension in a Blender extension repository and run the following command **before** the variant command. It writes both source files, the anatomical FBX and a separate skeleton-only motion FBX. To change only the joint sweep, run `generate_motion.py` against the baked base. The profile and cut positions in the variant generator are calibrated in meters to this anatomical base. Recalibrate them if changing its proportions. [MPFB release](https://github.com/makehumancommunity/mpfb2/releases/tag/v2.0.18) · [Upstream scripting examples](https://github.com/makehumancommunity/mpfb2/blob/master/script_samples/index.md)

   ```sh
   blender --background --factory-startup --python-exit-code 1 --python ArtSource/Humanoid/generate_base.py
   ```

3. Refresh Unity. The model importer configures the four Avatars and the motion clip. **Tools → Unity Utilities → Humanoid → Validate and capture** measures the saved scene and updates the images and report here. **Rebuild review assets** recreates the prefab variants and scene after a layout or builder change. Close the review scene before rebuilding it.

| Parameter | Effect |
| --- | --- |
| `triangles` | Target triangle budget for edge collapse. The mask protects the head and gives bends and fingers additional detail. |
| `head_sides` | Number of vertices around each head ring. The jaw profile and oblique cheek line are defined in the generator. |
| `smooth_passes` | Number of local smoothing passes over the trunk and feet. |
| `torso_planes` | Number of sides in the trunk cross section. Zero preserves the relaxed anatomical cross section. |
| `waist_inset_meters` | Additional inset on each side of the waist before reduction. It tapers to zero at the pelvis and chest. Offset Planes uses 0.009 m. |
| `hip_blend_meters` | Vertical distance over which skin weights transition between the thigh and pelvis in the hip treatment. Offset Planes uses 0.265 m. Zero skips the hip treatment. |
| `patch_angle` | Maximum angle in degrees between adjacent clusters at each merge. Zero skips fitting. |
| `patch_radius` | Half the maximum diagonal of a merged patch's bounding box, in meters. |
| `relief_meters`, `seed` | Maximum signed patch-plane offset and its reproducible random sequence. Relief also twists the trunk sections. Vertex movement is limited by the local edge length. |

## Verification

All four Humanoid Avatars passed with 52 mapped bones and 53 skin bind matrices each. Each prefab has one skinned renderer and the shared studio material. The variant generator checked closed manifold edges and triangle area. Two independent Blender runs produced identical vertex coordinates, polygon indices and skin weights for each treatment.

Unity sampled nine points across the 6.4-second clip. Each figure moved its left hand 0.56 m from bind pose, and its right ankle ranged from 0.07 to 0.28 m above the studio floor. In Play mode, focusing a single figure centered it, the lineup restored all four renderers, and resetting the sampled pose returned the measured hand to its bind position. The report includes the central 98% and full range of posed-to-rest triangle-edge lengths for inspecting localized stretch. [Unity measurements](<Unity validation.txt>)
