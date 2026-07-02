# Architecture

This document describes the intended architecture for 終わりなき扉. It is a planning document only; no gameplay scripts have been created in this milestone.

## Scene Model

### Persistent DesktopHost Scene

`DesktopHost` is the persistent entry scene. It owns the simulated desktop shell and survives level transitions. It should contain only durable host responsibilities:

- Desktop canvas and desktop-space presentation roots.
- Desktop camera for 3D objects that exist outside the game window.
- RenderTexture and window registration points.
- Additive level loading coordination.
- Shared input routing between desktop controls and player controls.
- Reset orchestration for the loop ending.

Level-specific puzzle, story, and player state should stay out of `DesktopHost`.

### Additive Level Scenes

Each level scene should be loaded additively under the persistent host. A level scene owns its own gameplay content, spawn points, puzzle objects, story sequence entry data, and gameplay camera. Level unload should remove only level-local objects while keeping the desktop host, desktop windows, and global UI alive.

## Cameras And RenderTexture Flow

### Desktop Camera

The desktop camera renders the fictional computer desktop space. This camera can show 3D desktop objects, overlays, and other presentation elements that exist outside the side-scrolling level.

### Gameplay Camera

Each active level has a gameplay camera aimed at the X/Y gameplay plane. The player and 3D city level are rendered by this camera while movement remains constrained to a 2.5D side-scrolling plane.

### Level Texture In A Window

The gameplay camera renders into a `RenderTexture`. A uGUI `RawImage` displays that texture inside a draggable desktop window. The window is presentation and interaction UI; it must not contain player movement or player state logic.

The basic data flow is:

1. Additive level scene renders 3D gameplay through the gameplay camera.
2. Gameplay camera outputs to a `RenderTexture`.
3. Desktop uGUI window displays the texture in a `RawImage`.
4. Window position, size, and overlap data are converted into puzzle rules.
5. Puzzle effects are applied back to gameplay objects through reversible state changes.

## Player

The player uses a maintainable 2.5D state machine under `Runtime/Player`. Minimum states:

- Idle
- Run
- Jump
- Fall
- Land
- Disabled

The player controller should support left and right movement, jump, ground detection, coyote time, jump buffering, variable jump height, facing direction, input locking, and X/Y plane constraint. Input locks should support multiple owners so dialogue, window interaction, transfer sequences, and ending reset can safely stack control restrictions.

## Desktop And Windowing

Desktop UI belongs under `Runtime/Desktop`, `Runtime/Windowing`, and `Runtime/UI`.

Windowing should own:

- Draggable window view state.
- RectTransform to screen and texture coordinate conversion.
- Window z-order and focus.
- RawImage content assignment.
- Pointer interaction for dragging and resizing if resizing is added later.

The player state machine must not know how windows are dragged.

## Window Overlap Puzzle Rules

Puzzle logic should use pure rectangle and coverage calculations where possible, then apply effects through explicit adapters. Rules should be deterministic and testable:

- Rectangle intersection.
- Coverage ratio.
- Threshold checks.
- Coordinate conversion from desktop window space to gameplay texture space.

Puzzle objects should not be destroyed for reversible effects. Disable and restore renderers, colliders, gravity zones, materials, triggers, or constraints as needed.

## Story And Dialogue

Dialogue and story sequences should be data-driven, preferably with ScriptableObjects under `ScriptableObjects`. Runtime story code belongs under `Runtime/Story`; UI rendering belongs under `Runtime/UI`.

Story data should drive:

- Speaker identity.
- Dialogue lines.
- Branch or condition keys.
- Story flags.
- Control lock requests.
- Sequence actions such as camera focus, window focus, level transfer, or reset.

Story flags must be resettable for the looping ending.

## Player Transfer Between Worlds

The game needs a controlled transfer from the window world to the desktop world. This should be handled by a transfer workflow rather than by ad hoc scene changes:

- Freeze or disable player input through a lock owner.
- Capture the source world, destination world, and spawn anchor.
- Switch active presentation and collision context.
- Move or reinstantiate the player representation according to the current world mode.
- Restore input only after the transfer sequence is complete.

The transfer should be reversible enough to support level reloads and the looping ending.

## Complete Runtime Reset

The loop ending requires a complete runtime reset. Reset should cover:

- Loaded additive level scenes.
- Player state, transform, velocity, input locks, and facing.
- Window position, size, focus, content, and overlap-derived effects.
- Puzzle reversible states.
- Story flags and active dialogue sequences.
- Camera targets, RenderTextures, and temporary presentation state.
- Desktop-world objects that changed during play.

Reset behavior should be explicit and testable. Shared reset coordination can exist in `Runtime/Ending` or `Runtime/Core`, but individual systems should own restoring their own state.

## Assemblies

- `OwariNakiTobira.Runtime` contains runtime code under namespace `OwariNakiTobira`.
- `OwariNakiTobira.Editor` contains editor tools and scene/prefab generators under `Tools/OwariNakiTobira`.
- `OwariNakiTobira.Tests.EditMode` contains EditMode tests for pure logic.
- `OwariNakiTobira.Tests.PlayMode` contains PlayMode tests for runtime behavior that needs Unity play mode.
