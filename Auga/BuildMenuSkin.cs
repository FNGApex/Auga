using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    /// <summary>
    /// Valheim 1.0 port: the build menu. 1.0 replaced the old piece selection window (which Auga skinned,
    /// and which the game now never shows) with hudroot/BuildUIV2: usage / material / recent / favourite
    /// tabs, a tag list, a filter box and a pooled grid of piece buttons. Like the settings screen it is
    /// re-skinned in place, so all of its new behaviour stays vanilla. The piece and tag button prefabs are
    /// skinned too, because the menu instantiates those on demand.
    /// </summary>
    [HarmonyPatch(typeof(BuildUi), "Awake")]
    public static class BuildMenuSkin
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(BuildUi __instance)
        {
            if (Auga.BuildMenuSkinEnabled != null && !Auga.BuildMenuSkinEnabled.Value)
            {
                return;
            }

            try
            {
                SettingsSkin.Load();
                Skin(__instance.transform);
                if (__instance.m_pieceButtonPrefab != null)
                {
                    Skin(__instance.m_pieceButtonPrefab.transform);
                }

                if (__instance.m_tagButtonPrefab != null)
                {
                    Skin(__instance.m_tagButtonPrefab.transform);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Auga] build menu skin failed, leaving it vanilla: " + e);
            }
        }

        /// <summary>The "selected piece" readout next to the crosshair (name, description, requirements).</summary>
        [HarmonyPatch(typeof(Hud), "Awake")]
        public static class SelectedInfo
        {
            [HarmonyPostfix]
            [HarmonyPriority(Priority.Last)]
            public static void Postfix(Hud __instance)
            {
                if (Auga.BuildMenuSkinEnabled != null && !Auga.BuildMenuSkinEnabled.Value || __instance.m_buildHud == null)
                {
                    return;
                }

                try
                {
                    SettingsSkin.Load();
                    var info = __instance.m_buildHud.transform.Find("SelectedInfo");
                    if (info == null)
                    {
                        return;
                    }

                    // AUDIT2 hud-8: the panel art too, not only the text (Bkg2 = vanilla 'Background', res_bkg = item slots).
                    Skin(info);
                    foreach (var image in info.GetComponentsInChildren<Image>(true))
                    {
                        if (image.transform.parent == info && image.sprite != null && (image.sprite.name == "Background" || image.sprite.name.StartsWith("woodpanel")))
                        {
                            SettingsSkin.ReplacePanel(image);
                        }
                        else if (image.name.StartsWith("res_bkg") && Auga.Assets.ItemBackgroundSprite != null)
                        {
                            image.sprite = Auga.Assets.ItemBackgroundSprite;
                            image.color = Color.white;
                        }
                    }

                    foreach (var text in info.GetComponentsInChildren<TMP_Text>(true))
                    {
                        var isName = text == __instance.m_buildSelection;
                        SettingsSkin.SetFont(text, isName ? SettingsSkin._bold : SettingsSkin._regular);
                        if (isName)
                        {
                            text.fontStyle = FontStyles.UpperCase;
                        }

                        var c = text.color;
                        if (c.r > 0.8f && c.g > 0.35f && c.g < 0.85f && c.b < 0.45f)
                        {
                            text.color = isName ? SettingsSkin.Bright : SettingsSkin.Label;
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("[Auga] build info skin failed, leaving it vanilla: " + e);
                }
            }
        }

        private static void Skin(Transform root)
        {
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                SkinImage(image);
            }

            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                SkinText(text);
            }

            foreach (var scrollbar in root.GetComponentsInChildren<Scrollbar>(true))
            {
                if (scrollbar.GetComponent<Image>() is Image track)
                {
                    track.color = new Color(0f, 0f, 0f, 0.45f);
                }

                if (scrollbar.handleRect != null && scrollbar.handleRect.GetComponent<Image>() is Image handle)
                {
                    handle.sprite = null;
                    handle.color = new Color(SettingsSkin.Gold.r, SettingsSkin.Gold.g, SettingsSkin.Gold.b, 0.85f);
                }
            }

            // Tabs: the selected tab is the non-interactable one, and ButtonTextColor paints the label from these.
            foreach (var textColor in root.GetComponentsInChildren<ButtonTextColor>(true))
            {
                textColor.m_disabledColor = SettingsSkin.Bright;
                textColor.m_defaultColor = SettingsSkin.Dim;
                textColor.m_defaultMeshColor = SettingsSkin.Dim;
            }

            foreach (var tag in root.GetComponentsInChildren<BuildUiTagButton>(true))
            {
                tag.m_baseTextColor = SettingsSkin.Label;
            }
        }

        private static void SkinImage(Image image)
        {
            var spriteName = image.sprite != null ? image.sprite.name : "";
            switch (spriteName)
            {
                case "woodpanel_large":
                    SettingsSkin.ReplacePanel(image);
                    break;

                case "button_tab":
                    image.sprite = null;
                    image.color = Color.clear;
                    break;

                case "panel_separator":
                    image.color = new Color(SettingsSkin.Gold.r, SettingsSkin.Gold.g, SettingsSkin.Gold.b, 0.6f);
                    break;

                case "text_field":
                    SettingsSkin.Use(image, "TextBackdrop", new Color(0f, 0f, 0f, 0.6f));
                    image.preserveAspect = false;
                    image.type = Image.Type.Sliced;
                    break;

                case "Background":
                    if (image.GetComponent<BuildUiPieceButton>() != null)
                    {
                        // A piece slot: same square Auga uses for inventory slots.
                        if (Auga.Assets.ItemBackgroundSprite != null)
                        {
                            image.sprite = Auga.Assets.ItemBackgroundSprite;
                            image.type = Image.Type.Simple;
                        }

                        image.color = new Color(1f, 1f, 1f, 0.85f);
                    }
                    else if (image.name == "Selected")
                    {
                        // Selected tag row: vanilla blue becomes Auga gold.
                        image.color = new Color(SettingsSkin.Gold.r, SettingsSkin.Gold.g, SettingsSkin.Gold.b, 0.45f);
                    }
                    else if (image.GetComponent<Scrollbar>() == null)
                    {
                        // List backdrops.
                        image.color = new Color(0f, 0f, 0f, 0.25f);
                    }

                    break;
            }
        }

        private static void SkinText(TMP_Text text)
        {
            var fontName = text.font != null ? text.font.name : "";
            // Key glyph hints use sprite tags inside the sans font; leave those alone.
            if (text.text != null && text.text.Contains("<sprite"))
            {
                return;
            }

            var inTab = text.GetComponentInParent<ButtonTextColor>() != null;
            if (fontName.Contains("Norse"))
            {
                SettingsSkin.SetFont(text, fontName.Contains("bold") ? SettingsSkin._norseBold : SettingsSkin._norse);
            }
            else
            {
                SettingsSkin.SetFont(text, inTab ? SettingsSkin._bold : SettingsSkin._regular);
            }

            if (inTab)
            {
                text.fontStyle = FontStyles.UpperCase;
                return;
            }

            var c = text.color;
            var isOrange = c.r > 0.8f && c.g > 0.35f && c.g < 0.85f && c.b < 0.45f;
            if (isOrange)
            {
                text.color = SettingsSkin.Label;
            }
        }
    }
}
