# Beam Kit Demo

## Run the included scene

Use a Unity 6 project with an active URP 17 pipeline. Import **Beam Kit Demo** from
Package Manager's Samples tab, open `Scenes/Beam Kit Demo.unity`, and press Play.
The sample uses your current pipeline and does not replace project settings.

## What to try

- Compare the continuous, curved, electrical, and branching stations.
- Targets move and shader pulses trigger automatically; no input setup is needed.
- The overlay displays the current neutral physics contact count.
- Select a beam to change modifier order, strand detail, or its render profile.
- Reduce the demo controller's movement amplitude to zero to inspect a static path.

Contacts follow the CPU path, not fine shader displacement. A contact count is
reported as data; the sample does not apply damage or other gameplay effects.

If beams are missing, confirm an active URP pipeline and assigned beam materials.
For authoring and performance details, see the installed package's README and
Documentation~ folder.
