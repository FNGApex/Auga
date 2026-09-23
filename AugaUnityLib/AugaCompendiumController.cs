using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AugaUnity
{
    public class CompendiumItem : MonoBehaviour
    {
        public ItemTooltip ItemTooltip;
        public Image Icon;
        public Text Amount;

        public virtual void SetItem(ItemDrop.ItemData item, string amount = null)
        {
            ItemTooltip.Item = item;
            Icon.sprite = item.GetIcon();
            Amount.enabled = !string.IsNullOrEmpty(amount);
            Amount.text = amount;
        }
    }

    public class BestiaryStatBlock : MonoBehaviour
    {
        public Text LevelLabel;
        public GameObject LevelStar;
        public Text Health;
        public Text Weakness;
        public Text Resistance;
        public Text Immune;

        // Valheim 1.0 port (#153): Humanoid overloads kept for callers; the bestiary now lists every Character (Deer,
        // bosses, most Mistlands creatures are not Humanoids).
        public virtual void SetStats(Humanoid humanoid, int level)
        {
            SetStats((Character)humanoid, level);
        }

        public virtual void SetStats(Character character, int level)
        {
            LevelLabel.text = level == 1 ? "$baselevel" : (level - 1).ToString();
            LevelStar.SetActive(level >= 2);
            Health.text = GetMaxHealth(character, level).ToString("0");

            // Valheim 1.0 port (auga-lib-6): 1.0 added SlightlyWeak / SlightlyResistant, list them with their tier.
            var allWeak = GetDamageTypes(character, HitData.DamageModifier.SlightlyWeak)
                .Concat(GetDamageTypes(character, HitData.DamageModifier.Weak))
                .Concat(GetDamageTypes(character, HitData.DamageModifier.VeryWeak)).ToArray();
            var allResist = GetDamageTypes(character, HitData.DamageModifier.SlightlyResistant)
                .Concat(GetDamageTypes(character, HitData.DamageModifier.Resistant))
                .Concat(GetDamageTypes(character, HitData.DamageModifier.VeryResistant)).ToArray();
            var immune = GetDamageTypes(character, HitData.DamageModifier.Immune);

            Weakness.text = !allWeak.Any() ? "-" : string.Join("\n", allWeak.Select(x => $"$inventory_{x.ToString().ToLowerInvariant()}"));
            Resistance.text = !allResist.Any() ? "-" : string.Join("\n", allResist.Select(x => $"$inventory_{x.ToString().ToLowerInvariant()}"));
            Immune.text = !immune.Any() ? "-" : string.Join("\n", immune.Select(x => $"$inventory_{x.ToString().ToLowerInvariant()}"));
        }

        public static float GetMaxHealth(Humanoid humanoid, int level)
        {
            return GetMaxHealth((Character)humanoid, level);
        }

        public static float GetMaxHealth(Character character, int level)
        {
            return character.m_health * level;
        }

        public static List<HitData.DamageType> GetDamageTypes(Humanoid humanoid, HitData.DamageModifier modifier)
        {
            return GetDamageTypes((Character)humanoid, modifier);
        }

        public static List<HitData.DamageType> GetDamageTypes(Character character, HitData.DamageModifier modifier)
        {
            var result = new List<HitData.DamageType>();
            foreach (HitData.DamageType damageType in Enum.GetValues(typeof(HitData.DamageType)))
            {
                // Valheim 1.0 port (auga-lib-6): only the ten real damage types; skip 1.0's Damage / NonPlayer and the
                // Physical / Elemental masks (no $inventory_ token, NonPlayer would render raw).
                if (damageType == HitData.DamageType.Damage || damageType == HitData.DamageType.NonPlayer ||
                    damageType == HitData.DamageType.Physical || damageType == HitData.DamageType.Elemental)
                {
                    continue;
                }

                var mod = character.m_damageModifiers.GetModifier(damageType);
                if (mod == modifier)
                {
                    result.Add(damageType);
                }
            }

            return result;
        }
    }

    public class AugaCompendiumController : MonoBehaviour
    {
        public TabHandler TabController;
        public TextsDialog Compendium;
        public TextsDialog LoreCompendium;
        public GameObject BestiaryContent;
        public RectTransform BestiaryList;
        public GameObject BestiaryListElementPrefab;
        public Text BestiaryName;
        public Text BestiaryDescription;
        public CompendiumItem CompendiumItemPrefab;
        public RectTransform DropsContainer;
        public List<BestiaryStatBlock> StatBlocks;

        protected readonly List<KeyValuePair<string, GameObject>> _bestiaryItems = new List<KeyValuePair<string, GameObject>>();
        protected int _selectedBestiaryIndex = -1;
        // Valheim 1.0 port (#153): Character, not Humanoid - Deer, bosses and most Mistlands creatures are plain Characters.
        protected Dictionary<string, Character> _trophyToMonsterCache;

        protected virtual void SetupTrophyToMonsterCache()
        {
            if (ZNetScene.instance == null || ZNetScene.instance.m_namedPrefabs == null)
                return;

            if (_trophyToMonsterCache != null)
                return;

            if (!ZNetScene.instance.m_namedPrefabs.Values.Any())
                return;

            _trophyToMonsterCache = new Dictionary<string, Character>();

            foreach (var prefab in ZNetScene.instance.m_namedPrefabs.Values)
            {
                // Valheim 1.0 port (#44): modded / seasonal entries can leave empty prefab slots.
                if (prefab == null)
                    continue;

                var character = prefab.GetComponent<Character>();
                var characterDrop = prefab.GetComponent<CharacterDrop>();
                if (characterDrop == null || character == null || character is Player || characterDrop.m_drops == null || !characterDrop.m_drops.Any())
                {
                    continue;
                }

                foreach (var drop in characterDrop.m_drops)
                {
                    // Valheim 1.0 port (#44): a CharacterDrop entry with an empty prefab slot NRE'd here.
                    if (drop == null || drop.m_prefab == null)
                        continue;

                    var itemDrop = drop.m_prefab.GetComponent<ItemDrop>();
                    if (itemDrop == null || itemDrop.m_itemData?.m_shared == null || itemDrop.m_itemData.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Trophy)
                        continue;

                    // Valheim 1.0 port (#153): no `break` after the first trophy, so every trophy a creature drops is mapped.
                    // Several creatures can drop the same trophy (Draugr / Draugr_Ranged...); keep the first one found,
                    // but prefer the creature the trophy is named after (TrophyDraugr -> Draugr) so the pick is stable.
                    var trophyName = drop.m_prefab.name;
                    if (!_trophyToMonsterCache.TryGetValue(trophyName, out var existing) ||
                        (!IsNamesake(trophyName, existing) && IsNamesake(trophyName, character)))
                    {
                        _trophyToMonsterCache[trophyName] = character;
                    }
                }
            }
        }

        private static bool IsNamesake(string trophyName, Character character)
        {
            return character != null && string.Equals(trophyName, "Trophy" + character.gameObject.name, StringComparison.OrdinalIgnoreCase);
        }

        public virtual void ShowCompendium()
        {
            gameObject.SetActive(true);
            // Valheim 1.0 port (pause-texts-14): m_tabKeyInput is new in 1.0 and defaults to true, so Tab cycled these
            // tabs; pre-1.0 Tab only worked with m_gamepadInput (off here). Auga.PortTabHandlers does this for all prefabs.
            TabController.m_tabKeyInput = TabController.m_gamepadInput;
            // Valheim 1.0 port (pause-texts-12): TabHandler.Init now runs in the deferred Start and skips its default-tab
            // pass once SetActiveTab has been called, and SetActiveTab(0) returns early while m_selected is already 0,
            // so the first open showed no tab state. Force the selection.
            TabController.SetActiveTab(0, forceSelect: true);
            Compendium.Setup(Player.m_localPlayer);
            LoreCompendium.Setup(Player.m_localPlayer);
            SetupBestiary();
            Menu.instance.m_settingsInstance = gameObject;
        }

        public virtual void HideCompendium()
        {
            Menu.instance.m_settingsInstance = null;
            gameObject.SetActive(false);
        }

        public virtual void Update()
        {
            if (ZInput.GetKeyDown(KeyCode.Escape) || ZInput.GetButtonDown("JoyMenu"))
            {
                HideCompendium();
            }
        }

        // Valheim 1.0 port (#186): the creature list's ScrollRect (LeftColumn) has no Graphic, so the mouse wheel only
        // scrolled while over a list entry. A transparent raycast-target Image makes the whole area scrollable.
        protected virtual void EnsureBestiaryListRaycastTarget()
        {
            if (BestiaryList == null)
                return;

            var scrollRect = BestiaryList.GetComponentInParent<ScrollRect>(true);
            if (scrollRect == null || scrollRect.GetComponent<Graphic>() != null)
                return;

            var image = scrollRect.gameObject.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;
            var canvasRenderer = scrollRect.GetComponent<CanvasRenderer>();
            if (canvasRenderer != null)
                canvasRenderer.cullTransparentMesh = false;
        }

        public virtual void SetupBestiary()
        {
            EnsureBestiaryListRaycastTarget();

            SetupTrophyToMonsterCache();
            if (_trophyToMonsterCache == null)
            {
                return;
            }

            var player = Player.m_localPlayer;
            if (player == null)
            {
                BestiaryContent.SetActive(false);
                return;
            }

            foreach (var bestiaryItem in _bestiaryItems)
            {
                Destroy(bestiaryItem.Value);
            }
            _bestiaryItems.Clear();

            var trophies = player.GetTrophies();
            var tempList = new List<Tuple<int, string, GameObject>>();

            for (var index = 0; index < trophies.Count; ++index)
            {
                var trophyName = trophies[index];
                var trophyItemPrefab = ObjectDB.instance.GetItemPrefab(trophyName);
                if (trophyItemPrefab == null)
                {
                    continue;
                }

                var trophyItem = trophyItemPrefab.GetComponent<ItemDrop>();
                if (trophyItem == null)
                {
                    continue;
                }

                var position2d = trophyItem.m_itemData.m_shared.m_trophyPos;
                var position = position2d.y * 10 + position2d.x;

                // Valheim 1.0 port (#153): vanilla's trophy screen lists every trophy the player has collected. Trophies no
                // creature's CharacterDrop names (seasonal, modded) used to vanish; list them under the trophy's name.
                _trophyToMonsterCache.TryGetValue(trophyName, out var creaturePrefab);
                var listItem = Instantiate(BestiaryListElementPrefab, BestiaryList);
                listItem.SetActive(true);
                var t = listItem.transform;
                var entryName = creaturePrefab != null ? Localization.instance.Localize(creaturePrefab.m_name) : GetTrophyDisplayName(trophyItem);
                t.Find("name").GetComponent<TMP_Text>().text = entryName;
                t.Find("icon").GetComponent<Image>().sprite = trophyItem.m_itemData.GetIcon();
                tempList.Add(new Tuple<int, string, GameObject>(position, trophyName, listItem));
            }

            var orderedList = tempList.OrderBy(x => x.Item1).ToList();
            for (var index = 0; index < orderedList.Count; index++)
            {
                var entry = orderedList[index];
                var listItem = entry.Item3;
                listItem.transform.SetSiblingIndex(index);
                var i = index;
                listItem.GetComponent<Button>().onClick.AddListener(() => OnBestiaryItemClicked(i));
                _bestiaryItems.Add(new KeyValuePair<string, GameObject>(entry.Item2, listItem));
            }

            // Valheim 1.0 port (#153): was set before the list was rebuilt, from the previous open's count.
            BestiaryContent.SetActive(_bestiaryItems.Count > 0);
            OnBestiaryItemClicked(0);
        }

        // Same as vanilla's trophy screen: the item name without its " trophy" suffix.
        private static string GetTrophyDisplayName(ItemDrop trophyItem)
        {
            var name = Localization.instance.Localize(trophyItem.m_itemData.m_shared.m_name);
            return name.EndsWith(" trophy", StringComparison.OrdinalIgnoreCase) ? name.Substring(0, name.Length - 7) : name;
        }

        private void OnBestiaryItemClicked(int index)
        {
            _selectedBestiaryIndex = index;

            for (var i = 0; i < _bestiaryItems.Count; i++)
            {
                var bestiaryItem = _bestiaryItems[i];
                bestiaryItem.Value.transform.Find("selected").gameObject.SetActive(i == _selectedBestiaryIndex);
            }

            if (_selectedBestiaryIndex < 0 || _selectedBestiaryIndex >= _bestiaryItems.Count)
            {
                return;
            }

            var player = Player.m_localPlayer;
            if (player == null)
            {
                return;
            }

            _selectedBestiaryIndex = Mathf.Clamp(_selectedBestiaryIndex, 0, _bestiaryItems.Count - 1);
            var selectedEntry = _bestiaryItems[_selectedBestiaryIndex];
            var trophy = selectedEntry.Key;
            _trophyToMonsterCache.TryGetValue(trophy, out var creaturePrefab);

            var trophyPrefab = ObjectDB.instance.GetItemPrefab(trophy);
            var trophyItem = trophyPrefab != null ? trophyPrefab.GetComponent<ItemDrop>() : null;
            if (trophyItem == null)
            {
                return;
            }

            BestiaryContent.SetActive(true);
            // Valheim 1.0 port (#153): a trophy without a known creature shows its name and lore only; the drops and stat
            // sections (everything below the description) are hidden for it.
            BestiaryName.text = creaturePrefab != null ? creaturePrefab.m_name : GetTrophyDisplayName(trophyItem);
            BestiaryDescription.text = trophyItem.m_itemData.m_shared.m_name + "_lore";
            SetCreatureSectionsActive(creaturePrefab != null);

            foreach (Transform child in DropsContainer)
            {
                Destroy(child.gameObject);
            }

            if (creaturePrefab != null)
            {
                var characterDrop = creaturePrefab.GetComponent<CharacterDrop>();
                if (characterDrop != null && characterDrop.m_drops != null)
                {
                    foreach (var drop in characterDrop.m_drops)
                    {
                        // Valheim 1.0 port (#44): skip empty or non-item drop slots instead of throwing.
                        var dropItem = drop?.m_prefab != null ? drop.m_prefab.GetComponent<ItemDrop>() : null;
                        if (dropItem == null)
                            continue;

                        var dropElement = Instantiate(CompendiumItemPrefab, DropsContainer);
                        var amountText = (drop.m_amountMin == drop.m_amountMax ? $"{drop.m_amountMin}" : $"{drop.m_amountMin}-{drop.m_amountMax}") + $" ({Mathf.CeilToInt(drop.m_chance * 100)}%)";
                        dropElement.SetItem(dropItem.m_itemData, amountText);
                    }
                }

                for (var i = 0; i < StatBlocks.Count; i++)
                {
                    var statBlock = StatBlocks[i];
                    statBlock.SetStats(creaturePrefab, i + 1);
                }
            }

            Localization.instance.Localize(BestiaryContent.transform);
        }

        private void SetCreatureSectionsActive(bool active)
        {
            var content = BestiaryDescription.transform.parent;
            var descriptionIndex = BestiaryDescription.transform.GetSiblingIndex();
            for (var i = descriptionIndex + 1; i < content.childCount; i++)
            {
                content.GetChild(i).gameObject.SetActive(active);
            }
        }
    }

    public class AugaTextsDialogFilter : MonoBehaviour
    {
        public string Filter;
    }
}
