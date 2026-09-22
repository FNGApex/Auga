using HarmonyLib;
using TMPro;
using UnityEngine;

namespace Auga
{
    [HarmonyPatch]
    public static class TextViewer_Setup
    {
        [HarmonyPatch(typeof(TextViewer), nameof(TextViewer.Awake))]
        public static class TextViewer_Awake_Patch
        {
            public static bool Prefix(TextViewer __instance)
            {
                return !SetupHelper.DirectObjectReplace(__instance.transform, Auga.Assets.TextViewerPrefab, "TextViewer");
            }

            // Valheim 1.0 port: the rune topic ships inactive in Auga's prefab and its animation clips no longer have a
            // binding that turns it on (they still drive a DividerLarge that is gone), so rune stones showed no title.
            // It sits inside textroot, which the animator shows and hides as a whole.
            public static void Postfix(TextViewer __instance)
            {
                if (__instance.m_topic == null || __instance.name.EndsWith(PortCarryOver.DonorSuffix))
                {
                    return;
                }

                __instance.m_topic.gameObject.SetActive(true);
                if (UnityEngine.ColorUtility.TryParseHtmlString(API.BrightestGold, out var gold))
                {
                    __instance.m_topic.color = gold;
                }

                AddCloseHint(__instance);
            }

            // AUDIT2 text-3: Auga's prefab has no CloseText (m_closeText points at an inactive dummy), so the 'Press [E] to
            // hide' line under a rune stone was gone. Clone vanilla's from the hidden donor and restyle it.
            private static void AddCloseHint(TextViewer instance)
            {
                var textroot = instance.transform.Find("textroot");
                if (textroot == null || textroot.Find("CloseText") != null)
                {
                    return;
                }

                Transform donorClose = null;
                for (var parent = instance.transform.parent; parent != null && donorClose == null; parent = parent.parent)
                {
                    var donor = parent.Find("TextViewer" + PortCarryOver.DonorSuffix);
                    donorClose = donor != null ? donor.Find("textroot/CloseText") : null;
                }

                if (donorClose == null)
                {
                    return;
                }

                var close = Object.Instantiate(donorClose.gameObject, textroot, false);
                close.name = "CloseText";
                // Auga's textroot stays active and hides its children through the animator, so the hint is toggled by
                // the ShowText / Hide patches below instead of following the root like vanilla's does.
                close.SetActive(false);
                var text = close.GetComponent<TMP_Text>();
                if (text == null)
                {
                    Object.Destroy(close);
                    return;
                }

                SettingsSkin.Load();
                SettingsSkin.SetFont(text, SettingsSkin._regular);
                text.fontSize = 18f;
                text.enableAutoSizing = false;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.alignment = TextAlignmentOptions.Center;
                text.color = SettingsSkin.Label;
                text.alpha = 1f;
                text.enabled = true;
                text.overflowMode = TextOverflowModes.Overflow;
                if (text.canvasRenderer != null)
                {
                    text.canvasRenderer.SetAlpha(1f);
                }

                if (string.IsNullOrEmpty(text.text) || !text.text.Contains("$") && !text.text.Contains("["))
                {
                    text.text = "$hud_press [<color=yellow>$KEY_Use</color>] $hud_tohide";
                }

                text.text = text.text.Replace("color=yellow", $"color={Auga.Colors.Emphasis}");
                Localization.instance.Localize(close.transform);

                var rt = (RectTransform)close.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 18f);
                rt.sizeDelta = new Vector2(500f, 31f);
                instance.m_closeText = close;
            }
        }

        private static bool IsAugaCloseHint(TextViewer instance)
        {
            return instance != null && instance.m_closeText != null && instance.m_closeText.name == "CloseText"
                   && instance.m_closeText.transform.parent != null && instance.m_closeText.transform.parent.name == "textroot"
                   && !instance.name.EndsWith(PortCarryOver.DonorSuffix);
        }

        [HarmonyPatch(typeof(TextViewer), nameof(TextViewer.ShowText))]
        public static class TextViewer_ShowText_Patch
        {
            public static void Postfix(TextViewer __instance, TextViewer.Style style, bool autoHide)
            {
                if (!IsAugaCloseHint(__instance))
                {
                    return;
                }

                // Only the rune / plain text panel (textroot) has the hint; it closes on Use / Escape (1.0's LateUpdate).
                if (style == TextViewer.Style.Rune && !(Player.m_localPlayer == null && autoHide))
                {
                    __instance.m_closeText.SetActive(true);
                }
            }
        }

        [HarmonyPatch(typeof(TextViewer), nameof(TextViewer.Hide))]
        public static class TextViewer_Hide_Patch
        {
            public static void Postfix(TextViewer __instance)
            {
                if (IsAugaCloseHint(__instance))
                {
                    __instance.m_closeText.SetActive(false);
                }
            }
        }
    }
}
