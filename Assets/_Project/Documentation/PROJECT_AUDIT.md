# Project Audit

Audit date: 2026-07-02

Project: Owari Naki Tobira / 終わりなき扉

## Unity Version

- Editor version: 6000.0.72f1
- Editor revision: b731fd3ae857
- Source: `ProjectSettings/ProjectVersion.txt`

## Render Pipeline

- Universal Render Pipeline is installed as `com.unity.render-pipelines.universal` version `17.0.4`.
- `ProjectSettings/GraphicsSettings.asset` assigns the custom render pipeline asset with GUID `4b83569d67af61e458304325a23e5dfd`, which maps to `Assets/Settings/PC_RPAsset.asset`.
- `ProjectSettings/GraphicsSettings.asset` assigns URP global settings with GUID `18dc0cd2c080841dea60987a38ce93fa`, which maps to `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`.
- `ProjectSettings/QualitySettings.asset` defines two quality levels:
  - `Mobile`, using `Assets/Settings/Mobile_RPAsset.asset`.
  - `PC`, using `Assets/Settings/PC_RPAsset.asset`.
- Current quality index is `1`, which is `PC`.
- Standalone defaults to `PC`; Android and iPhone default to `Mobile`.
- The PC URP asset uses `Assets/Settings/PC_Renderer.asset`, requires depth and opaque textures, supports HDR, and has SRP Batcher enabled.
- The Mobile URP asset uses `Assets/Settings/Mobile_Renderer.asset`, has render scale `0.8`, and does not require depth or opaque textures.

## Packages

- Input System is available: `com.unity.inputsystem` version `1.19.0`.
- uGUI is available: `com.unity.ugui` version `2.0.0`.
- TextMeshPro is available through `com.unity.ugui` version `2.0.0` as assembly `Unity.TextMeshPro`.
- Test Framework is available: `com.unity.test-framework` version `1.6.0`.
- URP package cache and lock file are present, so the listed packages appear resolved locally.
- No packages were installed or modified during this audit.

## Input And UI

- `ProjectSettings/ProjectSettings.asset` has `activeInputHandler: 1`, meaning the project is configured for the new Input System.
- `ProjectSettings/EditorBuildSettings.asset` points `com.unity.input.settings.actions` at `Assets/InputSystem_Actions.inputactions`.
- The input action asset currently contains Unity template maps for `Player` and `UI`.
- No project-owned TextMeshPro Essentials assets are present yet under `Assets`.

## Existing Scenes And Settings

- Existing scene: `Assets/Scenes/SampleScene.unity`.
- Build settings include `Assets/Scenes/SampleScene.unity` and it is enabled.
- The sample scene contains a `Main Camera`, `Directional Light`, and `Global Volume`.
- Project name in settings: `Endless-Door`.
- Company name in settings: `DefaultCompany`.
- Default standalone screen size: `1024x768`.
- Asset serialization mode is force text.
- Project generation root namespace is currently blank.
- Package Manager has only the default Unity registry configured, with preview packages disabled.

## Existing Code

- Existing project scripts before this scaffold:
  - `Assets/TutorialInfo/Scripts/Readme.cs`
  - `Assets/TutorialInfo/Scripts/Editor/ReadmeEditor.cs`
- These are Unity template readme scripts and are not under `Assets/_Project`.
- No project-owned gameplay scripts existed before this scaffold.

## Visible Compile Or Package Risks

- No compile errors are visible from the inspected project files.
- Unity compilation and tests were not executed, so this audit does not claim a successful Unity import or test run.
- Potential risks:
  - `AGENTS.md` appears to contain mojibake for the Japanese title, suggesting an encoding mismatch in that file.
  - Template readme assets still live under `Assets/TutorialInfo` and may auto-open a layout in the Unity Editor.
  - The existing sample scene is outside `Assets/_Project/Scenes`.
  - TextMeshPro code is available, but TMP Essentials/resources have not been imported into project assets.
  - The default template input actions are not yet tailored for the 2.5D player or desktop window controls.
  - The Unity project root namespace is blank; runtime code should still use `OwariNakiTobira`.
