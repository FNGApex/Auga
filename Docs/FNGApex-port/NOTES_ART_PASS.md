# Art pass notes (5.18 + 5.19 folded, 2026-09-24) - research summary

## Bundle pipeline
- Bundle content = every asset whose `.meta` has `assetBundleName: augaassets` (100 tagged: root prefabs in Assets/Prefabs, fonts,
  7 sprites). Untagged sprites only ride along as prefab dependencies and cannot be `LoadAsset`-ed by name.
- Build: `AugaUnity/Assets/Editor/AugaLauncher.cs:140-145` -> `AugaUnity/AssetBundles/augaassets` (StandaloneWindows).
  Headless: `Unity.exe -batchmode -quit -projectPath AugaUnity -executeMethod AugaLauncher.BuildAssetBundles -logFile build.log`.
- Plugin embeds it (`Auga/Auga.csproj:61`, LogicalName `Auga.augaassets`), loads in `Auga.cs:605-617`; `LoadAssets()` `Auga.cs:554-598`
  fills `AugaAssets` (`Auga.cs:21-63`) via `LoadAsset<T>("Name")`.
- New asset: tag its .meta, add an `AugaAssets` field + LoadAsset line (or `Auga.AssetBundle.LoadAsset<T>`), rebuild bundle + plugin.
- Serialization is ForceText (`ProjectSettings/EditorSettings.asset:8`): prefab/meta YAML is text-editable.
- Headless prefab render: `AugaPortTools.RenderPrefab -augaPrefab Assets/Prefabs/X.prefab -augaOut out.png [-augaSize WxH]` (no -nographics).

## Auga look
- Panel: `AugaPanelBase` = UIGradient 4-corner fill (UL .125,.102,.082 / UR .282,.235,.188 / LR .322,.267,.216 / LL .180,.153,.125),
  `CornerDecoration` 111x106 corners tinted #706457, `darken_blob` 512 (border 160) backdrop.
- Text boxes: `TextBackdrop` 33x32 (border 5,4,5,4) tint (.094,.078,.063,.8). Dividers (#706457): Divider_Cap/_Left, Chevron_A/b, Diamond.
- Buttons: SettignsButton* 296x44 (border 28), SmallButton* 77x22, MediumButton* 244x70, FancyButton* 548x100, DiamondButton* 240.
- Slots: Container_Square_A 128 (tagged), Container_Square_B_Sliced 85x84, Container_Diamond 104, Container_Circle 102.
- Palette: text #EAE1D9 body, #A39689 dim, #D1C9C2, #EAA800 topic gold, #FFBF1B brightest gold; images #706457 ornament, #181410 dark,
  #B98A12 gold. Fonts: SourceSansPro-Bold/Regular SDF, Norsebold SDF (titles), Norse SDF.
- Runtime skin helpers: `SettingsSkin.cs` Load :51-84 (sprite harvest), Art(name) :86, Apply :89-137, SkinImage :139-209,
  Use :211-222, ReplacePanel :225-252, SkinButton :254-278, SkinText :280-336; `BuildMenuSkin.cs` Skin :107-145.

## hud-5
`AugaUnity/Assets/Prefabs/HUD.prefab`: IconDeath/Icon Image (line ~68) uses mapicon_boss guid 6f6e53c863102d74d9b609ff1147c922;
IconBoss/Icon Image (line ~21701) uses mapicon_death guid 59e81aaeece19184cbda0246ebf97e0d -> swap (and sizeDeltas 32x32/32x30).
Code stopgap to remove: `Auga/Minimap_Setup.cs:120-128`.

## Panels still vanilla (open in single player)
1. UnifiedPopup `IngameGui/UnifiedPopup` (bkg woodpanel_512x512, buttons `button`, AveriaSerif TMP). Open: FavoritePieceList
   .OpenNewFavoriteTagPopup / BuildUi.ShowDeletePopup; main menu ServerOptions disclaimer (pref ServerOptionsDisclaimer=0).
2. SessionPlayerList: `call Menu.instance.OnCurrentPlayers` after `ui menu open` (button hidden in SP). Blocked-list use is
   already skinned (`SettingsSkin.cs:351-383`), reuse.
3. Achievements panel `InventoryGui.m_achievementsPanel` (AchFrame border woodpanel_trophys 1310x800, AchElement bkg
   InputFieldBackground, selection_frame, Closebutton `button`). achievements.chs.
4. AchievementUnlockPopup (no board: serif font + bare layout). ClaudeHeim `achievementpopup` (extras.chs).
5. Piece author window `hudroot/BuildHud/IngameGui_HUD_HoveredPieceAuthor` (Bkg2 `Background`, Norsebold TMP). Needs
   `set Hud.s_showBuildPieceAuthor true`, a placed piece, hammer equipped, hover it.
6. Radial frame `hudroot/ValheimRadial` (T_radialHighlighter, T_radialIndicator, ElementInfo, InventoryInfo `Background`).
   audit3.chs lines 18-26.
7. ServerOptionsGUI `StartGui_ServerOptions` (panel/bkg woodpanel_512x512 540x649, preset buttons `button`, sliders).
   worldflow.chs 26-33.
Also: `root/Trophies` uses woodpanel_trophys (check if Auga shows it).
