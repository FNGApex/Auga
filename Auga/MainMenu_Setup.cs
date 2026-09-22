using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AugaUnity;
using HarmonyLib;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Auga
{

    [HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.SnapTo))]
    public static class SnapTo_Patch
    {
        public static void Postfix(TextsDialog __instance, RectTransform listRoot, ScrollRect scrollRect)
        {
            var augaTextComponent = __instance.GetComponent<AugaTextsDialogFilter>();
            if (augaTextComponent == null)
                return;

            var newVector = new Vector2(0, listRoot.anchoredPosition.y);
            listRoot.anchoredPosition = newVector;
        }
    }

    /// <summary>
    /// Valheim 1.0 port of Auga's main menu replacement.
    ///
    /// Everything happens in a Postfix, not a Prefix as in 2023: 1.0's FejdStartup.Awake dereferences
    /// m_crossplayServerToggle, m_menuList, m_startGamePanel, m_serverOptions and the camera markers before
    /// anything else, and Auga's components (AugaCharacterSelectPhotoBooth, CharacterPortraitsController)
    /// read FejdStartup.instance in their own Awake - which vanilla Awake only assigns on its first line.
    /// Running after it means the serialized vanilla references are still intact when the game needs them,
    /// and FejdStartup.instance exists when Auga's prefab wakes up.
    ///
    /// Nothing is destroyed: Extensions.Replace turns each vanilla branch into a hidden "_VanillaDonor"
    /// (PortCarryOver), so every field that has no Auga counterpart keeps pointing at a live - if invisible -
    /// vanilla object instead of going null. Fields are only re-pointed at Auga objects that can actually
    /// carry them; the rest is listed in the port comments below.
    /// </summary>
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    public static class FejdStartup_Awake_Patch
    {
        /// <summary>Set when the vanilla password error label was moved into Auga's tooltip slot.</summary>
        internal static bool PasswordErrorAdopted;

        [UsedImplicitly]
        public static void Prefix()
        {
            ZInput.Initialize();
        }

        [UsedImplicitly]
        public static void Postfix(FejdStartup __instance)
        {
            var prefab = Auga.Assets.MainMenuPrefab;
            if (prefab == null)
            {
                Auga.LogError("MainMenu prefab missing from the asset bundle, the main menu stays vanilla");
                return;
            }

            // Valheim 1.0 port: the Settings screen is skinned in place by SettingsSkin.cs - 1.0 split Settings
            // into Valheim.SettingsGui.* tab components and Auga's AugaSettings prefab still carries the old
            // monolithic layout, so m_settingsPrefab deliberately stays vanilla.

            // Keep the vanilla branches around so their children can be pulled over one by one below.
            var vanillaMenu = __instance.transform.Find("Menu");
            var vanillaStartGame = __instance.transform.Find("StartGame");

            SetupMenu(__instance, prefab, vanillaMenu);
            SetupConnectionFailed(__instance, prefab);
            SetupCredits(__instance, prefab);
            SetupLoading(__instance, prefab);
            SetupCharacterSelect(__instance, prefab);
            SetupNewCharacter(__instance, prefab);
            SetupStartGame(__instance, prefab, vanillaStartGame);

            // The fade / first-startup animation lives on the prefab root and drives children called Menu,
            // StartGame, CharacterSelection, Credits, Loading and BLACK - the names the screens above now have.
            if (__instance.m_menuAnimator != null)
            {
                var augaAnimator = prefab.GetComponent<Animator>();
                if (augaAnimator != null && augaAnimator.runtimeAnimatorController != null)
                {
                    __instance.m_menuAnimator.runtimeAnimatorController = augaAnimator.runtimeAnimatorController;
                }
            }

            SetupPhotoBooth(__instance, prefab);

            RelabelButtons(__instance.transform);
            Localization.instance.Localize(__instance.transform);
        }

        /// <summary>
        /// Valheim 1.0 port: Auga's menu buttons are nested prefab instances whose per-instance label overrides did
        /// not survive the Unity 6 bundle rebuild - every button came up as "Label" (or the first entry's text).
        /// The labels are set here from localization tokens, with the English text as a fallback for a token
        /// the game does not have.
        /// </summary>
        private static void RelabelButtons(Transform root)
        {
            var labels = new (string path, string token, string fallback)[]
            {
                ("Menu/MenuList/StartGame", "$menu_start", "Start"),
                ("Menu/MenuList/Settings", "$menu_settings", "Settings"),
                ("Menu/MenuList/Credits", "$menu_credits", "Credits"),
                ("Menu/MenuList/Exit", "$menu_exit", "Quit"),
                ("CharacterSelection/SelectCharacter/Panel/Inset/RemoveButton", "$menu_remove", "Remove"),
                ("CharacterSelection/SelectCharacter/Panel/Inset/NewButton", "$menu_new", "New"),
                ("CharacterSelection/SelectCharacter/Panel/Inset/NewButtonBig", "$menu_new", "New"),
                ("CharacterSelection/SelectCharacter/Panel/Back", "$menu_back", "Back"),
                ("CharacterSelection/SelectCharacter/Panel/Start", "$menu_start", "Start"),
                ("CharacterSelection/SelectCharacter/Panel/StartDisabled", "$menu_start", "Start"),
                ("CharacterSelection/SelectCharacter/Panel/ManageSaves", "$menu_managesaves", "Manage saves"),
                ("CharacterSelection/SelectCharacter/RemoveCharacterDialog/ButtonYes", "$menu_yes", "Yes"),
                ("CharacterSelection/SelectCharacter/RemoveCharacterDialog/ButtonNo", "$menu_no", "No"),
                ("CharacterSelection/NewCharacterPanel/Panel/Done", "$menu_done", "Done"),
                ("CharacterSelection/NewCharacterPanel/Panel/Cancel", "$menu_cancel", "Cancel"),
                ("StartGame/Panel/WorldPanel/RemoveButton", "$menu_remove", "Remove"),
                ("StartGame/Panel/WorldPanel/NewButton", "$menu_new", "New"),
                ("StartGame/Panel/WorldPanel/Back", "$menu_back", "Back"),
                ("StartGame/Panel/WorldPanel/Start", "$menu_start", "Start"),
                ("StartGame/RemoveWorldDialog/ButtonYes", "$menu_yes", "Yes"),
                ("StartGame/RemoveWorldDialog/ButtonNo", "$menu_no", "No"),
                ("StartGame/NewWorldDialog/Done", "$menu_done", "Done"),
                ("StartGame/NewWorldDialog/Cancel", "$menu_cancel", "Cancel"),
                ("StartGame/JoinIP/Connect", "$menu_joinip", "Connect"),
                ("StartGame/JoinIP/Cancel", "$menu_cancel", "Cancel"),
                ("ConnectionFailed/ButtonYes", "$menu_ok", "OK"),
                ("Credits/Back-panel/ButtonSettings", "$menu_back", "Back"),
            };

            foreach (var (path, token, fallback) in labels)
            {
                var button = root.Find(path);
                if (button == null)
                {
                    continue;
                }

                var label = button.Find("Label") ?? button.Find("Text");
                if (label == null)
                {
                    continue;
                }

                var localized = Localization.instance.Localize(token);
                var text = localized.StartsWith("[") && localized.EndsWith("]") ? fallback : localized;
                var tmp = label.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    tmp.text = text;
                    continue;
                }

                var legacy = label.GetComponent<Text>();
                if (legacy != null)
                {
                    legacy.text = text;
                }
            }
        }

        private static void SetupMenu(FejdStartup startup, GameObject prefab, Transform vanillaMenu)
        {
            // Valheim 1.0 port: BLACK is the full screen fader the menu animator drives; it has no FejdStartup
            // field, it only has to exist under the name the controller animates.
            startup.Replace("BLACK", prefab);

            var menu = startup.Replace("Menu", prefab);
            if (menu == null)
            {
                return;
            }

            startup.m_mainMenu = menu.gameObject;

            if (vanillaMenu != null)
            {
                // The logo (1.0 ships the Deep North one) is the one piece of the vanilla menu worth keeping:
                // move it out of the hidden donor so it shows above Auga's own list, as Auga did in 2023.
                var logo = vanillaMenu.Find("Logo");
                if (logo != null)
                {
                    logo.SetParent(menu, true);
                    logo.SetAsFirstSibling();
                }

                // Auga's changelog panel is a 2023 copy of ChangeLog: the TextAsset fields it needs are empty
                // and m_textField was a Text back then, so ChangeLog.Start would throw. Fill both from vanilla.
                var vanillaChangeLog = vanillaMenu.GetComponentInChildren<ChangeLog>(true);
                var augaChangeLog = menu.GetComponentInChildren<ChangeLog>(true);
                if (vanillaChangeLog != null && augaChangeLog != null)
                {
                    PortCarryOver.Fill(augaChangeLog.gameObject, vanillaChangeLog.gameObject);
                }
            }

            var menuList = F(menu, "MenuList");
            if (menuList != null)
            {
                startup.m_menuList = menuList.gameObject;
            }

            SetButtonListener(menu, "MenuList/StartGame", startup.OnStartGame);
            SetButtonListener(menu, "MenuList/Settings", startup.OnButtonSettings);
            SetButtonListener(menu, "MenuList/Credits", startup.OnCredits);
            SetButtonListener(menu, "MenuList/Exit", startup.OnAbort);
            startup.m_menuSelectedButton = FC<Button>(menu, "MenuList/StartGame") ?? startup.m_menuSelectedButton;

            // Awake cached the vanilla entries in m_menuButtons before the swap; UpdateKeyboard indexes that
            // array unconditionally, so it has to be the Auga entries now.
            var buttons = startup.m_menuList != null ? startup.m_menuList.GetComponentsInChildren<Button>() : null;
            if (buttons != null && buttons.Length > 0)
            {
                startup.m_menuButtons = buttons;
            }

            // SetupGui writes the version string into m_versionLabel, a TMP_Text; Auga's Version is a 2023
            // UnityEngine.UI.Text, so the vanilla label takes its place instead (see AdoptLabel).
            startup.m_versionLabel = AdoptLabel(F(menu, "Version"), startup.m_versionLabel);

            // Valheim 1.0 port left vanilla (hidden in the donor, so the game keeps working on them):
            //   m_betaText / m_ndaPanel / m_moddedText / m_merchStoreButtonParent / m_showEulaButton /
            //   m_showLogButton / m_showChangelogButton / m_changeLog / m_changeLogNotice / m_patchLogScroll /
            //   m_cinematicsMenuList (1.0's Cinematics entry) - Auga's menu has no place for them.
        }

        private static void SetupConnectionFailed(FejdStartup startup, GameObject prefab)
        {
            var connectionFailed = startup.Replace("ConnectionFailed", prefab);
            if (connectionFailed == null)
            {
                return;
            }

            startup.m_connectionFailedPanel = connectionFailed.gameObject;
            startup.m_connectionFailedError = AdoptLabel(F(connectionFailed, "Text"), startup.m_connectionFailedError);
            SetButtonListener(connectionFailed, "ButtonYes", startup.OnConnectionFailedOk);
            // The Auga dialog is a ConfirmDialog with two buttons; there is only one vanilla action, so the
            // second one closes it too rather than sitting there dead.
            SetButtonListener(connectionFailed, "ButtonNo", startup.OnConnectionFailedOk);
        }

        private static void SetupCredits(FejdStartup startup, GameObject prefab)
        {
            var credits = startup.Replace("Credits", prefab);
            if (credits == null)
            {
                return;
            }

            startup.m_creditsPanel = credits.gameObject;
            var list = F(credits, "ContactInfo") as RectTransform;
            if (list != null)
            {
                startup.m_creditsList = list;
            }

            SetButtonListener(credits, "Back-panel/ButtonSettings", startup.OnCreditsBack);
        }

        private static void SetupLoading(FejdStartup startup, GameObject prefab)
        {
            var loading = startup.Replace("Loading", prefab);
            if (loading == null)
            {
                return;
            }

            startup.m_loading = loading.gameObject;
            startup.m_loading.SetActive(false);

            // Valheim 1.0 port: PleaseWait stays vanilla - Auga's copy never worked here (see the 2023 note)
            // and it is a root screen of its own, so the vanilla one is untouched and fully functional.
        }

        private static void SetupCharacterSelect(FejdStartup startup, GameObject prefab)
        {
            // Only the panel is swapped, not the CharacterSelection node above it: that node stays as the live
            // container m_characterSelectScreen already points at, and holds both Auga panels.
            var charSelect = startup.Replace("CharacterSelection/SelectCharacter", prefab);
            if (charSelect == null)
            {
                return;
            }

            startup.m_selectCharacterPanel = charSelect.gameObject;
            startup.m_removeCharacterDialog = FO(charSelect, "RemoveCharacterDialog") ?? startup.m_removeCharacterDialog;
            startup.m_removeCharacterName = AdoptLabel(F(charSelect, "RemoveCharacterDialog/Text"), startup.m_removeCharacterName);

            startup.m_csRemoveButton = FC<Button>(charSelect, "Panel/Inset/RemoveButton") ?? startup.m_csRemoveButton;
            startup.m_csStartButton = FC<Button>(charSelect, "Panel/Start") ?? startup.m_csStartButton;
            startup.m_csNewButton = FC<Button>(charSelect, "Panel/Inset/NewButton") ?? startup.m_csNewButton;
            startup.m_csNewBigButton = FC<Button>(charSelect, "Panel/Inset/NewButtonBig") ?? startup.m_csNewBigButton;

            SetButtonListener(charSelect, "Panel/Inset/RemoveButton", startup.OnCharacterRemove);
            SetButtonListener(charSelect, "Panel/Inset/NewButton", startup.OnCharacterNew);
            SetButtonListener(charSelect, "Panel/Inset/NewButtonBig", startup.OnCharacterNew);
            SetButtonListener(charSelect, "Panel/Back", startup.OnSelelectCharacterBack);
            SetButtonListener(charSelect, "Panel/Start", startup.OnCharacterStart);
            SetButtonListener(charSelect, "Panel/ManageSaves", () => startup.OnManageSaves(1));
            SetButtonListener(charSelect, "RemoveCharacterDialog/ButtonYes", startup.OnButtonRemoveCharacterYes);
            SetButtonListener(charSelect, "RemoveCharacterDialog/ButtonNo", startup.OnButtonRemoveCharacterNo);

            // Valheim 1.0 port left vanilla (hidden, still written to by UpdateCharacterList so nothing throws):
            //   m_csName / m_csFileSource / m_csSourceInfo / m_csLeftButton / m_csRightButton. Auga replaces the
            //   one-character-at-a-time carousel with AugaCharacterSelect's portrait list, which draws the names
            //   and the save source itself; the 2023 port pointed those fields at a dummy Text, which cannot
            //   work in 1.0 because they are all TMP_Text now.
        }

        private static void SetupNewCharacter(FejdStartup startup, GameObject prefab)
        {
            var newCharacter = startup.Replace("CharacterSelection/NewCharacterPanel", prefab);
            if (newCharacter == null)
            {
                return;
            }

            startup.m_newCharacterPanel = newCharacter.gameObject;
            startup.m_csNewCharacterDone = FC<Button>(newCharacter, "Panel/Done") ?? startup.m_csNewCharacterDone;
            startup.m_csNewCharacterCancel = FC<Button>(newCharacter, "Panel/Cancel") ?? startup.m_csNewCharacterCancel;
            startup.m_newCharacterError = FO(newCharacter, "Panel/Content/NameExistsWarning") ?? startup.m_newCharacterError;

            var nameField = FC<GUIFramework.GuiInputField>(newCharacter, "Panel/Content/CharacterName");
            if (nameField != null)
            {
                startup.m_csNewCharacterName = nameField;
            }

            // Valheim 1.0 port: 1.0 keeps saving to the cloud by default, so Done asks for the cloud slot
            // (the 2023 port forced local here, which would silently turn cloud saves off).
            SetButtonListener(newCharacter, "Panel/Done", () => startup.OnNewCharacterDone(false));
            SetButtonListener(newCharacter, "Panel/Cancel", startup.OnNewCharacterCancel);

            // PlayerCustomizaton's own references (sliders, toggles, beard panel) come from the prefab, and the
            // ones the bundle cannot carry - m_noHair / m_noBeard, and m_selectedHair / m_selectedBeard, which
            // were Text in 2023 and are TMP_Text now - were filled from the hidden vanilla panel by Replace.
            var customization = newCharacter.GetComponent<PlayerCustomizaton>();
            if (customization != null)
            {
                BindSexToggle(FC<Toggle>(newCharacter, "Panel/Content/ToggleGroup/Toggle_Female"), customization, 1);
                BindSexToggle(FC<Toggle>(newCharacter, "Panel/Content/ToggleGroup/Toggle_Male"), customization, 0);
            }

            // The hair / beard portrait list is driven by CharacterPortraitsController on the panel; its tab
            // buttons only switch the mode, the controller re-renders on the next Update.
            var portraits = newCharacter.GetComponentInChildren<CharacterPortraitsController>(true);
            if (portraits != null)
            {
                var hairTab = FC<Button>(newCharacter, "Panel/Content/TabButtons/Tabs/Hair");
                if (hairTab != null)
                {
                    hairTab.onClick = new Button.ButtonClickedEvent();
                    hairTab.onClick.AddListener(portraits.SwitchToHairMode);
                }

                var beardTab = FC<Button>(newCharacter, "Panel/Content/TabButtons/Tabs/Beard");
                if (beardTab != null)
                {
                    beardTab.onClick = new Button.ButtonClickedEvent();
                    beardTab.onClick.AddListener(portraits.SwitchToBeardMode);
                }
            }
        }

        private static void BindSexToggle(Toggle toggle, PlayerCustomizaton customization, int playerModel)
        {
            if (toggle == null)
            {
                return;
            }

            toggle.onValueChanged = new Toggle.ToggleEvent();
            toggle.onValueChanged.AddListener(on =>
            {
                if (on)
                {
                    customization.SetPlayerModel(playerModel);
                }
            });

            // Auga's 2023 toggles show their checked state through a child rather than Toggle.graphic.
            if (toggle.graphic == null && toggle.transform.childCount > 1)
            {
                var marker = toggle.transform.GetChild(1).gameObject;
                toggle.onValueChanged.AddListener(marker.SetActive);
                marker.SetActive(toggle.isOn);
            }
        }

        private static void SetupStartGame(FejdStartup startup, GameObject prefab, Transform vanillaStartGame)
        {
            var startGame = startup.Replace("StartGame", prefab);
            if (startGame == null)
            {
                return;
            }

            startup.m_startGamePanel = startGame.gameObject;

            var panel = F(startGame, "Panel");
            if (panel != null)
            {
                // OnServerListTab reaches the tabs through m_startGamePanel.transform.GetChild(0).
                panel.SetAsFirstSibling();
            }

            startup.m_worldListPanel = FO(startGame, "Panel/WorldPanel") ?? startup.m_worldListPanel;
            startup.m_createWorldPanel = FO(startGame, "NewWorldDialog") ?? startup.m_createWorldPanel;
            startup.m_removeWorldDialog = FO(startGame, "RemoveWorldDialog") ?? startup.m_removeWorldDialog;
            startup.m_removeWorldName = AdoptLabel(F(startGame, "RemoveWorldDialog/Text"), startup.m_removeWorldName);

            var worldListRoot = F(startGame, "Panel/WorldPanel/ScrollRect/ItemList") as RectTransform;
            if (worldListRoot != null)
            {
                startup.m_worldListRoot = worldListRoot;
            }

            startup.m_worldListEnsureVisible = FC<ScrollRectEnsureVisible>(startGame, "Panel/WorldPanel/ScrollRect")
                                               ?? startup.m_worldListEnsureVisible;

            // Valheim 1.0 port: m_worldListElement stays vanilla. UpdateWorldList reads "name", "seed" and a
            // "modifiers" child as TMP_Text; Auga's WorldListElement has no "modifiers" at all and labels with
            // UnityEngine.UI.Text, so using it would throw on every refresh. Rebuilding that prefab in Unity is
            // what it takes to Augafy the rows; until then the vanilla row is instantiated into Auga's list.

            // The world tooltips (world modifiers) are anchored to two empty rects inside the vanilla panel;
            // move them into Auga's world panel or they would pop up inside the hidden donor.
            var augaWorldPanel = F(startGame, "Panel/WorldPanel");
            MoveAnchor(startup.m_tooltipAnchor, augaWorldPanel);
            MoveAnchor(startup.m_tooltipSecondaryAnchor, augaWorldPanel);

            startup.m_worldStart = FC<Button>(startGame, "Panel/WorldPanel/Start") ?? startup.m_worldStart;
            startup.m_worldRemove = FC<Button>(startGame, "Panel/WorldPanel/RemoveButton") ?? startup.m_worldRemove;

            var newWorldName = FC<GUIFramework.GuiInputField>(startGame, "NewWorldDialog/WorldName");
            if (newWorldName != null)
            {
                startup.m_newWorldName = newWorldName;
            }

            var newWorldSeed = FC<GUIFramework.GuiInputField>(startGame, "NewWorldDialog/WorldSeed");
            if (newWorldSeed != null)
            {
                startup.m_newWorldSeed = newWorldSeed;
            }

            startup.m_newWorldDone = FC<Button>(startGame, "NewWorldDialog/Done") ?? startup.m_newWorldDone;

            SetupServerFields(startup, startGame);

            SetButtonListener(startGame, "Panel/WorldPanel/Start", startup.OnWorldStart);
            SetButtonListener(startGame, "Panel/WorldPanel/RemoveButton", startup.OnWorldRemove);
            SetButtonListener(startGame, "Panel/WorldPanel/NewButton", startup.OnWorldNew);
            SetButtonListener(startGame, "Panel/WorldPanel/Back", startup.OnStartGameBack);
            SetButtonListener(startGame, "RemoveWorldDialog/ButtonYes", startup.OnButtonRemoveWorldYes);
            SetButtonListener(startGame, "RemoveWorldDialog/ButtonNo", startup.OnButtonRemoveWorldNo);
            SetButtonListener(startGame, "NewWorldDialog/Cancel", startup.OnNewWorldBack);
            // 1.0 keeps saving to the cloud by default - see the new character Done button.
            SetButtonListener(startGame, "NewWorldDialog/Done", () => startup.OnNewWorldDone(false));

            var joinPanel = MoveServerBrowser(startup, startGame, vanillaStartGame);
            SetupTabs(startup, startGame, joinPanel);
        }

        private static void SetupServerFields(FejdStartup startup, Transform startGame)
        {
            var publicToggle = FC<Toggle>(startGame, "Panel/WorldPanel/CheckboxRow/StartPublicGameToggle");
            if (publicToggle != null)
            {
                startup.m_publicServerToggle = publicToggle;
                AddTmpColorProbe(publicToggle);
            }

            var openToggle = FC<Toggle>(startGame, "Panel/WorldPanel/CheckboxRow/StartServerToggle");
            if (openToggle != null)
            {
                startup.m_openServerToggle = openToggle;
                AddTmpColorProbe(openToggle);
            }

            var password = FC<GUIFramework.GuiInputField>(startGame, "Panel/WorldPanel/ServerPassword");
            if (password != null)
            {
                startup.m_serverPassword = password;
            }

            var errorSlot = F(startGame, "Panel/WorldPanel/ServerPassword/Tooltip/ErrorText");
            startup.m_passwordError = AdoptLabel(errorSlot, startup.m_passwordError, out var errorAdopted);
            PasswordErrorAdopted = errorAdopted;

            // Valheim 1.0 port left vanilla (hidden): m_crossplayServerToggle, the two
            // m_samePlatformOnlyToggle* and m_serverOptionsButton / m_serverOptions (the world modifiers
            // window). Auga's 2023 world panel has no row for them; the toggles keep their PlatformPrefs
            // value and Update only touches the button when it is visible, which it never is.
        }

        /// <summary>
        /// Valheim 1.0 port: the server browser is a ServerListGui component on the vanilla JoinPanel now, and
        /// FejdStartup lost every server list field it used to have (m_serverListRoot, m_serverListElement,
        /// m_serverCount, m_serverRefreshButton, m_filterInputField, m_friendFilterSwitch, m_publicFilterSwitch,
        /// m_joinGameButton and the whole Join-by-IP dialog). Auga's JoinPanel is an empty shell against that,
        /// so the working vanilla panel is moved into Auga's frame and Auga's own one is switched off.
        /// </summary>
        private static RectTransform MoveServerBrowser(FejdStartup startup, Transform startGame, Transform vanillaStartGame)
        {
            var slot = F(startGame, "Panel/JoinPanel") as RectTransform;
            var vanillaJoinPanel = vanillaStartGame != null ? vanillaStartGame.Find("Panel/JoinPanel") as RectTransform : null;
            if (slot == null || vanillaJoinPanel == null)
            {
                Auga.LogWarning("Main menu: server browser left where it was, Auga's JoinPanel or the vanilla one is missing");
                if (slot != null)
                {
                    startup.m_serverListPanel = slot.gameObject;
                }

                return null;
            }

            vanillaJoinPanel.SetParent(slot.parent, false);
            CopyPlacement(slot, vanillaJoinPanel);
            vanillaJoinPanel.SetSiblingIndex(slot.GetSiblingIndex());
            vanillaJoinPanel.gameObject.SetActive(false);

            slot.gameObject.SetActive(false);
            slot.name = "JoinPanel_AugaUnused";

            startup.m_serverListPanel = vanillaJoinPanel.gameObject;
            return vanillaJoinPanel;
        }

        private static void SetupTabs(FejdStartup startup, Transform startGame, RectTransform joinPanel)
        {
            var tabHandler = FC<TabHandler>(startGame, "Panel");
            if (tabHandler == null || tabHandler.m_tabs == null || tabHandler.m_tabs.Count < 2)
            {
                Auga.LogWarning("Main menu: Auga's StartGame panel has no two-tab TabHandler, world / join tabs not wired");
                return;
            }

            tabHandler.m_tabs[0].m_onClick = new UnityEvent();
            tabHandler.m_tabs[0].m_onClick.AddListener(startup.OnSelectWorldTab);
            tabHandler.m_tabs[1].m_onClick = new UnityEvent();
            tabHandler.m_tabs[1].m_onClick.AddListener(startup.OnServerListTab);
            if (joinPanel != null)
            {
                tabHandler.m_tabs[1].m_page = joinPanel;
            }
        }

        /// <summary>
        /// Valheim 1.0 port: AugaCharacterSelectPhotoBooth clones the menu camera and switches its DepthOfField
        /// and PostProcessingBehaviour off. If 1.0's menu camera no longer carries those, its Awake would throw
        /// on every start and its coroutine would keep throwing, so the portraits are only added when it can work.
        /// </summary>
        private static void SetupPhotoBooth(FejdStartup startup, GameObject prefab)
        {
            var photoBoothPrefab = prefab.GetComponentInChildren<AugaCharacterSelectPhotoBooth>(true);
            if (photoBoothPrefab == null)
            {
                return;
            }

            var camera = startup.m_mainCamera != null ? startup.m_mainCamera.GetComponent<Camera>() : null;
            if (camera == null || camera.GetComponent("DepthOfField") == null || camera.GetComponent("PostProcessingBehaviour") == null
                || startup.m_cameraMarkerCharacter == null)
            {
                Auga.LogWarning("Main menu: character portraits are off, 1.0's menu camera has no DepthOfField / PostProcessingBehaviour to clone");
                return;
            }

            Object.Instantiate(photoBoothPrefab, startup.transform);
        }

        // ---- helpers -------------------------------------------------------------------------------------

        private static Transform F(Transform root, string path)
        {
            if (root == null)
            {
                return null;
            }

            var found = root.Find(path);
            if (found == null)
            {
                Auga.LogWarning($"Main menu: '{path}' not found under '{root.name}'");
            }

            return found;
        }

        private static T FC<T>(Transform root, string path) where T : Component
        {
            var found = F(root, path);
            if (found == null)
            {
                return null;
            }

            var component = found.GetComponent<T>();
            if (component == null)
            {
                Auga.LogWarning($"Main menu: no {typeof(T).Name} on '{path}'");
            }

            return component;
        }

        private static GameObject FO(Transform root, string path)
        {
            var found = F(root, path);
            return found != null ? found.gameObject : null;
        }

        private static void SetButtonListener(Transform root, string path, UnityAction listener)
        {
            var button = FC<Button>(root, path);
            if (button == null)
            {
                return;
            }

            // A new event also drops the calls baked into the prefab, whose targets do not survive the bundle.
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(listener);
        }

        /// <summary>
        /// Valheim 1.0 port: every label field on FejdStartup is a TMP_Text now, while Auga's 2023 prefabs use
        /// UnityEngine.UI.Text, so the field cannot be pointed at Auga's label. The vanilla label - still alive
        /// inside the hidden donor and still the one the game writes to - is moved into Auga's slot instead and
        /// takes over its placement, and Auga's own label is switched off.
        /// </summary>
        private static TMP_Text AdoptLabel(Transform slot, TMP_Text vanillaLabel)
        {
            return AdoptLabel(slot, vanillaLabel, out _);
        }

        private static TMP_Text AdoptLabel(Transform slot, TMP_Text vanillaLabel, out bool adopted)
        {
            adopted = false;
            var rect = slot as RectTransform;
            if (rect == null || vanillaLabel == null || rect.parent == null)
            {
                return vanillaLabel;
            }

            var label = vanillaLabel.rectTransform;
            if (label == null)
            {
                return vanillaLabel;
            }

            label.SetParent(rect.parent, false);
            CopyPlacement(rect, label);
            label.SetSiblingIndex(rect.GetSiblingIndex());

            var augaLabel = rect.GetComponent<Text>();
            if (augaLabel != null)
            {
                vanillaLabel.color = augaLabel.color;
                vanillaLabel.fontSize = augaLabel.fontSize;
                vanillaLabel.alignment = ToTextAlignmentOptions(augaLabel.alignment);
            }

            rect.gameObject.SetActive(false);
            vanillaLabel.gameObject.SetActive(true);
            adopted = true;
            return vanillaLabel;
        }

        /// <summary>Move an empty vanilla rect (a tooltip anchor) out of its hidden donor into an Auga panel.</summary>
        private static void MoveAnchor(RectTransform anchor, Transform newParent)
        {
            if (anchor == null || newParent == null || anchor.IsChildOf(newParent))
            {
                return;
            }

            anchor.SetParent(newParent, false);
            anchor.gameObject.SetActive(true);
        }

        private static void CopyPlacement(RectTransform from, RectTransform to)
        {
            to.anchorMin = from.anchorMin;
            to.anchorMax = from.anchorMax;
            to.pivot = from.pivot;
            to.sizeDelta = from.sizeDelta;
            to.anchoredPosition = from.anchoredPosition;
            to.localScale = from.localScale;
            to.localRotation = from.localRotation;
        }

        /// <summary>
        /// Valheim 1.0 port: FejdStartup.SetToggleState recolours a toggle through GetComponentInChildren&lt;TMP_Text&gt;
        /// every frame the start screen is up, and throws when it finds none. Auga's checkbox labels are
        /// UnityEngine.UI.Text, so an empty TMP label is added for it to write to and Auga's label keeps its colour.
        /// </summary>
        private static void AddTmpColorProbe(Toggle toggle)
        {
            if (toggle == null || toggle.GetComponentInChildren<TMP_Text>(true) != null)
            {
                return;
            }

            var probe = new GameObject("_AugaToggleColorProbe", typeof(RectTransform));
            probe.layer = toggle.gameObject.layer;
            var rect = (RectTransform)probe.transform;
            rect.SetParent(toggle.transform, false);
            rect.sizeDelta = Vector2.zero;
            var text = probe.AddComponent<TextMeshProUGUI>();
            // A TMP with no font asset throws inside GetPreferredWidth when the layout asks for its size.
            SettingsSkin.Load();
            var font = SettingsSkin._regular ?? Resources.FindObjectsOfTypeAll<TMP_FontAsset>().FirstOrDefault();
            if (font != null)
            {
                text.font = font;
            }

            text.text = string.Empty;
            text.raycastTarget = false;
        }

        private static TextAlignmentOptions ToTextAlignmentOptions(TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft: return TextAlignmentOptions.TopLeft;
                case TextAnchor.UpperCenter: return TextAlignmentOptions.Top;
                case TextAnchor.UpperRight: return TextAlignmentOptions.TopRight;
                case TextAnchor.MiddleLeft: return TextAlignmentOptions.Left;
                case TextAnchor.MiddleCenter: return TextAlignmentOptions.Center;
                case TextAnchor.MiddleRight: return TextAlignmentOptions.Right;
                case TextAnchor.LowerLeft: return TextAlignmentOptions.BottomLeft;
                case TextAnchor.LowerCenter: return TextAlignmentOptions.Bottom;
                default: return TextAlignmentOptions.BottomRight;
            }
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.UpdateCharacterList))]
    public static class FejdStartup_UpdateCharacterList_Patch
    {
        [UsedImplicitly]
        public static void Postfix(FejdStartup __instance)
        {
            var characterSelect = __instance.GetComponentInChildren<AugaCharacterSelect>(true);
            if (characterSelect == null)
            {
                return;
            }

            try
            {
                characterSelect.UpdateCharacterList();
            }
            catch (Exception e)
            {
                // Best effort: the portrait list is Auga's own extra, the vanilla screen underneath is done by now.
                Auga.LogWarning($"Main menu: character portrait list failed to refresh: {e.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.UpdatePasswordError))]
    public static class FejdStartup_UpdatePasswordError_Patch
    {
        [UsedImplicitly]
        public static void Postfix(FejdStartup __instance)
        {
            // Auga shows the error in a tooltip bubble that only makes sense when there is something in it.
            if (!FejdStartup_Awake_Patch.PasswordErrorAdopted || __instance.m_passwordError == null)
            {
                return;
            }

            var bubble = __instance.m_passwordError.transform.parent;
            if (bubble != null)
            {
                bubble.gameObject.SetActive(!string.IsNullOrEmpty(__instance.m_passwordError.text));
            }
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.OnNewCharacterDone))]
    public static class FejdStartup_OnNewCharacterDone_Patch
    {
        [UsedImplicitly]
        public static void Postfix(FejdStartup __instance)
        {
            var photoBooth = __instance.GetComponentInChildren<AugaCharacterSelectPhotoBooth>(true);
            if (photoBooth != null)
                photoBooth.StartCoroutine(NewCharPhotoCoroutine(__instance, photoBooth));
        }

        private static IEnumerator NewCharPhotoCoroutine(FejdStartup instance, AugaCharacterSelectPhotoBooth photoBooth)
        {
            yield return photoBooth.TakePhoto(instance.m_profileIndex);
            instance.UpdateCharacterList();
        }
    }

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.ClearCharacterPreview))]
    public static class FejdStartup_ClearCharacterPreview_Patch
    {
        [UsedImplicitly]
        public static bool Prefix(FejdStartup __instance)
        {
            // While the photo booth cycles through the characters the change effect would fire on every one.
            if (AugaCharacterSelectPhotoBooth.TakingPhotos)
            {
                Object.Destroy(__instance.m_playerInstance);
                __instance.m_playerInstance = null;
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Valheim 1.0 port: both Auga camera components clone the menu camera and read m_playerInstance every
    /// frame. They are eye candy - portraits in the character list and in the hair / beard picker - so a
    /// missing camera effect or a preview that is not spawned yet must not take the menu down with it.
    /// </summary>
    [HarmonyPatch]
    public static class AugaMenuCamera_Patches
    {
        [UsedImplicitly]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            var targets = new[]
            {
                AccessTools.Method(typeof(CharacterPortraitsController), nameof(CharacterPortraitsController.Awake)),
                AccessTools.Method(typeof(CharacterPortraitsController), nameof(CharacterPortraitsController.Update)),
                AccessTools.Method(typeof(AugaCharacterSelectPhotoBooth), nameof(AugaCharacterSelectPhotoBooth.Awake)),
                AccessTools.Method(typeof(AugaCharacterSelectPhotoBooth), nameof(AugaCharacterSelectPhotoBooth.Start))
            };

            foreach (var target in targets)
            {
                // PatchAll gives up on the whole plugin at the first bad patch, so a renamed method is skipped.
                if (target != null)
                {
                    yield return target;
                }
            }
        }

        [UsedImplicitly]
        public static Exception Finalizer(Exception __exception)
        {
            return __exception is NullReferenceException ? null : __exception;
        }
    }
}
