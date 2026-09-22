using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    /// <summary>
    /// Valheim 1.0 port (AUDIT2 inventory-11). 1.0 moved the split-stack popup into its own SplitDialog component
    /// (InventoryGui.m_splitDialog). Auga ships a styled split panel (Inventory_screen/root/SplitDialog: dark dialog,
    /// slider, item icon, amount) but no such component, so the vanilla wood panel was still showing. This gives Auga's
    /// panel the component at runtime and wires it the way vanilla's prefab is wired, so InventoryGui's keyboard /
    /// gamepad handling (digits, arrows, Enter, Escape / B) works unchanged. Auga's OK / Cancel buttons (ColorButtonText)
    /// are wired as the dialog's buttons; hidden stand-ins only if a rebuilt prefab ever drops them. The darkened
    /// backdrop cancels on touch like vanilla's CloseButton.
    /// </summary>
    public static class PortSplitDialog
    {
        public static SplitDialog Setup(Transform root)
        {
            var panel = root.Find("Dialog") as RectTransform;
            var slider = root.GetComponentInChildren<Slider>(true);
            var icon = root.Find("Dialog/InventoryElement/icon")?.GetComponent<Image>();
            var amount = root.Find("Dialog/InventoryElement/amount")?.GetComponent<TMP_Text>();
            var itemName = root.Find("Dialog/InventoryElement/DummyText")?.GetComponent<TMP_Text>();
            if (panel == null || slider == null || icon == null || amount == null)
            {
                Debug.LogWarning($"[Auga] split dialog: Auga's panel is missing a part (panel={panel != null} slider={slider != null} icon={icon != null} amount={amount != null}), keeping vanilla's");
                return null;
            }

            if (root.GetComponent<SplitDialog>() != null)
            {
                return root.GetComponent<SplitDialog>();
            }

            var dialog = root.gameObject.AddComponent<SplitDialog>();
            dialog.m_splitSlider = slider;
            dialog.m_panel = panel;
            dialog.m_panelNormalPosition = Marker(panel, "PanelNormalPosition");
            dialog.m_panelTouchPosition = Marker(panel, "PanelTouchPosition");
            dialog.m_splitAmount = amount;
            dialog.m_splitIcon = icon;
            dialog.m_splitIconName = itemName != null ? itemName : amount;
            dialog.m_splitOkButton = root.Find("Dialog/ButtonOk")?.GetComponent<Button>() ?? HiddenButton(root, "Button_ok");
            dialog.m_splitCancelButton = root.Find("Dialog/ButtonCancel")?.GetComponent<Button>() ?? HiddenButton(root, "Button_cancel");

            var darken = root.Find("Darken");
            if (darken != null)
            {
                var backdrop = darken.GetComponent<Button>() ?? darken.gameObject.AddComponent<Button>();
                backdrop.transition = Selectable.Transition.None;
                backdrop.onClick.AddListener(dialog.BackgroundPress);
            }

            slider.wholeNumbers = true;
            root.gameObject.SetActive(false);
            return dialog;
        }

        private static RectTransform Marker(RectTransform panel, string name)
        {
            var marker = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)marker.transform;
            rt.SetParent(panel.parent, false);
            rt.anchorMin = panel.anchorMin;
            rt.anchorMax = panel.anchorMax;
            rt.pivot = panel.pivot;
            rt.anchoredPosition = panel.anchoredPosition;
            rt.sizeDelta = Vector2.zero;
            return rt;
        }

        private static Button HiddenButton(Transform root, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Button));
            go.transform.SetParent(root, false);
            ((RectTransform)go.transform).sizeDelta = Vector2.zero;
            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.None;
            return button;
        }
    }
}
