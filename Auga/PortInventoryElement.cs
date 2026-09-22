using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    /// <summary>
    /// Valheim 1.0 port. InventoryGrid used to find the parts of a slot by child name ("icon", "amount",
    /// "durability"...); 1.0 reads them from an InventoryElement component on the slot prefab instead.
    /// Auga's slot prefab predates that component but still uses the old child names, so the component is
    /// added to the prefab on first use and wired by those names. Parts Auga's slot doesn't have get a
    /// hidden stand-in so vanilla's unconditional accesses don't throw.
    /// </summary>
    [HarmonyPatch(typeof(InventoryGrid), nameof(InventoryGrid.UpdateGui))]
    public static class PortInventoryElement
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        public static void Prefix(InventoryGrid __instance)
        {
            var prefab = __instance.m_elementPrefab;
            if (prefab != null && prefab.GetComponent<InventoryElement>() == null)
            {
                Setup(prefab);
            }
        }

        private static void Setup(GameObject prefab)
        {
            var root = prefab.transform;
            var element = prefab.AddComponent<InventoryElement>();

            element.m_button = prefab.GetComponent<Button>() ?? prefab.AddComponent<Button>();
            // 1.0 also wires drag callbacks on every slot through a UIDragHandler (a class that did not exist in 2023).
            if (prefab.GetComponentInChildren<UIDragHandler>(true) == null)
            {
                prefab.AddComponent<UIDragHandler>();
            }
            // AUDIT2 inventory-12: vanilla's slot has a 70x70 'touchrect' hit area and a 'dropFocus' image that 1.0 tints
            // while a split stack is held on touch layouts. Auga's slot has neither: build both.
            element.m_touchRect = FindDeep(root, "touchrect") as RectTransform ?? TouchRect(root as RectTransform);
            element.m_icon = Part<Image>(root, "icon");
            element.m_amount = Part<TextMeshProUGUI>(root, "amount");
            element.m_quality = Part<TextMeshProUGUI>(root, "quality");
            element.m_equiped = Part<Image>(root, "equiped");
            element.m_queued = Part<Image>(root, "queued");
            element.m_selected = (FindDeep(root, "selected") ?? Part<Image>(root, "selected").transform).gameObject;
            element.m_noteleport = Part<Image>(root, "noteleport");
            element.m_food = Part<Image>(root, "foodicon");
            element.m_durability = Part<GuiBar>(root, "durability");
            element.m_dropFocus = FindDeep(root, "dropfocus")?.GetComponent<Image>() ?? DropFocus(root as RectTransform);
            element.DropFocusOriginalColor = new Color(1f, 0.85f, 0.4f, 0.55f);
            element.m_tooltip = prefab.GetComponent<UITooltip>() ?? prefab.GetComponentInChildren<UITooltip>(true) ?? prefab.AddComponent<UITooltip>();
            element.m_touchHighlightColor = element.m_button.colors.highlightedColor;

            Debug.LogWarning($"[PortDiagnostics] InventoryElement added to slot prefab '{prefab.name}'");
        }

        private static RectTransform TouchRect(RectTransform slot)
        {
            var go = new GameObject("touchrect", typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(slot, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(70f, 70f);
            return rt;
        }

        /// <summary>Slot-sized highlight behind the icon; invisible until InventoryGrid tints it on a touch layout.</summary>
        private static Image DropFocus(RectTransform slot)
        {
            var go = new GameObject("dropfocus", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(slot, false);
            rt.SetSiblingIndex(0);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var image = go.GetComponent<Image>();
            image.sprite = Auga.Assets.ItemBackgroundSprite;
            image.type = Image.Type.Simple;
            image.color = Color.clear;
            image.raycastTarget = false;
            return image;
        }

        /// <summary>The named child's component, or a hidden stand-in when Auga's slot has no such part.</summary>
        private static T Part<T>(Transform root, string name) where T : Component
        {
            var child = FindDeep(root, name);
            if (child != null)
            {
                var existing = child.GetComponent<T>();
                if (existing != null)
                {
                    return existing;
                }
            }

            Debug.LogWarning($"[PortDiagnostics] slot prefab '{root.name}' has no {typeof(T).Name} named '{name}' - hidden stand-in created");
            var standIn = new GameObject(name + "_PortStandIn", typeof(RectTransform));
            standIn.transform.SetParent(root, false);
            standIn.SetActive(false);
            return standIn.AddComponent<T>();
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t != root && t.name == name)
                {
                    return t;
                }
            }

            return null;
        }
    }
}
