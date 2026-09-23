using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using AugaUnity;
using HarmonyLib;
using JetBrains.Annotations;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Auga
{
    [HarmonyPatch]
    public static class PauseMenu_Setup
    {
        [HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.Update))]
        public static class TextDialog_Update_Patch
        {
            public static void TextDialogUpdate(TextsDialog instance)
            {
                instance.UpdateGamepadInput();
                if (instance.m_texts.Count <= 0)
                    return;

                if (instance.m_leftScrollbar == null)
                    return;

                if (instance.m_leftScrollRect == null)
                    return;

                instance.m_leftScrollbar.size = ((RectTransform)instance.m_leftScrollRect.transform).rect.height / instance.m_listRoot.rect.height;    
            }

            [UsedImplicitly]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instrs = instructions.ToList();

                var counter = 0;

                CodeInstruction LogMessage(CodeInstruction instruction)
                {
                    //Debug.LogWarning($"IL_{counter}: Opcode: {instruction.opcode} Operand: {instruction.operand}");
                    return instruction;
                }

                for (int i = 0; i < instrs.Count; ++i)
                {
                    if (i == 0)
                    {
                        yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_0));
                        counter++;

                        yield return LogMessage(new CodeInstruction(OpCodes.Call,AccessTools.DeclaredMethod(typeof(TextDialog_Update_Patch), nameof(TextDialogUpdate))));
                        counter++;

                        yield return LogMessage(new CodeInstruction(OpCodes.Ret));
                        counter++;

                    }
                }
            }
        }

        [HarmonyPatch(typeof(TextsDialog), nameof(TextsDialog.ShowText), new []{typeof(TextsDialog.TextInfo)})]
        public static class TextDialog_ShowText_Patch
        {
            public static void ShowText(TextsDialog instance, TextsDialog.TextInfo text)
            {
                if (text == null)
                    return;

                instance.m_textAreaTopic.text = Localization.instance.Localize(text.m_topic);
                instance.m_textArea.text = Localization.instance.Localize(text.m_text);
                foreach (TextsDialog.TextInfo text1 in instance.m_texts)
                    text1.m_selected.SetActive(false);
                text.m_selected.SetActive(true);
                if (instance.m_leftScrollRect != null)
                {
                    instance.StartCoroutine(instance.FocusOnCurrentLevel(instance.m_leftScrollRect, instance.m_listRoot, text.m_selected.transform as RectTransform));                    
                }
            }

            [UsedImplicitly]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instrs = instructions.ToList();

                var counter = 0;

                CodeInstruction LogMessage(CodeInstruction instruction)
                {
                    //Debug.LogWarning($"IL_{counter}: Opcode: {instruction.opcode} Operand: {instruction.operand}");
                    return instruction;
                }

                for (int i = 0; i < instrs.Count; ++i)
                {
                    if (i == 0)
                    {
                        yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_0));
                        counter++;

                        yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_1));
                        counter++;

                        yield return LogMessage(new CodeInstruction(OpCodes.Call,AccessTools.DeclaredMethod(typeof(TextDialog_ShowText_Patch), nameof(ShowText))));
                        counter++;

                        yield return LogMessage(new CodeInstruction(OpCodes.Ret));
                        counter++;

                    }
                }
            }
        }

        [HarmonyPatch(typeof(Menu), nameof(Menu.UpdateNavigation))]
        public static class Menu_UpdateNavigation_Patch
        {
            public static void UpdateNavigation(Menu instance)
            {
                try
                {
                    Button component1;
                    Button component2;
                    Button component3;
                    Button component4;
                    Button component5;

                    List<Button> buttonList = new List<Button>();

                    if (instance.name.StartsWith("Auga"))
                    {
                        component1 = instance.m_menuDialog.Find("MenuEntries/Logout").GetComponent<Button>();
                        component2 = instance.m_menuDialog.Find("MenuEntries/Exit").GetComponent<Button>();
                        component3 = instance.m_menuDialog.Find("MenuEntries/DividerMedium/CloseButton").GetComponent<Button>();
                        component4 = instance.m_menuDialog.Find("MenuEntries/Settings").GetComponent<Button>();
                        component5 = instance.m_menuDialog.Find("MenuEntries/Compendium").GetComponent<Button>();

                        instance.m_firstMenuButton = component3;

                        // AUDIT2 menus-3: Skip Intro (during the intro) and Player list (on a server) are real entries too.
                        if (instance.m_skipButton != null && instance.m_skipButton.gameObject.activeSelf)
                            buttonList.Add(instance.m_skipButton);

                        //Settings
                        buttonList.Add(component4);

                        //Compendium
                        buttonList.Add(component5);

                        //Save
                        if (instance.m_saveButton.interactable)
                            buttonList.Add(instance.m_saveButton);

                        //Player list
                        if (instance.m_playerListButton != null && instance.m_playerListButton.gameObject.activeSelf)
                            buttonList.Add(instance.m_playerListButton);

                        // Valheim 1.0 port (pause-texts-7): Invite Friends, when SetButtonsEnabled shows it.
                        if (instance.m_inviteButton != null && instance.m_inviteButton.gameObject.activeSelf
                            && instance.m_inviteButton.transform.IsChildOf(instance.transform))
                            buttonList.Add(instance.m_inviteButton);

                        //Logout
                        buttonList.Add(component1);

                        //Exit
                        if (component2.gameObject.activeSelf)
                            buttonList.Add(component2);

                        //Close Menu
                        buttonList.Add(component3);
                    }
                    else
                    {
                        component1 = instance.m_menuDialog.Find("MenuEntries/Logout").GetComponent<Button>();
                        component2 = instance.m_menuDialog.Find("MenuEntries/Exit").GetComponent<Button>();
                        component3 = instance.m_menuDialog.Find("MenuEntries/Continue").GetComponent<Button>();
                        component4 = instance.m_menuDialog.Find("MenuEntries/Settings").GetComponent<Button>();

                        instance.m_firstMenuButton = component3;

                        buttonList.Add(component3);

                        if (instance.m_saveButton.interactable)
                            buttonList.Add(instance.m_saveButton);

                        if (instance.m_playerListButton.gameObject.activeSelf)
                            buttonList.Add(instance.m_playerListButton);

                        buttonList.Add(component4);

                        buttonList.Add(component1);

                        if (component2.gameObject.activeSelf)
                            buttonList.Add(component2);
                    }

                    for (int index = 0; index < buttonList.Count; ++index)
                    {
                        Navigation navigation = buttonList[index].navigation with
                        {
                            selectOnUp = index <= 0 ? buttonList[buttonList.Count - 1] : (Selectable) buttonList[index - 1],
                            selectOnDown = index >= buttonList.Count - 1 ? buttonList[0] : (Selectable) buttonList[index + 1],
                            // Unity only honours selectOnUp/Down in Explicit mode (vanilla 1.0 sets it too).
                            mode = Navigation.Mode.Explicit
                        };
                        buttonList[index].navigation = navigation;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Start Menu Navigation ({instance.name}) Error Caught {e.Message}");
                    Debug.LogWarning($"{e.StackTrace}");
                }
            }

            [UsedImplicitly]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instrs = instructions.ToList();

                var counter = 0;

                CodeInstruction LogMessage(CodeInstruction instruction)
                {
                    //Debug.LogWarning($"IL_{counter}: Opcode: {instruction.opcode} Operand: {instruction.operand}");
                    return instruction;
                }

                for (int i = 0; i < instrs.Count; ++i)
                {
                    if (i == 0)
                    {
                        yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_0));
                        counter++;

                        yield return LogMessage(new CodeInstruction(OpCodes.Call,AccessTools.DeclaredMethod(typeof(Menu_UpdateNavigation_Patch), nameof(UpdateNavigation))));
                        counter++;

                        yield return LogMessage(new CodeInstruction(OpCodes.Ret));
                        counter++;

                    }
                }
            }
        }

        [HarmonyPatch(typeof(Menu), nameof(Menu.Start))]
        public static class Menu_Start_Patch
        {
            [UsedImplicitly]
            public static void Postfix(Menu __instance)
            {
                if (__instance.name != "Menu")
                {
                    return;
                }

                var parent = __instance.transform.parent;
                var playerPrefab = __instance.CurrentPlayersPrefab;
                // Valheim 1.0 port: keep the vanilla menu as a hidden donor for the buttons 1.0 added (see PortCarryOver).
                // The vanilla Start just subscribed to the save events; only the Auga menu may react to them.
                PlayerProfile.SavingFinished -= __instance.SaveFinished;
                ZNet.WorldSaveFinished -= __instance.SaveFinished;
                var donor = PortCarryOver.MakeDonor(__instance.gameObject);
                var newMenu = PortCarryOver.InstantiateFilled(Auga.Assets.MenuPrefab, parent, donor).GetComponent<Menu>();
                newMenu.CurrentPlayersPrefab = playerPrefab;
                // m_skipButton was a GameObject in 2023 and is a Button now, so the bundle's value does not load.
                var skipIntro = newMenu.transform.Find("MenuRoot/Menu/MenuEntries/SkipIntro");
                if (skipIntro != null && skipIntro.GetComponent<Button>() != null)
                {
                    var skipButton = skipIntro.GetComponent<Button>();
                    newMenu.m_skipButton = skipButton;
                    // The prefab wires this entry to OnManualSave; switch the baked call off and skip the intro instead.
                    for (var i = 0; i < skipButton.onClick.GetPersistentEventCount(); i++)
                    {
                        skipButton.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
                    }

                    skipButton.onClick.AddListener(newMenu.OnSkip);
                }
                else if (newMenu.m_skipButton == null)
                {
                    newMenu.m_skipButton = __instance.m_skipButton;
                }

                // AUDIT2 menus-2: m_continueButton / m_settingsButton / m_logoutButton / m_quitButton are new in 1.0 and were
                // filled from the hidden donor, so SetButtonsEnabled (DemoMode, console) never reached Auga's entries.
                var menuEntries = newMenu.transform.Find("MenuRoot/Menu/MenuEntries");
                if (menuEntries != null)
                {
                    Button Entry(string path) => menuEntries.Find(path)?.GetComponent<Button>();
                    newMenu.m_continueButton = Entry("DividerMedium/CloseButton") ?? newMenu.m_continueButton;
                    newMenu.m_settingsButton = Entry("Settings") ?? newMenu.m_settingsButton;
                    newMenu.m_logoutButton = Entry("Logout") ?? newMenu.m_logoutButton;
                    newMenu.m_quitButton = Entry("Exit") ?? newMenu.m_quitButton;

                    SetupInviteButton(newMenu, menuEntries);
                }

                SetupBackdrop(newMenu);

                // The cloud-storage warnings only exist in vanilla; move them out of the hidden donor so they can show.
                foreach (var warning in new[] { newMenu.m_cloudStorageWarning, newMenu.m_cloudStorageWarningNextSave })
                {
                    if (warning != null && warning.transform.IsChildOf(donor.transform))
                    {
                        warning.transform.SetParent(newMenu.transform, false);
                        // Their OK buttons are baked to call the *donor's* Menu, whose callback list is empty - the quit /
                        // log out / save they gate was silently dropped. Point them at the live menu.
                        var isNextSave = warning == newMenu.m_cloudStorageWarningNextSave;
                        foreach (var ok in warning.GetComponentsInChildren<Button>(true))
                        {
                            var rebound = false;
                            for (var i = 0; i < ok.onClick.GetPersistentEventCount(); i++)
                            {
                                if (ok.onClick.GetPersistentTarget(i) == __instance)
                                {
                                    ok.onClick.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
                                    rebound = true;
                                }
                            }

                            if (rebound)
                            {
                                if (isNextSave)
                                {
                                    ok.onClick.AddListener(newMenu.OnCloudStorageLowNextSaveWarningOk);
                                }
                                else
                                {
                                    ok.onClick.AddListener(newMenu.OnCloudStorageFullWarningOk);
                                }
                            }
                        }
                    }
                }

                // New in 1.0 and a plain struct, so the reference carry-over does not see it.
                newMenu.m_startScene = __instance.m_startScene;

                // Auga's Menu still names AugaSettings, which is built for the pre-1.0 Settings class. Vanilla settings stay.
                newMenu.m_settingsPrefab = __instance.m_settingsPrefab;
            }

            /// <summary>
            /// Valheim 1.0 port (pause-texts-7): 1.0 added an Invite Friends entry (Menu.InviteFriends, shown by
            /// SetButtonsEnabled only to a hosting player whose platform can invite). AugaMenu has no such entry, so
            /// m_inviteButton pointed into the hidden donor. Clone Auga's own Settings entry, label it with the vanilla
            /// text and wire it to the vanilla handler; AugaInviteRowLayout makes room for it when it is shown.
            /// </summary>
            private static void SetupInviteButton(Menu menu, Transform menuEntries)
            {
                var template = menuEntries.Find("Settings") as RectTransform;
                var playerList = menuEntries.Find("CurrentPlayerList") as RectTransform;
                var compendium = menuEntries.Find("Compendium") as RectTransform;
                if (template == null || playerList == null || compendium == null || menuEntries.Find("InviteFriends") != null)
                {
                    return;
                }

                // The vanilla label's source string ("$menu_..." token) when the game has localized it already.
                var token = "Invite Friends";
                var vanillaLabel = menu.m_inviteButton != null ? menu.m_inviteButton.GetComponentInChildren<TMP_Text>(true) : null;
                if (vanillaLabel != null)
                {
                    token = Localization.instance.textMeshStrings.TryGetValue(vanillaLabel, out var source) ? source : vanillaLabel.text;
                }

                var invite = Object.Instantiate(template, menuEntries, false);
                invite.name = "InviteFriends";
                invite.SetSiblingIndex(playerList.GetSiblingIndex() + 1);
                invite.anchoredPosition = playerList.anchoredPosition;
                invite.gameObject.SetActive(false);

                var label = invite.GetComponentInChildren<TMP_Text>(true);
                if (label != null)
                {
                    label.text = Localization.instance.Localize(token);
                    if (label.text != token)
                    {
                        // So a language change re-localizes it like the prefab's own labels.
                        Localization.instance.textMeshStrings[label] = token;
                    }
                }

                var button = invite.GetComponent<Button>();
                button.onClick = new Button.ButtonClickedEvent();
                button.onClick.AddListener(menu.InviteFriends);
                menu.m_inviteButton = button;

                var layout = invite.gameObject.AddComponent<AugaInviteRowLayout>();
                layout.Step = template.anchoredPosition.y - compendium.anchoredPosition.y;
                foreach (var name in new[] { "SkipIntro", "CurrentPlayerList", "DividerSmall" })
                {
                    if (menuEntries.Find(name) is RectTransform row)
                    {
                        layout.Rows.Add(row);
                        layout.BasePositions.Add(row.anchoredPosition);
                    }
                }
            }

            /// <summary>
            /// Valheim 1.0 port (pause backdrop): vanilla's pause menu dims the game behind it; Auga's only darkening is a
            /// soft spot behind the entries. Add a full-screen dim in vanilla's modal colour (the Menu's own full-screen
            /// dialog backdrop) as MenuRoot's first child, so it sits behind the entries, the confirm dialogs and the
            /// compendium, and blocks clicks to the HUD like vanilla's modal backdrops.
            /// </summary>
            private static void SetupBackdrop(Menu menu)
            {
                var root = menu.m_root;
                if (root == null || root.Find("AugaBackdrop") != null)
                {
                    return;
                }

                // Plain black at vanilla's modal-dim strength. (Copying the cloud-warning Image's colour gave an invisible
                // backdrop in game: that Image is not the dim.)
                var color = new Color(0f, 0f, 0f, 0.45f);

                var backdrop = new GameObject("AugaBackdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                backdrop.layer = root.gameObject.layer;
                var rect = (RectTransform)backdrop.transform;
                rect.SetParent(root, false);
                rect.SetAsFirstSibling();
                // Centred and far larger than any screen: MenuRoot is not a full-screen rect, so stretching to it covered
                // only the menu's own area. The canvas clips the rest.
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(20000f, 20000f);
                rect.position = root.GetComponentInParent<Canvas>() is Canvas canvas ? canvas.transform.position : rect.position;
                var image = backdrop.GetComponent<Image>();
                image.color = color;
                image.raycastTarget = true;
            }
        }

        // Valheim 1.0 port (pause-texts-7): SetButtonsEnabled decides whether Invite Friends shows; move the rows above
        // it up by one entry when it does (AugaMenu positions its entries by hand, it has no layout group).
        [HarmonyPatch(typeof(Menu), nameof(Menu.SetButtonsEnabled))]
        public static class Menu_SetButtonsEnabled_Patch
        {
            [UsedImplicitly]
            public static void Postfix(Menu __instance)
            {
                if (__instance.m_inviteButton == null)
                {
                    return;
                }

                var layout = __instance.m_inviteButton.GetComponent<AugaInviteRowLayout>();
                if (layout != null)
                {
                    layout.Apply();
                }
            }
        }

        [HarmonyPatch(typeof(Menu), nameof(Menu.OnClose))]
        public static class Menu_OnClose_Patch
        {
            [UsedImplicitly]
            public static void Postfix(Menu __instance)
            {
                var compendium = __instance.GetComponent<AugaCompendiumController>();
                if (compendium != null)
                {
                    compendium.HideCompendium();
                }
            }
        }

        [HarmonyPatch(typeof(TextsDialog))]
        public static class TextsDialog_Patch
        {
            [HarmonyPrefix]
            [HarmonyPatch(nameof(TextsDialog.AddActiveEffects))]
            public static bool AddActiveEffects_Prefix()
            {
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(TextsDialog.AddLog))]
            public static bool AddLog_Prefix()
            {
                return false;
            }

            [HarmonyPrefix]
            [HarmonyPatch(nameof(TextsDialog.UpdateTextsList))]
            public static bool UpdateTextsList_Prefix(TextsDialog __instance)
            {
                __instance.m_texts.Clear();

                var filter = __instance.GetComponent<AugaTextsDialogFilter>();
                foreach (var knownText in Player.m_localPlayer.GetKnownTexts())
                {
                    if (filter == null || knownText.Key.Contains(filter.Filter))
                    {
                        var keyText = Localization.instance.Localize(knownText.Key);
                        var separatorIndex = keyText.IndexOf(": ", StringComparison.Ordinal);
                        keyText = separatorIndex >= 0 ? keyText.Substring(separatorIndex + 2) : keyText;
                        __instance.m_texts.Add(new TextsDialog.TextInfo(keyText, Localization.instance.Localize(knownText.Value)));
                    }
                }

                __instance.m_texts.Sort((a, b) => string.Compare(a.m_topic, b.m_topic, StringComparison.CurrentCulture));

                // Valheim 1.0 port (pause-texts-8 / harmony-6): 1.0's player statistics page ($inventory_stats: stats per
                // difficulty, known worlds, kills, items found/crafted) comes from AddStats, which only the vanilla body
                // calls. Add it to the unfiltered list (not the tutorials one), at the top.
                if (filter == null)
                {
                    var before = __instance.m_texts.Count;
                    __instance.AddStats();
                    if (__instance.m_texts.Count > before)
                    {
                        var stats = __instance.m_texts[__instance.m_texts.Count - 1];
                        __instance.m_texts.RemoveAt(__instance.m_texts.Count - 1);
                        __instance.m_texts.Insert(0, stats);
                    }
                }

                return false;
            }

            [HarmonyPostfix]
            [HarmonyPatch(nameof(TextsDialog.ShowText), typeof(TextsDialog.TextInfo))]
            public static void AddLog_Postfix(TextsDialog __instance)
            {
                __instance.m_textArea.text = __instance.m_textArea.text.Replace("color=yellow", $"color={Auga.Colors.Topic}");
            }
        }
    }

    /// <summary>
    /// Valheim 1.0 port (pause-texts-7): AugaMenu places its entries at fixed positions. When the cloned Invite Friends
    /// entry is shown it takes the Player list / Skip Intro slot, and those rows plus the top divider move up one step.
    /// </summary>
    public class AugaInviteRowLayout : MonoBehaviour
    {
        public float Step = 46f;
        public readonly List<RectTransform> Rows = new List<RectTransform>();
        public readonly List<Vector2> BasePositions = new List<Vector2>();

        public void Apply()
        {
            var offset = gameObject.activeSelf ? new Vector2(0f, Step) : Vector2.zero;
            for (var i = 0; i < Rows.Count; i++)
            {
                if (Rows[i] != null)
                {
                    Rows[i].anchoredPosition = BasePositions[i] + offset;
                }
            }
        }
    }
}
