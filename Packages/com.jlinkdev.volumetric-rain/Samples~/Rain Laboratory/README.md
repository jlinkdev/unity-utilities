# Rain Laboratory

Open the included **Rain Laboratory.unity** scene, or generate a fresh copy with
**Tools > jlinkdev > Volumetric Rain > Create Rain Laboratory** after installing the package. The editor generator creates an independent scene, rain profile, URP
renderer and pipeline, and greybox occluders under a unique `Assets/VolumetricRain`
folder. It is included with the package and requires no other utility or input system.

Open the scene by itself and press **Play**. Rain Camera has a **Rain Demo Pipeline**
component that temporarily activates the supplied pipeline and restores previous
Graphics/Quality settings when the demo stops. It affects all loaded scenes while
running. For edit-mode preview, assign Rain Pipeline manually or add the rain
feature to your existing renderer. Select
Rain Camera to freeze time, change the debug view, or adjust intensity. Select its
profile to change field appearance and budgets. The renderer's Scene View Profile
lets you inspect parallax with normal Scene view navigation.

See the package evaluation guide for the visual checks and GPU benchmark matrix.
