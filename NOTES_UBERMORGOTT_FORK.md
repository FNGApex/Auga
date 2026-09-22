# Findings from UberMorgott's Auga fork

> [!WARNING]
> **Credit Morgott if you use their implementation.**
> Everything below comes from <https://github.com/UberMorgott/Valheim-Mod-Auga-Fork> by **Morgott (UberMorgott)**,
> read at commit `30002bd` (2026-09-20). Their `LICENSE` puts Morgott's own contributions under
> **CC BY-NC 4.0** (<https://creativecommons.org/licenses/by-nc/4.0/>). Before copying or adapting their code, art
> or a substantial part of their docs into this repo:
>
> 1. Credit them where the material lands, for example as a header comment:
>    `// Adapted from UberMorgott/Valheim-Mod-Auga-Fork (<file>@<commit>) by Morgott, CC BY-NC 4.0. Changes: <what we changed>.`
> 2. Say that it is adapted and what changed, and name the source commit in our commit message.
> 3. Keep this fork non-commercial. CC BY-NC forbids commercial use.
> 4. Their grant does **not** cover everything in their repo. The following have no licence grant, so don't copy them
>    until the owner is clear:
>    - mrcook1e's April 2026 commits (author "John Doe").
>    - `Auga/APIManagerPatcher.cs` (commit `05f5be6`), which is decompiler output of blaxxun's APIManager.
>    - Inherited Auga, game and third-party material. Upstream Auga has no licence file at all.
>
> Facts about the game or other mods (field names, call sites, behaviour) can be used freely. Credit them anyway: they
> did the digging. Line numbers like `Hud.cs:1116` refer to **their 1.0.12 decompile**. We run 1.0.15, so re-check the
> lines before relying on them.

Their fork changed direction on 2026-09-11. It is now "AugaSkin", which restyles the vanilla UI in place and has no
Auga layouts and no Auga.API. Many of their findings still apply to our approach, which keeps Auga's screens and API.
Tags used below:

- **[us]**: applies to our port and is not handled yet. Their finding, not yet reproduced by us.
- **[done]**: our port already handles it.
- **[info]**: background, or only relevant if we restyle vanilla objects the way they do.

To look at their code again, clone it with `git -c core.longpaths=true clone https://github.com/UberMorgott/Valheim-Mod-Auga-Fork`.
A plain Windows checkout fails on long paths.

## 1. Mod compatibility

This is the most valuable part: they tested with the installed mod builds and decompiled them with ilspycmd.

- **[us] EAQS 3.1.1 slots sit offset from Auga's paper doll.** EAQS's `AugaPanel.UpdatePanel` (`AugaPanel.cs:101-113`)
  calls `API.Panel_Create(m_player, (255,352))`, which centres the pivot (`API.cs:109`, done this way since 2021). EAQS
  then sets only the anchors to (0,1) and `anchoredPosition = (752,-166)` and never touches the pivot. Its slot maths
  assumes a top-left pivot. The cells live in its own `EaqsSlotRoot`, placed at `m_gridRoot`'s reference point
  (`EquipmentPanel.cs:611-631`). Net offset between cells and art: **(+131.5, -180)**. The bug is on EAQS's side.
  Changing `Panel_Create`'s pivot globally would break EpicLoot, which also calls it. Their options were an EAQS-side
  fix, or pinning `EaqsSlotRoot` / special-casing the pivot for EAQS. Neither was tried before they dropped the API.
- **[us] EAQS adds hidden inventory rows.** It sets `m_inventory.m_height = BaseRows + 3` and moves those cells into its
  Auga panel from an `InventoryGrid.UpdateGui` postfix. Our `PlayerInventory_Setup.InventoryGrid_UpdateGui_Patch`
  re-parents **every** PlayerGrid element into `Top` / `Main/Grid`, so it fights EAQS for those cells. Their fix was to
  re-parent only elements that are still under `m_gridRoot`, and to run after EAQS.
- **[us] How consumers bind to Auga.API.**
  - EAQS, EpicLoot and VNEI ship a self-redirecting stub. It looks for assembly `Auga`, type `Auga.API` and GUID
    `randyknapp.mods.auga`, all of which we keep.
  - AAACrafting ships a 72-method internal stub that binds through **blaxxun's APIManager**. Auga must run
    `APIManager.Patcher.Patch()` for it, and our `Auga.cs:180` has that call commented out, so AAACrafting's Auga path
    stays unbound in our port.
  - The APIManager patcher has a bug (commit `76853a8`): `VisitMethod` re-owns `MethodDefinition`s nested in a
    consumer's stub (`<Transpiler>d__2`), and Cecil's `Write` then throws `InvalidCastException` in `AddOverrides`.
    Guard it the way `VisitField` is guarded: skip `MethodDefinition`. AdventureBackpacks ships its own copy of the
    patcher with the same bug.
- **[info] Which API methods consumers actually call**, useful to scope `auga-api.chs`:

  | Consumer | Methods called |
  | --- | --- |
  | EAQS 3.1.1 | `IsLoaded`, `Panel_Create(Transform,Vector2,string,bool)`, `Divider_CreateSmall(Transform,string,float)` |
  | EpicLoot 0.14.2 | none directly |
  | VNEI 0.17.6 | `PlayerPanel_HasTab`, `PlayerPanel_AddTab(string,Sprite,string,Action<int>)`, `PlayerPanel_IsTabActive`, `PlayerPanel_GetTabButton` |
  | AAACrafting 2.1.6 | `IsLoaded`, `Workbench_GetCraftingTabButton`, `Workbench_GetUpgradeTabButton` |

  Stub type drift: the stub's `CustomVariantPanel_Enable` returns `Text` where Auga returns `TMP_Text`, and
  `PlayerPanelTabData.TabTitle` / `WorkbenchTabData.TabTitle` have the same `Text` vs `TMP_Text` split. Nobody calls
  these today, so there is no runtime effect.
- **[us] EpicLoot 0.14.2.** `HasAuga` is never assigned, so EpicLoot always takes its vanilla UI path.
  `MagicSearchField..ctor` reads `InventoryGui.m_crafting.Find("RepairButton/Glow")`, and `MagicPages.Reset` NREs after
  that. Our port keeps vanilla `root/Crafting` as a hidden donor, so the lookup probably succeeds. EpicLoot's UI would
  then land inside the hidden panel. Unverified.
- **[us] VNEI 0.17.6** (`Plugin.cs:335`) and **AdventureBackpacks** (`Patches/GuiBar.cs:22`) detect Auga by GUID.
  Under Auga, AdventureBackpacks skips its 54 px durability-bar width. While they still used the Auga GUID, they saw a
  `MainVneiHandlerAuga` NRE. They did not record the cause, so check it when VNEI is tested with our port.
- **[us] StarLevelSystem and MonsterModifiers break our enemy HUD** (their commits `b3b434e`, `6252c5f`).
  - Our `EnemyHud_Awake_Patch` replaces the whole object (`DirectObjectReplace`). Other mods' `EnemyHud.Awake`
    postfixes then run on a destroyed instance, which NREs at startup for StarLevelSystem.
  - Their fix: keep the vanilla EnemyHud object, its Canvas and its Awake, and swap only `HudRoot` and the template
    fields in the Awake prefix.
  - Stars must keep the vanilla shape: an Image "star" with a child "star (1)". StarLevelSystem clones that in
    `SetupStar`, and MonsterModifiers recolours it.
  - They drive extra star levels right after vanilla's own level_2/level_3 code in `UpdateHuds`. When a level mod has
    removed that code, the extra displays stay hidden instead of doubling up.
  - Vanilla EnemyHud root: ScreenSpaceOverlay Canvas with sort order 200, a ConstantPixelSize CanvasScaler and a
    GuiScaler. `UpdateHuds` positions huds in screen pixels (`EnemyHud.cs:238-241`).
- **[info] EAQS 3.1.1 slot labels are English literals** (`Slots.cs:973-980`, `() => "Head"`). There is no translation
  hook.
- **[info] Third-party breakage on 1.0.x.**
  - AAACrafting 2.1.6: `Undefined target method`, because private `Inventory.AddItem` gained
    `bool skipValidPositionCheck`. This aborts its whole `PatchAll`.
  - Jotunn `GameVersions.GetNetworkVersion` reads a missing `Version::m_networkVersion`.
  - ConfigurationManager `Start` reads a missing `Screen::showCursor`.
  - EpicLoot logs `missing ItemDrop` for FrozenKing_Summon, PropFeastDeepNorth and SnowRoller.

  All of these were seen on 1.0.12 and may be fixed in newer builds.

## 2. Game 1.0 UI facts

- **[us] Double sound on mouse clicks** (commit `ab08cc3`). About 130 bundle buttons have `ButtonSfx.m_selectSfxPrefab`
  set; vanilla sets it on 2 of 59. Mouse-down selects (select sfx) and mouse-up clicks (click sfx) more than 2 frames
  later, past `SfxTimer`, so a single click sounds like two. Their fix: `ButtonSfx.OnSelect` skips `PointerEventData`,
  so keyboard and gamepad keep the select sound. Our port has no such patch.
- **[us] The HP bar stalls under continuous healing** (commit `1ba3882`, `tools/guibar-delay-check.ps1`). `GuiBar.SetValue`
  re-arms `m_delayTimer = m_changeDelay` on every call where the value rises and `m_smoothFill` is set
  (`GuiBar.cs:73-76`). A mod that heals every frame, such as SmoothRegen, keeps re-arming the timer, so the fill never
  moves while the slow bar runs ahead. Vanilla heals once every 10 s, so vanilla never shows it. Their fix: set
  `FastBar.m_changeDelay = 0` in `AugaHealthBar.Start`. Our `AugaHealthBar` has the same code.
- **[info] The HUD rewrites these every frame:**
  - Stamina and eitr roots get `anchoredPosition (0,130)` (`Hud.cs:1116,1185`). In build mode and on a ship the lift
    constants are 320 and 285 (`Hud.cs:1112,1152,1181`).
  - The value texts are written every frame (`Hud.cs:1091,1107,1176`).
  - Stamina and eitr fade out 1 s after full or zero max through their animators (`Hud.cs:1098-1106,1167-1175`).
- **[info] Animators own some vanilla HUD images.** The `health_flash` clip keys `Health/border` `m_Color`/`m_Sprite`
  and `darken` `m_IsActive`. The stamina clips key `Stamina` `m_IsActive` and the `Stamina/darken` colour. A restyle of
  those Images gets overwritten; one came back as a grey box. Disable the Image (`m_Enabled` is never keyed) and add
  your own child instead.
- **[info] Adrenaline.** The vanilla bar is a 4 px strip on the stamina bar's bottom edge. `SetAdrenalineBarSize`
  receives the stamina fill width. Vanilla hides it at 0 and flashes it when full (`Player.cs:4604`).
- **[info] Shield.** Vanilla has no shield bar, only the status-effect icon (`Hud.cs:1657-1692`). The remaining absorb
  is `SE_Shield.m_totalAbsorbDamage - m_damage` (`SE_Shield.cs:23-57`). Their shield outline, drawn behind the HP bar,
  is in `AugaStatBars.cs`; credit applies if we copy it.
- **[info] Dark box behind text.** A TMP material outline (0.2) on the bundle's Norsebold SDF dilates the glyph quads
  into a visible dark box. Check for this on any dark box behind Auga text.
- **[info] Auga input art vs vanilla text.**
  - Auga's `TextInputBG` has a wider 9-slice border than vanilla `text_field`, so typed text runs under the chevron ends.
    Their fix (`c459d16`): grow `TMP_InputField.textViewport` (legacy: text and placeholder) by the border difference,
    `sprite.border / (pixelsPerUnit x pixelsPerUnitMultiplier)`.
  - A focused input falls back to the vanilla selected sprite through the Selectable's SpriteSwap states. Clear
    `spriteState` when skinning (`eb267d6`).
  - Relevant for `SettingsSkin` and `BuildMenuSkin`.
- **[info] Vanilla-owned leftovers.**
  - `BuildUi.m_debugUi` is destroyed by vanilla itself (`BuildUi.cs:140`). Ignore it in `PortDiagnostics` dead-ref
    reports.
  - On the large map, KeyHints overlapping the Quests/Treasure toggles also happens with no mods installed.
- **[info] Canvas sort orders.** The vanilla `Menu` Canvas uses override sorting with order 1700. Its parents
  `IngameGui`, `PixelFix` and `LoadingGUI` have no Canvas. We found the same root cause independently: see
  `PortCarryOver.CopyCanvas` / `WrapInCanvas`.
- **[done] Already handled in our port:**
  - Menu: the 2023 AugaMenu prefab wires Skip Intro to `OnManualSave`.
  - Chest panel is under `root/Player` in 1.0.
  - `m_uiGroups` order.
  - Grid delegates, including `CanDropDragOntoItem` (called unchecked).
  - `InventoryElement` needs `m_dropFocus` as a separate non-raycast Image, reads `m_button.colors` unchecked, and needs
    `UIDragHandler`.
  - `SplitDialog` positions.
  - Minimap `m_onLeftDown`/`m_onLeftUp` and `RemovePinUnderPointer`.
  - `Hud.UpdateBuild` anchor `"{0} [<color=yellow>{1}</color>]"`.
  - `DamageText.AddInworldText(string)`.
- **[info] Changes from 1.0.7 to 1.0.12.**
  - `InventoryGui` shows `$achievements_permanently_cheated_bypass` on `m_achievementsCheatedText`.
  - `AchievementUnlockPopup` got a SFX gate (`TryPlaySfx`).
  - New dev command `yesiuseddevcommandsbutiwantmyachievementsanyway`.

  Most of the rest of that diff is compiler noise.
- **[info] Latent bug in the store transpiler.** `Store_Setup`'s `StoreGui.Awake` transpiler calls the instance method
  `StoreMethods.SetupAugaStoreGui` with `call`, passing the StoreGui as `this`. That only works because `this` is
  unused.

## 3. Testing and tooling ideas (for ClaudeHeim)

- **Skip the intro cinematic.** `CinematicsManager.Stop()` is the Esc path (`CinematicsManager.cs:123-125`). No launch
  argument or pref exists; `m_introOnStartup` is checked at `FejdStartup.cs:477`. This could cut about 2.5 minutes from
  our cold-start scenarios (`mainmenu.chs` waits for `m_mainMenu`).
- **Typed input in automation:** `TMP_InputField.ProcessEvent(Event)` followed by `ForceLabelUpdate()`.
- **Their `auga_audit` dev command** (commit `17925ed`, `Auga/AugaAudit.cs`) checks each open screen for:
  - missing scripts
  - dead vanilla refs
  - **Graphics with no Canvas ancestor**, which would have caught our invisible-screen bug at once
  - sibling rect overlaps, with an allowlist

  Our `PortDiagnostics` only does dead refs. The Canvas-ancestor and overlap checks are worth adding as ClaudeHeim
  assertions. The idea is free to use; credit applies if we copy the code.
- **Scene dumps.** They dumped the 1.0.12 main scene with UnityPy from `valheim_Data/StreamingAssets/SoftRef/Bundles/17245031`
  (= main.unity) and the animator clip bindings from `SoftRef/Bundles/d59cfac`. The bundle ids may differ in 1.0.15.

## 4. Unity and bundle

- **[info]** mrcook1e's `FixMissingScripts` editor tool **deletes** components with missing scripts. Don't adopt it.
  Our MD4 fileID remap (`PORT_1.0.md`) keeps the references.
- **[info]** In Unity 6, `Unity.Auga.dll.meta` needs `platformData` with `Any` enabled, or the editor won't load it.
- **[info]** Their bundle is still mrcook1e's 6000.0.61f1 build and still has `Fishlabs.GuiInputField` references. Ours
  is rebuilt with those remapped.

## 5. Their implementations we might reuse (credit and licence apply)

| What | Where in their repo | Why it might help us |
| --- | --- | --- |
| Sprite-name to Auga-art restyle map | `Auga/AugaStyle.cs` (`a26fdc0`, `6159aef`) | Generalises our `SettingsSkin` / `BuildMenuSkin`: `woodpanel_*`, `panel_interior`, `button`, `button_tab`, `checkbox`, list-selection colour, button-label TMP margins |
| Shield outline on the HP bar | `Auga/AugaStatBars.cs` (`f097390`, `d17ad1c`) | Feature we lack |
| Input text inset for Auga input art | `AugaStyle.InsetInputText` (`c459d16`) | Any vanilla input we skin |
| Enemy HUD template swap inside vanilla | `Auga/EnemyHud_Setup.cs` (`b3b434e`, `6252c5f`) | StarLevelSystem / MonsterModifiers compatibility |
| Select-sfx fix | `Auga/ButtonSfx_Patch.cs` (`ab08cc3`) | Double click sound |
| UI audit command | `Auga/AugaAudit.cs` (`17925ed`) | Test assertions |
| Movable HUD by config offset/scale | `MovableHudElement` + `[HudLayout]` (`0a5248d`) | Offsets from the vanilla anchor, applied once and again on SettingChanged |
