# Portal Playground

Open **Scenes/Portal Playground** and press Play.

- Move with **WASD** and look with the mouse.
- Press **Escape** to release the pointer; click the Game view to capture it again.
- Walk through either portal to verify CharacterController traversal and uniform scaling.
- Watch the moving orange crate for Rigidbody velocity preservation and clipped transition rendering.
- Walk toward the face-to-face **Recursive Display Pair** near the back of the main room to see bounded recursive rendering. Traversal is disabled on that pair so it serves as a stable rendering exhibit.

The sample uses the Input System when it is installed and falls back to Unity's legacy input API otherwise. The portal runtime itself has no input dependency.
