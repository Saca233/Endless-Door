# Development Roadmap

This roadmap is intentionally milestone-based. Stop after each milestone, verify, and review before starting the next one.

## Milestone 0 - Audit And Scaffold

Status: complete for this task.

- Inspect Unity version, packages, scenes, render pipeline, and settings.
- Create `Assets/_Project` folder structure.
- Create runtime, editor, EditMode test, and PlayMode test assembly definitions.
- Document architecture and development order.
- Do not create gameplay scripts.

## Milestone 1 - Project Hygiene

- Decide whether to keep or remove Unity template readme assets.
- Import TextMeshPro Essentials through the Unity Editor if dialogue UI will use TMP immediately.
- Create the persistent `DesktopHost` scene through the Unity Editor or an approved editor generation tool.
- Move future prototype scenes under `Assets/_Project/Scenes`.

## Milestone 2 - Pure Logic Foundations

- Add tested logic for input lock ownership.
- Add rectangle intersection and coverage ratio logic.
- Add window coordinate conversion logic.
- Add story flag containers.
- Add reset contracts or reset state snapshots.

## Milestone 3 - Player State Machine

- Implement the 2.5D player state machine.
- Cover Idle, Run, Jump, Fall, Land, and Disabled transitions with EditMode tests where possible.
- Add coyote time, jump buffering, variable jump height, facing direction, and X/Y plane constraint.

## Milestone 4 - Desktop Window Prototype

- Create draggable uGUI window views.
- Display placeholder content in a RawImage.
- Separate window input from player input.
- Add tests for screen, window, and texture coordinate conversion.

## Milestone 5 - RenderTexture Level Prototype

- Create a prototype additive level scene with replaceable primitives.
- Render the gameplay camera into a RenderTexture.
- Display that RenderTexture inside the desktop window.
- Verify camera framing and window content in the Unity Editor.

## Milestone 6 - Window Overlap Puzzle Prototype

- Connect window overlap calculations to reversible puzzle effects.
- Use colliders and renderers that can be disabled and restored.
- Avoid destruction for reversible puzzle state.

## Milestone 7 - Story And Dialogue Prototype

- Create ScriptableObject-based dialogue and story sequence data.
- Render dialogue through uGUI and TextMeshPro.
- Add story flag tests and reset tests.

## Milestone 8 - Player Transfer Prototype

- Prototype transfer from the RenderTexture window world to desktop world.
- Define spawn anchors and input lock behavior during transfer.
- Verify the player can return or reset cleanly.

## Milestone 9 - Loop Ending Reset

- Implement full runtime reset across player, story, windows, puzzles, cameras, and loaded scenes.
- Add EditMode tests for resettable pure state and PlayMode tests for scene/runtime reset paths.

## Next Recommended Milestone

Milestone 1: project hygiene and persistent `DesktopHost` scene planning/generation. Do not begin it until explicitly requested.
