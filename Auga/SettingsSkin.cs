using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    /// <summary>
    /// Valheim 1.0 port: the settings screen. 1.0 replaced the single Settings class with a tabbed screen
    /// (Gameplay, Keyboard & Mouse, Controller, Graphics, Audio, Accessibility, Radial), so Auga's 2023
    /// settings prefab can no longer drive it. Instead of rebuilding that prefab against seven new classes,
    /// the live vanilla screen is re-skinned when it is created: same objects, same behaviour, Auga's
    /// panel, fonts, colours and widget art. New options the game adds later are skinned automatically.
    /// </summary>
    [HarmonyPatch(typeof(Settings), "Awake")]
    public static class SettingsSkin
    {
        internal static readonly Color Dark = new Color(0.094f, 0.078f, 0.063f, 1f);
        internal static readonly Color Gold = new Color(0.725f, 0.541f, 0.071f, 1f);
        internal static readonly Color BrightGold = new Color(1f, 0.749f, 0.106f, 1f);
        internal static readonly Color Label = new Color(0.78f, 0.74f, 0.68f, 1f);
        internal static readonly Color Dim = new Color(0.5f, 0.46f, 0.42f, 1f);
        internal static readonly Color Bright = new Color(0.95f, 0.93f, 0.9f, 1f);

        internal static TMP_FontAsset _regular, _bold, _norse, _norseBold;
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private static bool _loaded;

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(Settings __instance)
        {
            if (Auga.SettingsSkinEnabled != null && !Auga.SettingsSkinEnabled.Value)
            {
                return;
            }

            try
            {
                Load();
                Apply(__instance.transform);
            }
            catch (System.Exception e)
            {
                // A cosmetic pass must never take the settings screen down with it.
                Debug.LogWarning("[Auga] settings skin failed, leaving the screen vanilla: " + e);
            }
        }

        internal static void Load()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            var bundle = Auga.AssetBundle;
            if (bundle != null)
            {
                _regular = bundle.LoadAsset<TMP_FontAsset>("SourceSansPro-Regular SDF");
                _bold = bundle.LoadAsset<TMP_FontAsset>("SourceSansPro-Bold SDF");
                _norse = bundle.LoadAsset<TMP_FontAsset>("Norse SDF");
                _norseBold = bundle.LoadAsset<TMP_FontAsset>("Norsebold SDF");
            }

            // Auga's art, harvested from the prefabs that already ship in the bundle.
            foreach (var source in new[] { Auga.Assets.SettingsPrefab, Auga.Assets.PanelBase, Auga.Assets.ButtonSettings, Auga.Assets.ButtonFancy, Auga.Assets.InventoryScreen })
            {
                if (source == null)
                {
                    continue;
                }

                foreach (var image in source.GetComponentsInChildren<Image>(true))
                {
                    if (image.sprite != null && !Sprites.ContainsKey(image.sprite.name))
                    {
                        Sprites[image.sprite.name] = image.sprite;
                    }
                }
            }
        }

        internal static Sprite Art(string name) => Sprites.TryGetValue(name, out var sprite) ? sprite : null;

        /// <summary>
        /// Skin everything under <paramref name="scope"/> (default: the whole settings screen at <paramref name="root"/>).
        /// <paramref name="texts"/> false = art only; other screens (PanelSkins) style their own text.
        /// </summary>
        internal static void Apply(Transform root, Transform scope = null, bool texts = true)
        {
            scope = scope != null ? scope : root;
            foreach (var image in scope.GetComponentsInChildren<Image>(true))
            {
                SkinImage(image);
            }

            if (texts)
            {
                foreach (var text in scope.GetComponentsInChildren<TMP_Text>(true))
                {
                    SkinText(root, text);
                }
            }

            // Sliders and scrollbars by their component wiring rather than sprite names: not all of them share art.
            foreach (var slider in scope.GetComponentsInChildren<Slider>(true))
            {
                var background = slider.transform.Find("Background");
                if (background != null && background.GetComponent<Image>() is Image backgroundImage)
                {
                    backgroundImage.color = new Color(0f, 0f, 0f, 0.55f);
                }

                if (slider.fillRect != null && slider.fillRect.GetComponent<Image>() is Image fill)
                {
                    fill.color = Gold;
                }

                if (slider.handleRect != null && slider.handleRect.GetComponent<Image>() is Image handle)
                {
                    Use(handle, "Container_Diamond", BrightGold);
                    handle.transform.localScale = Vector3.one;
                }
            }

            foreach (var scrollbar in scope.GetComponentsInChildren<Scrollbar>(true))
            {
                if (scrollbar.GetComponent<Image>() is Image track)
                {
                    track.color = new Color(0f, 0f, 0f, 0.45f);
                }

                if (scrollbar.handleRect != null && scrollbar.handleRect.GetComponent<Image>() is Image handle)
                {
                    // The vanilla handle sprite is itself orange wood; a flat bar takes the tint cleanly.
                    handle.sprite = null;
                    handle.color = new Color(Gold.r, Gold.g, Gold.b, 0.85f);
                }
            }
        }

        private static void SkinImage(Image image)
        {
            var spriteName = image.sprite != null ? image.sprite.name : "";
            var parentName = image.transform.parent != null ? image.transform.parent.name : "";
            switch (spriteName)
            {
                case "woodpanel_settings":
                case "woodpanel_400_tileable":
                case "woodpanel_password":
                case "woodpanel_512x512":
                case "woodpanel_trophys":
                    ReplacePanel(image);
                    break;

                case "text_field":
                    Use(image, "TextBackdrop", new Color(0f, 0f, 0f, 0.6f));
                    image.type = Image.Type.Sliced;
                    image.preserveAspect = false;
                    break;

                case "panel_interior_bkg_128":
                    image.sprite = null;
                    image.color = new Color(0f, 0f, 0f, 0.22f);
                    break;

                case "button":
                    SkinButton(image, Auga.Assets.ButtonSettings);
                    break;

                case "button_tab":
                    // Auga tabs are plain text; the image stays as the click target.
                    image.sprite = null;
                    image.color = Color.clear;
                    break;

                case "button_tab_selected":
                    image.sprite = null;
                    image.color = Color.clear;
                    break;

                case "checkbox":
                    Use(image, "Container_Diamond", Dark);
                    break;

                case "checkbox_marker":
                    // Checked = the whole diamond turns gold; clearer than a small inset mark.
                    Use(image, "Container_Diamond", BrightGold);
                    break;

                case "Knob":
                    Use(image, "Container_Diamond", BrightGold);
                    break;

                case "Background":
                    image.color = new Color(0f, 0f, 0f, 0.55f);
                    break;

                case "InputFieldBackground":
                case "item_background":
                    Use(image, "TextBackdrop", new Color(0f, 0f, 0f, 0.6f));
                    break;

                case "panel_separator":
                    image.color = new Color(Gold.r, Gold.g, Gold.b, 0.6f);
                    break;

                case "selection_frame":
                    image.color = BrightGold;
                    break;

                case "":
                    if (image.name == "Fill")
                    {
                        image.color = Gold;
                    }

                    break;
            }
        }

        internal static void Use(Image image, string art, Color color)
        {
            var sprite = Art(art);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Simple;
                image.preserveAspect = true;
            }

            image.color = color;
        }

        /// <summary>The wood board becomes an invisible click blocker with an Auga panel behind the content.</summary>
        internal static void ReplacePanel(Image wood)
        {
            wood.sprite = null;
            wood.color = Color.clear;
            if (Auga.Assets.PanelBase == null || wood.transform.Find("AugaPanel") != null)
            {
                return;
            }

            var panel = Object.Instantiate(Auga.Assets.PanelBase, wood.transform, false);
            panel.name = "AugaPanel";
            panel.transform.SetAsFirstSibling();
            if (panel.transform is RectTransform rect)
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            // Decoration only: layout groups on the vanilla board must not position it, clicks must pass through.
            var layout = panel.AddComponent<LayoutElement>();
            layout.ignoreLayout = true;
            foreach (var graphic in panel.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = false;
            }
        }

        internal static void SkinButton(Image image, GameObject augaButtonPrefab)
        {
            if (augaButtonPrefab == null)
            {
                return;
            }

            var sourceImage = augaButtonPrefab.GetComponentInChildren<Image>(true);
            var sourceButton = augaButtonPrefab.GetComponentInChildren<Button>(true);
            if (sourceImage != null)
            {
                image.sprite = sourceImage.sprite;
                image.type = sourceImage.type;
                image.color = sourceImage.color;
                image.material = sourceImage.material;
            }

            var button = image.GetComponent<Selectable>();
            if (button != null && sourceButton != null)
            {
                button.transition = sourceButton.transition;
                button.spriteState = sourceButton.spriteState;
                button.colors = sourceButton.colors;
            }
        }

        private static void SkinText(Transform root, TMP_Text text)
        {
            var isTitle = text.transform.parent != null && text.transform.parent.parent == root && text.name == "Title";
            // AUDIT2 menus-4: the skin runs from Settings.Awake, before TabHandler activates the non-default tabs, and the
            // no-arg GetComponentInParent skips inactive objects - so every widget on those tabs was styled as a plain label.
            var inButton = text.GetComponentInParent<Selectable>(true) != null && text.GetComponentInParent<Toggle>(true) == null && text.GetComponentInParent<Slider>(true) == null;
            var inTabs = text.GetComponentInParent<TabHandler>(true) != null;
            var parentName = text.transform.parent != null ? text.transform.parent.name : "";

            if (isTitle)
            {
                SetFont(text, _norse ?? _norseBold);
                text.fontSize = 44f;
                text.fontStyle = FontStyles.UpperCase;
                text.color = Bright;
                return;
            }

            if (inTabs)
            {
                SetFont(text, _bold);
                text.fontStyle = FontStyles.UpperCase;
                text.color = text.name == "LabelSelected" ? Bright : Dim;
                return;
            }

            if (inButton && (parentName == "Back" || parentName == "Ok"))
            {
                SetFont(text, _norseBold ?? _bold);
                text.enableAutoSizing = true;
                text.fontSizeMax = 34f;
                text.fontSizeMin = 16f;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.fontStyle = FontStyles.UpperCase;
                text.color = Bright;
                return;
            }

            SetFont(text, inButton ? _bold : _regular);
            if (inButton)
            {
                text.fontStyle = FontStyles.UpperCase;
                // Auga's bold caps run wider than the vanilla serif: shrink to fit instead of spilling out of the button.
                text.enableAutoSizing = true;
                text.fontSizeMax = text.fontSize;
                text.fontSizeMin = 9f;
                text.textWrappingMode = TextWrappingModes.NoWrap;
            }

            // Vanilla labels are orange; keep white values and red warnings as they are.
            var c = text.color;
            var isOrange = c.r > 0.8f && c.g > 0.35f && c.g < 0.85f && c.b < 0.45f;
            if (isOrange)
            {
                text.color = inButton ? Bright : Label;
            }
        }

        internal static void SetFont(TMP_Text text, TMP_FontAsset font)
        {
            if (font != null)
            {
                text.font = font;
            }
        }
    }

    /// <summary>
    /// AUDIT2 menus-5: the settings screen instantiates some sub-panels on demand (Gameplay -> Blocked players), after the
    /// Awake-time skin pass, so they came up in vanilla wood inside the Auga screen. Skin them when they appear.
    /// </summary>
    [HarmonyPatch(typeof(Valheim.SettingsGui.GameplaySettings), nameof(Valheim.SettingsGui.GameplaySettings.OnBlockedPlayerList))]
    public static class SettingsSkin_BlockedPlayerList
    {
        private static readonly System.Reflection.FieldInfo ListInstance =
            AccessTools.Field(typeof(Valheim.SettingsGui.GameplaySettings), "m_blockedPlayerListInstance");

        [HarmonyPostfix]
        public static void Postfix(Valheim.SettingsGui.GameplaySettings __instance)
        {
            if (Auga.SettingsSkinEnabled != null && !Auga.SettingsSkinEnabled.Value)
            {
                return;
            }

            try
            {
                var list = ListInstance?.GetValue(__instance) as GameObject;
                if (list == null || list.GetComponent<SettingsSkinMarker>() != null)
                {
                    return;
                }

                list.AddComponent<SettingsSkinMarker>();
                var settings = __instance.GetComponentInParent<Settings>(true);
                SettingsSkin.Load();
                SettingsSkin.Apply(settings != null ? settings.transform : list.transform, list.transform);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Auga] settings sub-panel skin failed, leaving it vanilla: " + e);
            }
        }
    }

    /// <summary>Marks an on-demand settings sub-panel that has already been skinned.</summary>
    public class SettingsSkinMarker : MonoBehaviour
    {
    }
}
