# Portal Playground

Open **Scenes/Portal Playground** and press Play.

- Move with **WASD** and look with the mouse. Press **R** to return to the starting scale and position.
- Press **Escape** to release the pointer; click the Game view to capture it again.
- Use the **Linked Portal Pair** for straightforward 1:1 CharacterController traversal.
- Watch the orange crates for Rigidbody velocity preservation, scale mapping, and clipped transition rendering.
- Walk through the face-to-face **Recursion Window** to see bounded recursive rendering continue through traversal.
- Enter the full-size **Size Lab** portal to arrive on the tabletop at 1:4 scale. Its paired portals face one another, so the view forms a recursive scale tunnel and each repeated forward pass shrinks by another factor of four. Reverse direction through the small portal to grow.
- Portal fronts show the linked view and accept traversal. Portal backs use a dark inactive panel and do not accept traversal.

The sample uses the Input System when it is installed and falls back to Unity's legacy input API otherwise. The portal runtime itself has no input dependency.
