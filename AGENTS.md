# AGENTS.md

## Project Overview
- **Project**: Unity game (Spirimonz / Spektrum Hunter).
- **Unity Version**: `2022.3.62f2` (LTS).
- **Primary folders**:
  - `Assets/Scripts` for runtime code.
  - `Assets/Editor` for editor-only tooling.
  - `Assets/Scenes` for playable scenes (`World01`, `House00`, `House01`, etc.).

## Development Notes
- **Input System**: Custom `InputManager.cs` (not Unity Input System). If you change input bindings, ensure the changes are saved/loaded via `SaveManager`.
- **Mobile controls**: Toggled via `GameManager` and `MobileInput`. Mobile UI lives in `Assets/Scripts/UI/`.
- **UI**: `UIGame`, `UIManager`, `UISettingsMenu` are fully script-driven. Avoid adding hard references unless necessary.
- **Tweening**: Project uses **DOTween** (`DG.Tweening`). Use DOTween for animations where consistent with existing patterns.
- **Interactables**: Catchables inherit from `CatchableObject`. Books use `CatchableBook`.

## Code Style & Conventions
- Prefer minimal, readable changes.
- Keep public fields for inspector setup; avoid hard-coded object paths.
- Use `sharedMaterials` for prefab-safe reads and `MaterialPropertyBlock` for per-instance offsets.
- Avoid destructive commands (`git reset --hard`, etc.) unless explicitly requested.

## Testing & Validation
- No automated test suite configured.
- Validate changes in the Unity Editor:
  - Enter Play Mode.
  - Test in both `World` (TPS) and `House` (FPS) scenes as relevant.

## Build Notes (iOS)
- iOS builds require Xcode installed and licenses accepted.
- Ensure `xcode-select -p` resolves and `xcrun --sdk iphoneos --show-sdk-path` works.

## Safe Defaults
When unsure:
- Keep current input behavior intact.
- Avoid altering lighting/quality defaults unless requested.
- Prefer additive changes over refactors.
