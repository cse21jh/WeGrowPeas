# WeGrowPeas build compatibility patch

This embedded package is based on Unity Pipeline `0.5.0-exp.1` (revision
`17df0ac8d6533a8c22830c89dc85a24c01f30ade`). It keeps the Editor connection available
while fixing build preprocessing for this project's independently loaded 2D scenes.

The upstream `PipelineRuntimeBuildProcessor` opens every enabled build scene
additively to look for `RuntimePipelineManager`. Loading these scenes together
registers overlapping URP 2D Global Lights and aborts the build during preprocessing.

The local change in `Editor/BuildProcessors/PipelineRuntimeBuildProcessor.cs`:

- Inspects already loaded scenes directly, preserving unsaved state.
- Checks recursive scene dependencies, including nested prefabs and manager
  subclasses, before opening a closed scene.
- Skips closed scenes without a potential runtime manager. An unresolved script
  type conservatively retains the existing scene scan.
- Preserves bundled DLL integrity checks and runtime configuration validation.

This addresses scenes without runtime managers, including all five current
WeGrowPeas build scenes. Scenes that actually contain managers still use the
upstream additive scan; overlapping lights in those scenes require further isolation.

Keep the embedded package under version control so clearing `Library` does not
discard the fix. Review this patch when adopting a later upstream version.

Verified with Unity 6000.3.6f1 on 2026-09-22:

- `PipelineRuntimeSceneScanTests`: 4 passed (unrelated closed scene, inactive direct
  manager, inactive nested-prefab manager, and unsaved manager in an open scene).
- Windows x64 player build with all five enabled scenes: succeeded in 60.3 seconds,
  with 0 errors and 50 warnings. No duplicate Global Light errors occurred.
- Output: `Builds/Windows/WeGrowPeas.exe`; summarized diagnostics are saved beside
  the executable in `BuildVerification.json`.
