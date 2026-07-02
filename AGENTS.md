# Project

This is a Unity 6 URP game named 「終わりなき扉」.

It is a 3D side-scrolling puzzle game set inside a fictional computer
desktop. A girl named Tsukisaki is trapped inside a computer system.
The real-world player can move simulated desktop windows to change
the physical rules of the 3D world.

The final art assets will be provided later. All prototype scenes must
use replaceable cubes, capsules, planes and simple UI graphics.

# Core Technical Direction

- Unity 6.
- Universal Render Pipeline.
- C#.
- Unity Input System.
- uGUI for runtime desktop windows and dialogue UI.
- TextMeshPro for text.
- 3D physics.
- The character uses a 3D model but moves as a 2.5D side-scroller.
- Character movement is constrained to the X/Y gameplay plane.
- A gameplay Camera renders the 3D city into a RenderTexture.
- The RenderTexture is displayed inside a draggable uGUI RawImage window.
- The desktop itself may contain 3D objects rendered by a separate camera.
- Levels should support additive scene loading.
- The persistent DesktopHost scene must survive level transitions.

# Folder Rules

Put all project-owned files under:

Assets/_Project

Use these folders:

Assets/_Project/Runtime
Assets/_Project/Editor
Assets/_Project/Tests
Assets/_Project/Scenes
Assets/_Project/Prefabs
Assets/_Project/ScriptableObjects
Assets/_Project/Materials
Assets/_Project/Documentation
Assets/_Project/Settings

Put runtime code under the namespace:

OwariNakiTobira

Editor code must be inside an Editor folder or Editor assembly.

# Code Rules

- Use private serialized fields instead of unnecessary public fields.
- Keep UI, gameplay, story and presentation logic separated.
- Avoid giant manager classes.
- Avoid static mutable global state.
- Do not use GameObject.Find in gameplay code.
- Do not repeatedly call FindObjectOfType in Update.
- Cache component references.
- Unsubscribe events in OnDisable or OnDestroy.
- Do not allocate new collections every frame.
- Do not destroy puzzle objects when a reversible effect is intended.
- Disable and restore renderers and colliders instead.
- Support multiple control-lock sources.
- Support complete runtime reset for the looping ending.
- Do not introduce third-party packages without approval.
- Do not depend on final art assets.
- Use configurable ScriptableObjects where appropriate.

# Player Rules

Create a maintainable player state machine.

Minimum states:

- Idle
- Run
- Jump
- Fall
- Land
- Disabled

The player must support:

- Left and right movement
- Jump
- Ground detection
- Coyote time
- Jump buffering
- Variable jump height
- Facing direction
- Input locking
- 2.5D movement constraint

Do not mix UI window dragging logic into the player states.

# Scene Rules

Do not edit Unity scene YAML files manually.

When a scene or prefab needs complex automatic setup, create Editor tools under:

Tools/OwariNakiTobira

Editor tools should safely generate prototype scenes and prefabs.

Do not overwrite an existing scene without explicit confirmation.

# Testing Rules

Create EditMode tests for pure logic, including:

- State transitions
- Input lock ownership
- Rectangle intersection
- Coverage ratio
- Story flags
- Runtime reset
- Window coordinate conversion

Do not claim that Unity compilation or tests succeeded unless they were
actually executed.

# Workflow Rules

For every task:

1. Inspect existing files first.
2. Describe the intended changes.
3. Make one focused feature at a time.
4. Avoid unrelated refactors.
5. Report every file created or modified.
6. Report manual Unity Editor steps.
7. Stop when the requested milestone is complete.
8. Do not start the next milestone automatically.