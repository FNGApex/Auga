using System.Globalization;
using AugaUnity;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Auga
{
    [HarmonyPatch]
    public static class InventoryPanel_Patches
    {
        public static AugaCraftingPanel CraftingPanel;
        public static Transform TopRowInventory;
        public static Transform MainRowsInventory;

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetActiveGroup), typeof(int), typeof(bool))]
        public static class InventoryGui_SetActiveGroup_Patch
        {
            // Info (2) and crafting (3) both live in Auga's right panel; vanilla just switched it off for one of them.
            public static void Postfix(InventoryGui __instance)
            {
                if (__instance.m_uiGroups != null && __instance.m_uiGroups.Length > 3 && __instance.m_uiGroups[2] != null)
                {
                    __instance.m_uiGroups[2].SetActive(__instance.m_activeGroup >= 2);
                }
            }
        }

        /// <summary>
        /// Upstream issues #62 / #228 (reproduced on 1.0 with a 6-row inventory): vanilla keeps the chest panel inside the
        /// player panel, so it follows the panel when 1.0's SetInventorySize grows it for extra rows. Auga lifts the chest
        /// out as a sibling (see the Awake postfix), so it stayed put and covered the new rows. Keep the original gap.
        /// </summary>
        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetInventorySize))]
        public static class InventoryGui_SetInventorySize_Patch
        {
            private static float _containerBaseY = float.NaN;
            private static float _playerBaseHeight;

            public static void Postfix(InventoryGui __instance)
            {
                var container = __instance.m_container;
                var player = __instance.m_player;
                if (container == null || player == null || container.parent != player.parent)
                {
                    return;
                }

                if (float.IsNaN(_containerBaseY))
                {
                    _containerBaseY = container.anchoredPosition.y;
                    _playerBaseHeight = __instance.m_playerHeight;
                }

                var extra = player.sizeDelta.y - _playerBaseHeight;
                container.anchoredPosition = new Vector2(container.anchoredPosition.x, _containerBaseY - extra);
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
        public static class InventoryGui_Awake_Patch
        {
            /// <summary>
            /// Valheim 1.0 port (pause-texts-10): 1.0's Achievements panel (root/Achievements) is only reachable from the
            /// Achievements button on vanilla's root/Info strip, which Auga hides. Add an icon button at the end of Auga's
            /// player-panel tab row (a plain button like the PvP one, not a tab) with the vanilla icon and tooltip.
            /// </summary>
            private static void AddAchievementsButton(InventoryGui gui, Transform rightPanel)
            {
                var tabs = rightPanel.Find("DefaultContent/TabButtonContainer/Tabs");
                var template = tabs != null ? tabs.Find("TabButton_MessageLog") : null;
                var vanilla = gui.transform.Find("root/Info_VanillaDonor/Achievements") ?? gui.transform.Find("root/Info/Achievements");
                if (template == null || gui.m_achievementsPanel == null)
                {
                    return;
                }

                var button = Object.Instantiate(template, tabs, false);
                button.name = "TabButton_Achievements";
                button.SetAsLastSibling();

                var vanillaIcon = vanilla != null ? vanilla.Find("Image")?.GetComponent<Image>() : null;
                var icon = button.Find("Icon")?.GetComponent<Image>();
                if (icon != null && vanillaIcon != null && vanillaIcon.sprite != null)
                {
                    icon.sprite = vanillaIcon.sprite;
                    icon.preserveAspect = true;
                }

                var tooltip = button.GetComponent<UITooltip>();
                var vanillaTooltip = vanilla != null ? vanilla.GetComponent<UITooltip>() : null;
                if (tooltip != null)
                {
                    tooltip.m_text = vanillaTooltip != null && !string.IsNullOrEmpty(vanillaTooltip.m_text) ? vanillaTooltip.m_text : "$inventory_achievements";
                    if (vanillaTooltip != null && !string.IsNullOrEmpty(vanillaTooltip.m_topic))
                    {
                        tooltip.m_topic = vanillaTooltip.m_topic;
                    }
                }

                var click = button.GetComponent<Button>();
                click.onClick = new Button.ButtonClickedEvent();
                click.onClick.AddListener(gui.OnOpenAchievements);
            }

            [HarmonyPriority(Priority.First)]
            public static void Postfix(InventoryGui __instance)
            {
                AddItemIconMaterial.IconMaterial = __instance.m_dragItemPrefab.transform.Find("icon").GetComponent<Image>().material;

                __instance.m_playerGrid.m_onSelected = null;
                __instance.m_playerGrid.m_onRightClick = null;
                __instance.m_containerGrid.m_onSelected = null;
                __instance.m_containerGrid.m_onRightClick = null;

                // Valheim 1.0 port: the chest panel now lives inside the player panel (root/Player/Container).
                // Auga lays the two out as siblings, so lift it back up before root/Player is replaced.
                var nestedContainer = __instance.transform.Find("root/Player/Container");
                if (nestedContainer != null && __instance.transform.Find("root/Container") == null)
                {
                    nestedContainer.SetParent(__instance.transform.Find("root"), false);
                    // Right after the player panel, not last: appended at the end it drew over (and took clicks from) the split,
                    // trophies and texts dialogs, which in vanilla come after the panel that contains the chest.
                    nestedContainer.SetSiblingIndex(__instance.transform.Find("root/Player").GetSiblingIndex() + 1);
                }

                var playerInventory = __instance.Replace("root/Player", Auga.Assets.InventoryScreen, "root/Player");
                __instance.m_player = playerInventory.RectTransform();
                // Valheim 1.0 port: Awake cached the *vanilla* panel height (287) and row pitch for the new SetInventorySize
                // (extra inventory rows); applied to Auga's 332 high panel that cut the grid down to ~2.5 visible rows.
                __instance.m_playerHeight = __instance.m_player.sizeDelta.y;
                var mainGridLayout = playerInventory.Find("PlayerGrid/Main/Grid")?.GetComponent<GridLayoutGroup>();
                __instance.m_invGridHeight = mainGridLayout != null ? mainGridLayout.cellSize.y + mainGridLayout.spacing.y : 80f;
                __instance.m_playerGrid = playerInventory.Find("PlayerGrid").GetComponent<InventoryGrid>();
                __instance.m_playerGrid.m_onSelected += __instance.OnSelectedItem;
                __instance.m_playerGrid.m_onRightClick += __instance.OnRightClickItem;
                // Valheim 1.0 port: the rest of what InventoryGui.Awake wires per grid. CanDropDragOntoItem is
                // invoked without a null check inside InventoryGrid.UpdateGui.
                __instance.m_playerGrid.m_onReleased += __instance.OnReleasedItem;
                __instance.m_playerGrid.m_onEnter += __instance.OnEnterElement;
                __instance.m_playerGrid.OnSetTouchSelection += __instance.SetTouchSelection;
                __instance.m_playerGrid.CanDropDragOntoItem += __instance.CanDropDragOntoItem;
                __instance.m_playerGrid.OnMoveToLowerInventoryGrid += __instance.MoveToLowerInventoryGrid;
                __instance.m_weight = playerInventory.Find("Weight/Text").GetComponent<TMP_Text>();
                __instance.m_armor = playerInventory.Find("Armor/Text").GetComponent<TMP_Text>();

                var containerInventory = __instance.Replace("root/Container", Auga.Assets.InventoryScreen, "root/Container");
                __instance.m_container = containerInventory.RectTransform();
                __instance.m_containerName = containerInventory.Find("ContainerHeader/Name").GetComponent<TMP_Text>();
                __instance.m_containerGrid = containerInventory.Find("ContainerGrid").GetComponent<InventoryGrid>();
                __instance.m_containerGrid.m_onSelected += __instance.OnSelectedItem;
                __instance.m_containerGrid.m_onRightClick += __instance.OnRightClickItem;
                __instance.m_containerGrid.m_onReleased += __instance.OnReleasedItem;
                __instance.m_containerGrid.m_onEnter += __instance.OnEnterElement;
                __instance.m_containerGrid.OnSetTouchSelection += __instance.SetTouchSelection;
                __instance.m_containerGrid.CanDropDragOntoItem += __instance.CanDropDragOntoItem;
                __instance.m_containerGrid.OnMoveToUpperInventoryGrid += __instance.MoveToUpperInventoryGrid;
                __instance.m_containerWeight = containerInventory.Find("Weight/Text").GetComponent<TMP_Text>();
                __instance.m_takeAllButton = containerInventory.Find("TakeAll").GetComponent<ColorButtonText>();
                __instance.m_takeAllButton.onClick.AddListener(__instance.OnTakeAll);
                __instance.m_stackAllButton = containerInventory.Find("StackAll").GetComponent<ColorButtonText>();
                __instance.m_stackAllButton.onClick.AddListener(__instance.OnStackAll);

                var oldCraftingPanel = __instance.transform.Find("root/Crafting");
                var craftingPanelSiblingIndex = oldCraftingPanel.GetSiblingIndex();
                // Valheim 1.0 port: kept hidden instead of destroyed - 1.0 has fields pointing into this panel that Auga doesn't replace.
                PortCarryOver.MakeDonor(oldCraftingPanel.gameObject);

                var variantDialog = __instance.Replace("root/VariantDialog", Auga.Assets.InventoryScreen, "root/DummyObjects/DummyVariantDialog");
                __instance.m_variantDialog = variantDialog.GetComponent<VariantDialog>();

                var skillsDialog = __instance.Replace("root/Skills", Auga.Assets.InventoryScreen, "root/RightPanel/TabContent/TabContent_Skills");
                __instance.m_skillsDialog = skillsDialog.GetComponent<SkillsDialog>();
                var dummyContainer = new GameObject("DummyDialogs", typeof(RectTransform));
                dummyContainer.transform.SetParent(skillsDialog.parent);
                variantDialog.SetParent(dummyContainer.transform);
                skillsDialog.SetParent(dummyContainer.transform);
                dummyContainer.SetActive(false);

                var rightPanel = Object.Instantiate(Auga.Assets.InventoryScreen.transform.Find("root/RightPanel"), containerInventory.parent, false);
                rightPanel.gameObject.name = "RightPanel";
                rightPanel.SetSiblingIndex(craftingPanelSiblingIndex);
                CraftingPanel = rightPanel.GetComponentInChildren<AugaCraftingPanel>(true);
                CraftingPanel.SetMultiCraftEnabled(Auga.HasMultiCraft);
                __instance.m_playerName = rightPanel.Find("DefaultContent/TitleContainer/PlayerPanelTitle").GetComponent<TMP_Text>();
                __instance.m_pvp = rightPanel.Find("TabContent/TabContent_PVP/Dummy/PVPToggle").GetComponent<Toggle>();
                __instance.m_recipeElementPrefab = CraftingPanel.RecipeItemPrefab;
                __instance.m_recipeListRoot = CraftingPanel.RecipeList;
                __instance.m_recipeListScroll = CraftingPanel.RecipeListScrollbar;
                __instance.m_recipeEnsureVisible = CraftingPanel.RecipeListEnsureVisible;
                __instance.m_recipeListSpace = 34;
                __instance.m_craftingStationName = CraftingPanel.WorkbenchName;
                __instance.m_craftingStationIcon = CraftingPanel.WorkbenchIcon;
                __instance.m_craftingStationLevelRoot = CraftingPanel.WorkbenchLevelRoot;
                __instance.m_craftingStationLevel = CraftingPanel.WorkbenchLevel;
                __instance.m_craftButton = CraftingPanel.CraftButton;
                __instance.m_craftButton.onClick.AddListener(__instance.OnCraftPressed);
                __instance.m_craftCancelButton = CraftingPanel.CraftCancelButton;
                __instance.m_craftCancelButton.onClick.AddListener(__instance.OnCraftCancelPressed);
                __instance.m_craftProgressPanel = CraftingPanel.CraftProgressPanel;
                __instance.m_variantButton = CraftingPanel.VariantButton;
                __instance.m_variantButton.onClick.AddListener(__instance.OnShowVariantSelection);
                __instance.m_variantDialog = CraftingPanel.VariantDialog;
                __instance.m_variantDialog.m_selected += __instance.OnVariantSelected;
                __instance.m_repairButton = CraftingPanel.DefaultRepairButton;
                __instance.m_repairButtonGlow = CraftingPanel.DefaultRepairGlow;
                __instance.m_repairPanel = CraftingPanel.DefaultRepairButton.transform;
                __instance.m_repairButton.onClick.AddListener(__instance.OnRepairPressed);

                __instance.m_recipeIcon = CraftingPanel.DummyIcon;
                __instance.m_recipeName = CraftingPanel.DummyName;
                __instance.m_recipeDecription = CraftingPanel.DummyDescription;
                __instance.m_repairPanelSelection = CraftingPanel.DummyRepairPanelSelection;
                __instance.m_tabCraft = CraftingPanel.DummyCraftTabButton;
                __instance.m_tabUpgrade = CraftingPanel.DummyUpgradeTabButton;
                __instance.m_craftProgressBar = CraftingPanel.DummyCraftProgressBar;
                __instance.m_qualityPanel = CraftingPanel.DummyQualityPanel;
                __instance.m_minStationLevelIcon = CraftingPanel.DummyMinStationLevelIcon;
                CraftingPanel.Initialize(__instance);

                // Valheim 1.0 port: hidden, not destroyed (the new Achievements button lives here).
                PortCarryOver.MakeDonor(__instance.transform.Find("root/Info").gameObject);
                AddAchievementsButton(__instance, rightPanel);
                /*var info = Object.Instantiate(Auga.Assets.InventoryScreen.transform.Find("root/Info"), containerInventory.parent, false);
                info.SetSiblingIndex(3);
                info.gameObject.name = "Info";
                info.Find("Texts").GetComponent<Button>().onClick.AddListener(__instance.OnOpenTexts);
                info.Find("Trophies").GetComponent<Button>().onClick.AddListener(__instance.OnOpenTrophies);*/

                // AUDIT2 inventory-11: Auga's own split panel gets 1.0's SplitDialog component at runtime (PortSplitDialog);
                // vanilla's wood panel becomes a hidden donor.
                var vanillaSplit = __instance.transform.Find("root/SplitDialog");
                var augaSplitTemplate = Auga.Assets.InventoryScreen.transform.Find("root/SplitDialog");
                if (vanillaSplit != null && augaSplitTemplate != null)
                {
                    var augaSplit = Object.Instantiate(augaSplitTemplate, vanillaSplit.parent, false);
                    augaSplit.name = "SplitDialog";
                    augaSplit.SetSiblingIndex(vanillaSplit.GetSiblingIndex());
                    var splitDialog = PortSplitDialog.Setup(augaSplit);
                    if (splitDialog != null)
                    {
                        PortCarryOver.MakeDonor(vanillaSplit.gameObject);
                        __instance.m_splitDialog = splitDialog;
                    }
                    else
                    {
                        Object.Destroy(augaSplit.gameObject);
                    }
                }

                // Valheim 1.0 port: vanilla addresses the groups by index - [2] info (skills, texts, trophies),
                // [3] crafting - and finds a group's index with Array.IndexOf, so the two slots need distinct
                // handlers. Auga shows both inside its one right panel: slot 3 gets an empty stand-in, and
                // InventoryGui_SetActiveGroup_Patch keeps the real panel active for either index.
                var craftingGroupStandIn = new GameObject("CraftingGroup_PortStandIn", typeof(RectTransform));
                craftingGroupStandIn.transform.SetParent(rightPanel, false);
                __instance.m_uiGroups = new [] {
                    containerInventory.GetComponent<UIGroupHandler>(),
                    playerInventory.GetComponent<UIGroupHandler>(),
                    rightPanel.GetComponent<UIGroupHandler>(),
                    craftingGroupStandIn.AddComponent<UIGroupHandler>()
                };

                // inventory-6: new in 1.0, pointed into the replaced player panel.
                var touchSplitAnchor = playerInventory.Find("TouchSplitAnchor");
                __instance.m_touchSplitAnchor = touchSplitAnchor != null ? touchSplitAnchor : playerInventory;

                var animator = __instance.GetComponent<Animator>();
                var newAnimator = Auga.Assets.InventoryScreen.GetComponent<Animator>();
                animator.runtimeAnimatorController = newAnimator.runtimeAnimatorController;
                animator.Rebind();

                var standardDivider = playerInventory.Find("StandardDivider");
                var trashDivider = playerInventory.Find("TrashDivider");
                standardDivider.gameObject.SetActive(!Auga.UseAugaTrash.Value);
                trashDivider.gameObject.SetActive(Auga.UseAugaTrash.Value);

                Localization.instance.Localize(__instance.transform);
            }
        }

        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
        public static class InventoryGrid_UpdateGui_Patch
        {
            public static void Postfix(InventoryGrid __instance)
            {
                if (__instance.name == "PlayerGrid")
                {
                    if (TopRowInventory == null)
                    {
                        TopRowInventory = __instance.transform.Find("Top");
                        MainRowsInventory = __instance.transform.Find("Main/Grid");
                    }
                }

                //Vector2 startPos = new Vector2(__instance.RectTransform().rect.width / 2f, 0.0f) - new Vector2(__instance.GetWidgetSize().x, 0.0f) * 0.5f;
                foreach (var element in __instance.m_elements)
                {
                    var itemTooltip = element.gameObject.GetComponent<ItemTooltip>();

                    var item = __instance.m_inventory.GetItemAt(element.Position.x, element.Position.y);

                    if (itemTooltip != null && !element.m_used)
                    {
                        itemTooltip.Item = null;
                    }

                    if (element.m_used && itemTooltip != null)
                    {
                        itemTooltip.Item = item;
                    }

                    if (__instance.name == "PlayerGrid")
                    {
                        if (element.Position.y == 0)
                        {
                            element.gameObject.transform.SetParent(TopRowInventory);
                        }
                        else
                        {
                            element.gameObject.transform.SetParent(MainRowsInventory);
                            //Vector2 currentPosition = new Vector3(element.Position.x * (__instance.m_elementSpace), (element.Position.y * -__instance.m_elementSpace) - 26);
                            //element.gameObject.RectTransform().anchoredPosition = startPos + currentPosition;
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Show))]
        public static class InventoryGui_Show_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                var player = Player.m_localPlayer;
                if (player != null)
                {
                    __instance.UpdateContainer(player);
                }
            }
        }

        //CreateItemTooltip
        [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.CreateItemTooltip))]
        public static class InventoryGrid_CreateItemTooltip_Patch
        {
            public static bool Prefix(InventoryGrid __instance, ItemDrop.ItemData item, UITooltip tooltip)
            {
                var itemTooltip = tooltip.GetComponent<ItemTooltip>();
                if (itemTooltip != null)
                {
                    itemTooltip.Item = item;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetRecipe))]
        public static class InventoryGui_SetRecipe_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                if (CraftingPanel != null)
                {
                    CraftingPanel.SetRecipe(__instance.m_selectedRecipe.Recipe, __instance.m_selectedRecipe.ItemData, __instance.m_selectedVariant);
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateRecipe))]
        public static class InventoryGui_UpdateRecipe_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                if (CraftingPanel != null)
                {
                    CraftingPanel.OnUpdateRecipe(__instance);
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnVariantSelected))]
        public static class InventoryGui_OnVariantSelected_Patch
        {
            public static void Postfix(InventoryGui __instance)
            {
                if (CraftingPanel != null)
                {
                    CraftingPanel.SetRecipe(__instance.m_selectedRecipe.Recipe, __instance.m_selectedRecipe.ItemData, __instance.m_selectedVariant);
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirementList))]
        public static class InventoryGui_SetupRequirementList_Patch
        {
            // Valheim 1.0 port: "amount" is the new multi-craft multiplier; m_reqList is what vanilla decided to show.
            public static void Postfix(InventoryGui __instance, int quality, Player player, bool allowedQuality, int amount)
            {
                if (CraftingPanel != null)
                {
                    CraftingPanel.PostSetupRequirementList(__instance.m_selectedRecipe.Recipe, __instance.m_selectedRecipe.ItemData, quality, player, allowedQuality, amount, __instance.m_reqList);
                }
            }
        }

        [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.UpdateCharacterStats))]
        public static class InventoryGui_UpdateCharacterStats_Patch
        {
            public static bool Prefix(InventoryGui __instance, Player player)
            {
                __instance.m_playerName.text = Game.instance.GetPlayerProfile().GetName();
                __instance.m_armor.text = player.GetBodyArmor().ToString(CultureInfo.InvariantCulture);
                return false;
            }
        }

        [HarmonyPatch(typeof(VariantDialog), nameof(VariantDialog.Setup))]
        public static class VariantDialog_Setup_Patch
        {
            public static void Postfix(VariantDialog __instance)
            {
                for (var index = 0; index < __instance.m_elements.Count; index++)
                {
                    var variantElement = __instance.m_elements[index];
                    var selected = index == InventoryGui.instance.m_selectedVariant;

                    var selectedObject = variantElement.transform.Find("selected");
                    if (selectedObject != null)
                    {
                        selectedObject.gameObject.SetActive(selected);
                    }
                }
            }
        }
    }
}
