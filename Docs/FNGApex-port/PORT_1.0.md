# Valheim 1.0 port notes (branch `port/valheim-1.0`, public fork FNGApex/Auga)

This repository has no license: nothing here is re-uploaded as a release, and the push URLs of the upstream remotes
are set to a dummy value on purpose. Base: `vapok/wip/1.3.13-WIP`. RandyKnapp's own 1.0 port is on upstream `main`
(since 2026-09-22); `NOTES_UPSTREAM_COMPARE.md` compares the two and lists which of our fixes apply to his code.

## Building the code

Paths live in `Directory.Build.props` (`ValheimDir`). Game assemblies are publicized at build time.

```
dotnet build AugaUnityLib/AugaUnityLib.csproj -c Release
dotnet build Auga/Auga.csproj -c Release                       # embeds the lib, fastJSON and the bundle
dotnet build Auga/Auga.csproj -c Release -p:DeployAuga=true    # also copies into BepInEx/plugins/Auga
```

The plugin project is labelled `net48` with `NoStdLib` and references the game's own
`mscorlib/netstandard/System/System.Core`: Harmony's `List<Label>` does not resolve through the
netstandard2.1 facades (CS7069).

## Screens handled at runtime instead of rebuilt prefabs

- Settings screen: 1.0 split `Settings` into `Valheim.SettingsGui.*` tab components. `Settings_Setup.cs` is excluded
  from the build; `SettingsSkin.cs` re-skins the live vanilla screen instead (all tabs, on-demand sub-panels).
- Item split dialog: 1.0 has a `SplitDialog` component on `InventoryGui.m_splitDialog`; `PortSplitDialog.cs` drives
  Auga's panel with it.
- Remaining vanilla panels (popups, achievements, player list, world modifiers, radial, piece author): `PanelSkins.cs`
  with the art-pass sprites (`NOTES_ART_PASS.md`).
- Mod API: `APIManager.Patcher.Patch()` stays commented out (the dll was never in the repo). Consumers use
  `AugaAPI.dll` (see "Mod API" below). Mods that embed the OLD Auga API shim are not patched (upstream's APIManager
  path does that).

## Unity project (`AugaUnity/`) - prepared for Unity 6000.0.75f1 (the game's version)

Prefabs bind scripts that live in dlls by the dll's `.meta` GUID plus a fileID that is the first four
bytes of MD4("s\0\0\0" + namespace + class). Checked with that formula:

- all 36 game classes used by the prefabs (329 refs into `assembly_valheim` / `assembly_guiutils`) still
  exist in 1.0 under the same names, so keeping the old `.meta` files keeps every reference;
- all 68 Auga classes (339 refs) resolve in the rebuilt `Unity.Auga.dll`;
- `ui_lib.dll` is gone: the 7 `Fishlabs.GuiInputField` refs (fileID -560918846) were rewritten to
  `GUIFramework.GuiInputField` in `gui_framework.dll` (fileID 1526933710, meta GUID
  `a7c1e5d29b3f4c60a1d2f0e9b8c7d6a5`, created here).

`Packages/valheim/`: 13 dlls replaced by their 1.0 versions, 10 that 1.0 no longer ships removed, and
the non-Unity dependencies of the 1.0 assemblies added (`gui_framework`, `SoftReferenceableAssets`,
`Splatform`, `MagicaClothV2`, `Newtonsoft.Json`). `Packages/manifest.json`: TextMeshPro now comes with
`com.unity.ugui` 2.0.0; `com.unity.collections` added (brings Burst and Mathematics, which the game
assemblies reference).

Build the bundle without opening the editor window:

```
Unity.exe -batchmode -quit -projectPath AugaUnity -executeMethod AugaLauncher.BuildAssetBundles -logFile build.log
```

Expected follow-up after the first open: serialized fields whose names or types changed in 1.0 lose
their values (check `Editor.log` for missing scripts / fields), and new 1.0 HUD parts have no Auga
prefab yet: adrenaline bar, mount panel, lava warning, piece author window, achievements panel, radial menus.

## Status

Done headless in Unity 6000.0.75f1: project upgrade + import (no errors), bundle rebuild, plugin rebuild with the new bundle.
Not done yet: the plugin has never been loaded by the game. First run: set `-p:DeployAuga=true`, start the game and read the
`[PortDiagnostics]` warnings in `BepInEx/LogOutput.log` - they list, per screen, the UI references that are destroyed or unassigned
after Auga's setup. `[Debug] PortDiagnostics = false` turns that off.

## New 1.0 UI (runtime skins, no prefab rebuild)

- `Auga/SettingsSkin.cs` - re-skins the live 1.0 settings screen (all 7 tabs). `[Settings] AugaSettingsSkin`.
- `Auga/BuildMenuSkin.cs` - re-skins `hudroot/BuildUIV2` plus its pooled piece/tag button prefabs and the selected-piece readout. `[BuildMenu] AugaBuildMenuSkin`.
- Adrenaline bar: `AugaHealthBar.ModeType.Adrenaline` on a second copy of the eitr bar (`Hud_Setup.cs`). `[StatBars] AdrenalineBar*`.
- Replacement screen roots get the vanilla Canvas/CanvasScaler/GuiScaler/GraphicRaycaster from their donor (`PortCarryOver.WrapInCanvas`): in 1.0 every screen root is its own canvas.

## Unattended testing

Auga carries no test code. Tests are ClaudeHeim scenarios (`../ClaudeHeim`, a standalone harness mod that references the game only):
`../ClaudeHeim/Run-ClaudeHeim.ps1 -Scenario auga-tour.chs -Mods Auga -OutDir <dir>` - lock, deploy, launch, collect log + screenshots + result.json, remove Auga, release lock. Character RETEP, world TestWorld only. Auga-specific hooks are reached by reflection from the scenario (`set AugaUnity.AugaHealthBar.DebugAdrenalineOverride 65`, `invoke AugaTabController@RightPanel SelectTab 1`). Unity prefab renders: `AugaUnity/Assets/Editor/AugaPortTools.cs`.

## Mod API (AugaAPI.dll)

`dotnet build AugaAPI/AugaAPI.csproj -c Release` -> `Auga/bin/API/AugaAPI.dll`: API.cs + API.Common.cs compiled with `API` defined (every body a stub) plus `Auga/API.Redirect.cs`, whose static constructor Harmony-redirects each stub to the same method of the loaded Auga. This replaces the 2023 `APIManager.dll` (never in the repo) and the dead `API.External.cs`. Consumers reference the dll and merge it into their own (ILRepack), as before. Test: `../ClaudeHeim/scenarios/auga-api.chs`. Not done: `AugaApiExample` is still the VS2017 project (missing usings, hardcoded paths) and `Auga.sln` still lists the old `API` solution configuration.

Second audit (every vanilla UI vs Auga + API): `AUDIT2_1.0.md`, with per-finding status.