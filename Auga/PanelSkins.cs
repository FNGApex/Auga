using System;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Auga
{
    /// <summary>
    /// Art pass 2026-09-24 (5.18 + 5.19): the vanilla 1.0 panels that still showed wood art under Auga - the generic popup
    /// (UnifiedPopup), the achievements panel and its unlock popup, the pause-menu player list, the world modifiers window,
    /// the radial menu and the hovered-piece author window - get Auga's panel, buttons, fonts and the art-pass sprites
    /// (port-tools/art_pass.py). Same objects, same behaviour; only the look changes. [Panels] AugaPanelSkins = false keeps
    /// them vanilla.
    /// </summary>
    public static class PanelSkins
    {
        internal static bool Enabled => Auga.PanelSkinsEnabled == null || Auga.PanelSkinsEnabled.Value;

        /// <summary>Skin a panel once: Auga art (SettingsSkin's image rules) and Auga fonts.</summary>
        internal static void Skin(Transform root, string what)
        {
            if (!Enabled || root == null || root.GetComponent<SettingsSkinMarker>() != null)
            {
                return;
            }

            try
            {
                root.gameObject.AddComponent<SettingsSkinMarker>();
                SettingsSkin.Load();
                SettingsSkin.Apply(root, root, false);
                FillBackdrops(root);
                foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                {
                    SkinText(text);
                }
            }
            catch (Exception e)
            {
                // A cosmetic pass must never take a screen down with it.
                Debug.LogWarning($"[Auga] {what} skin failed, leaving it vanilla: {e}");
            }
        }

        /// <summary>
        /// SettingsSkin draws TextBackdrop simple + aspect-kept (right for small input fields); list and content wells here
        /// are large, so it is sliced to fill them instead of floating as a rounded blob in the middle.
        /// </summary>
        internal static void FillBackdrops(Transform root)
        {
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite != null && image.sprite.name == "TextBackdrop")
                {
                    image.type = Image.Type.Sliced;
                    image.preserveAspect = false;
                }
            }
        }

        /// <summary>
        /// Vanilla serif/sans become Auga's Source Sans (bold on buttons and headers), Norse titles become Auga's Norse
        /// bold; vanilla orange labels become Auga's label colour, white and red stay.
        /// </summary>
        internal static void SkinText(TMP_Text text)
        {
            var fontName = text.font != null ? text.font.name : "";
            var inButton = text.GetComponentInParent<Button>(true) != null;
            var isHeader = text.name.IndexOf("topic", StringComparison.OrdinalIgnoreCase) >= 0
                           || text.name.IndexOf("header", StringComparison.OrdinalIgnoreCase) >= 0
                           || text.name.IndexOf("title", StringComparison.OrdinalIgnoreCase) >= 0;
            if (fontName.StartsWith("Norse", StringComparison.OrdinalIgnoreCase) || (isHeader && !inButton))
            {
                SettingsSkin.SetFont(text, SettingsSkin._norseBold ?? SettingsSkin._bold);
                text.color = SettingsSkin.Bright;
                return;
            }

            SettingsSkin.SetFont(text, inButton ? SettingsSkin._bold : SettingsSkin._regular);
            if (inButton)
            {
                text.fontStyle |= FontStyles.UpperCase;
                // Auga's bold caps run wider than the vanilla serif: shrink to fit instead of spilling out of the button.
                text.enableAutoSizing = true;
                text.fontSizeMax = Mathf.Max(text.fontSize, 12f);
                text.fontSizeMin = 9f;
                text.textWrappingMode = TextWrappingModes.NoWrap;
            }

            var c = text.color;
            var isOrange = c.r > 0.8f && c.g > 0.35f && c.g < 0.85f && c.b < 0.45f;
            if (isOrange)
            {
                text.color = inButton ? SettingsSkin.Bright : SettingsSkin.Label;
            }
        }

        /// <summary>A sliced art-pass sprite on an Image (list rows); keeps the Image's own colour alpha.</summary>
        internal static void UseSliced(Image image, Sprite sprite, float pixelsPerUnitMultiplier = 2f)
        {
            if (image == null || sprite == null)
            {
                return;
            }

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
            image.preserveAspect = false;
            image.color = Color.white;
        }

        /// <summary>
        /// A decoration Image drawn BEHIND <paramref name="target"/>: a sibling just before it (UI children draw on top of
        /// their parent), outside any layout, never taking clicks. <see cref="AugaToastFader"/> keeps it centred on the target.
        /// </summary>
        internal static Image AddBehind(Transform target, string name, Sprite sprite, Vector2 size, Color color)
        {
            var parent = target.parent;
            var existing = parent.Find(name);
            if (existing != null)
            {
                return existing.GetComponent<Image>();
            }

            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            go.transform.SetSiblingIndex(target.GetSiblingIndex());
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.position = target.position;
            go.GetComponent<LayoutElement>().ignoreLayout = true;
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }
    }

    // ------------------------------------------------------------------ UnifiedPopup (generic yes/no / text popups)

    [HarmonyPatch(typeof(UnifiedPopup), "Awake")]
    public static class PanelSkins_UnifiedPopup
    {
        [HarmonyPostfix]
        public static void Postfix(UnifiedPopup __instance) => PanelSkins.Skin(__instance.transform, "popup");
    }

    // ------------------------------------------------------------------ achievements panel + details

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnOpenAchievements))]
    public static class PanelSkins_Achievements
    {
        [HarmonyPostfix]
        public static void Postfix(InventoryGui __instance)
        {
            if (!PanelSkins.Enabled || __instance.m_achievementsPanel == null)
            {
                return;
            }

            var panel = __instance.m_achievementsPanel;
            PanelSkins.Skin(panel.transform, "achievements");
            // The list is rebuilt on every open: rows get the art-pass row plates.
            foreach (var row in panel.m_achievementsList)
            {
                SkinRow(row);
            }
        }

        internal static void SkinRow(GameObject row)
        {
            if (row == null || row.GetComponent<SettingsSkinMarker>() != null)
            {
                return;
            }

            row.AddComponent<SettingsSkinMarker>();
            foreach (var image in row.GetComponentsInChildren<Image>(true))
            {
                if (image.name == "bkg")
                {
                    PanelSkins.UseSliced(image, Auga.Assets.ListRow);
                }
                else if (image.name == "selected")
                {
                    PanelSkins.UseSliced(image, Auga.Assets.ListRowSelected);
                }
            }

            // The tiles are buttons, but their texts are a title and a sentence, not button labels: no caps, no shrink-to-one-line.
            foreach (var text in row.GetComponentsInChildren<TMP_Text>(true))
            {
                if (text.name == "name")
                {
                    SettingsSkin.SetFont(text, SettingsSkin._bold);
                    text.color = SettingsSkin.Bright;
                }
                else if (text.name == "description")
                {
                    SettingsSkin.SetFont(text, SettingsSkin._regular);
                    text.color = SettingsSkin.Label;
                    text.textWrappingMode = TextWrappingModes.Normal;
                }
                else
                {
                    PanelSkins.SkinText(text);
                }
            }
        }
    }

    [HarmonyPatch(typeof(AchievementsGui), nameof(AchievementsGui.OnOpenAchievementDetails))]
    public static class PanelSkins_AchievementDetails
    {
        [HarmonyPostfix]
        public static void Postfix(AchievementsGui __instance)
        {
            if (!PanelSkins.Enabled || __instance.m_achievementDetails == null)
            {
                return;
            }

            var details = __instance.m_achievementDetails.transform;
            SettingsSkin.Load();
            SettingsSkin.Apply(details, details, false);
            PanelSkins.FillBackdrops(details);
            foreach (var text in details.GetComponentsInChildren<TMP_Text>(true))
            {
                PanelSkins.SkinText(text);
            }
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnOpenTrophies))]
    public static class PanelSkins_Trophies
    {
        [HarmonyPostfix]
        public static void Postfix(InventoryGui __instance)
        {
            if (__instance.m_trophiesPanel != null)
            {
                PanelSkins.Skin(__instance.m_trophiesPanel.transform, "trophies");
            }
        }
    }

    // ------------------------------------------------------------------ achievement unlock popup (toast)

    [HarmonyPatch(typeof(AchievementUnlockPopup), nameof(AchievementUnlockPopup.SetInfo))]
    public static class PanelSkins_AchievementToast
    {
        [HarmonyPostfix]
        public static void Postfix(AchievementUnlockPopup __instance)
        {
            if (!PanelSkins.Enabled || __instance.GetComponent<SettingsSkinMarker>() != null)
            {
                return;
            }

            try
            {
                __instance.gameObject.AddComponent<SettingsSkinMarker>();
                SettingsSkin.Load();
                var fader = __instance.gameObject.AddComponent<AugaToastFader>();
                fader.Source = __instance.m_achName;

                // The name sits on the art-pass plate; the icon in a gold-rimmed Auga diamond. Both are drawn behind.
                var name = __instance.m_achName;
                if (name != null)
                {
                    SettingsSkin.SetFont(name, SettingsSkin._norseBold ?? SettingsSkin._bold);
                    name.fontStyle |= FontStyles.UpperCase;
                    var width = Mathf.Max(300f, name.preferredWidth + 140f);
                    // pixelsPerUnitMultiplier 3: the plate's 64 px side caps draw at ~43 units and its height stays undistorted (73 units)
                    var plate = PanelSkins.AddBehind(name.transform, "AugaToastPlate", Auga.Assets.ToastPlate, new Vector2(width, 73f), Color.white);
                    plate.type = Image.Type.Sliced;
                    plate.pixelsPerUnitMultiplier = 3f;
                    fader.Add(plate, name.transform, true);
                }

                var icon = __instance.m_achIcon;
                if (icon != null)
                {
                    var diamond = SettingsSkin.Art("Container_Diamond");
                    var size = ((RectTransform)icon.transform).rect.size;
                    // the vanilla icon has its own square frame: the diamond is big enough for its points to show around it
                    var edge = Mathf.Max(size.x, size.y) * 1.45f;
                    var rim = PanelSkins.AddBehind(icon.transform, "AugaIconRim", diamond, new Vector2(edge + 8f, edge + 8f), SettingsSkin.Gold);
                    var back = PanelSkins.AddBehind(icon.transform, "AugaIconBack", diamond, new Vector2(edge, edge), SettingsSkin.Dark);
                    fader.Add(rim, icon.transform);
                    fader.Add(back, icon.transform);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Auga] achievement popup skin failed, leaving it vanilla: " + e);
            }
        }
    }

    /// <summary>
    /// The unlock popup fades its icon and name by rewriting their colour and slides its root: the Auga decorations follow
    /// that alpha and stay centred on the element they sit behind.
    /// </summary>
    public class AugaToastFader : MonoBehaviour
    {
        public Graphic Source;
        private readonly System.Collections.Generic.List<(Graphic graphic, Transform target, bool textCentre)> _items = new System.Collections.Generic.List<(Graphic, Transform, bool)>();

        /// <param name="textCentre">centre on the rendered text (its rect is taller than the line and top-aligned).</param>
        public void Add(Graphic graphic, Transform target, bool textCentre = false) => _items.Add((graphic, target, textCentre));

        private void LateUpdate()
        {
            if (Source == null)
            {
                return;
            }

            var a = Source.color.a;
            foreach (var (graphic, target, textCentre) in _items)
            {
                if (graphic == null || target == null)
                {
                    continue;
                }

                var c = graphic.color;
                c.a = a;
                graphic.color = c;
                graphic.transform.position = textCentre && target.GetComponent<TMP_Text>() is TMP_Text text
                    ? text.rectTransform.TransformPoint(text.textBounds.center)
                    : target.position;
            }
        }
    }

    // ------------------------------------------------------------------ pause menu player list

    [HarmonyPatch(typeof(Menu), nameof(Menu.OnCurrentPlayers))]
    public static class PanelSkins_PlayerList
    {
        private static readonly System.Reflection.FieldInfo Instance = AccessTools.Field(typeof(Menu), "m_currentPlayersInstance");

        [HarmonyPostfix]
        public static void Postfix(Menu __instance)
        {
            var list = Instance?.GetValue(__instance) as GameObject;
            if (list != null)
            {
                PanelSkins.Skin(list.transform, "player list");
            }
        }
    }

    // ------------------------------------------------------------------ world modifiers window (main menu)

    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.OnServerOptions))]
    public static class PanelSkins_ServerOptions
    {
        [HarmonyPostfix]
        public static void Postfix(FejdStartup __instance)
        {
            if (__instance.m_serverOptions != null)
            {
                PanelSkins.Skin(__instance.m_serverOptions.transform, "world modifiers");
            }
        }
    }

    // ------------------------------------------------------------------ hovered piece author: below Auga's minimap

    /// <summary>
    /// Vanilla puts the hovered-piece author window in the top-right corner, where Auga's larger (and movable) minimap and
    /// its biome label sit. While it shows, keep its top edge just below the minimap.
    /// </summary>
    [HarmonyPatch(typeof(Hud), "UpdateCrosshair")]
    public static class PanelSkins_PieceAuthorPosition
    {
        private static readonly Vector3[] Corners = new Vector3[4];

        [HarmonyPostfix]
        public static void Postfix(Hud __instance)
        {
            var window = __instance.m_hoveredPieceAuthorWindow;
            if (!PanelSkins.Enabled || window == null || !window.activeSelf || Minimap.instance == null || Minimap.instance.m_smallRoot == null)
            {
                return;
            }

            var map = Minimap.instance.m_smallRoot.transform as RectTransform;
            var rect = window.transform as RectTransform;
            if (map == null || rect == null || !map.gameObject.activeInHierarchy)
            {
                return;
            }

            // bottom of the minimap incl. its biome label, in the window's parent space
            map.GetWorldCorners(Corners);
            var mapBottom = Mathf.Min(Corners[0].y, Corners[3].y);
            var biome = Minimap.instance.m_biomeNameSmall != null ? Minimap.instance.m_biomeNameSmall.rectTransform : null;
            if (biome != null && biome.gameObject.activeInHierarchy)
            {
                biome.GetWorldCorners(Corners);
                mapBottom = Mathf.Min(mapBottom, Corners[0].y);
            }

            rect.GetWorldCorners(Corners);
            var windowTop = Mathf.Max(Corners[1].y, Corners[2].y);
            var gap = 12f * (rect.lossyScale.y > 0f ? rect.lossyScale.y : 1f);
            if (windowTop > mapBottom - gap)
            {
                rect.position += new Vector3(0f, mapBottom - gap - windowTop, 0f);
            }
        }
    }

    // ------------------------------------------------------------------ radial menu + hovered piece author (HUD)

    [HarmonyPatch(typeof(Hud), "Awake")]
    public static class PanelSkins_Hud
    {
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        public static void Postfix(Hud __instance)
        {
            if (!PanelSkins.Enabled)
            {
                return;
            }

            try
            {
                SettingsSkin.Load();
                SkinRadial(__instance);
                SkinPieceAuthor(__instance);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Auga] HUD panel skin failed, leaving it vanilla: " + e);
            }
        }

        private static void SkinRadial(Hud hud)
        {
            var radial = hud.m_radialMenu != null ? hud.m_radialMenu.transform : null;
            if (radial == null)
            {
                return;
            }

            foreach (var image in radial.GetComponentsInChildren<Image>(true))
            {
                if (image.name == "ElementInfo" && Auga.Assets.RadialCenter != null && image.transform.Find("AugaRadialCenter") == null)
                {
                    // The radial re-tints this image every frame, so the Auga disc is its first child: drawn over the
                    // vanilla backdrop, under the title texts (later siblings).
                    var size = ((RectTransform)image.transform).rect.size;
                    var go = new GameObject("AugaRadialCenter", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
                    go.transform.SetParent(image.transform, false);
                    go.transform.SetAsFirstSibling();
                    var rect = (RectTransform)go.transform;
                    rect.anchorMin = Vector2.zero;
                    rect.anchorMax = Vector2.one;
                    rect.offsetMin = rect.offsetMax = Vector2.zero;
                    go.GetComponent<LayoutElement>().ignoreLayout = true;
                    var disc = go.GetComponent<Image>();
                    disc.sprite = Auga.Assets.RadialCenter;
                    disc.preserveAspect = true;
                    disc.raycastTarget = false;
                }
                else if (image.sprite != null && image.sprite.name == "Background")
                {
                    SettingsSkin.Use(image, "TextBackdrop", new Color(0f, 0f, 0f, 0.75f));
                    image.type = Image.Type.Sliced;
                    image.preserveAspect = false;
                }
            }

            foreach (var text in radial.GetComponentsInChildren<TMP_Text>(true))
            {
                PanelSkins.SkinText(text);
                if (text.name == "Title")
                {
                    text.color = SettingsSkin.BrightGold;
                }
            }
        }

        private static void SkinPieceAuthor(Hud hud)
        {
            var window = hud.m_hoveredPieceAuthorWindow;
            if (window == null)
            {
                return;
            }

            foreach (var image in window.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite != null && image.sprite.name == "Background")
                {
                    SettingsSkin.Use(image, "TextBackdrop", new Color(0.094f, 0.078f, 0.063f, 0.85f));
                    image.type = Image.Type.Sliced;
                    image.preserveAspect = false;
                }
                else if (image.name == "player_portrait_border" && Auga.Assets.PortraitRing != null)
                {
                    image.sprite = Auga.Assets.PortraitRing;
                    image.type = Image.Type.Simple;
                    image.preserveAspect = true;
                    image.color = Color.white;
                }
                else if (image.name == "player_portrait_background")
                {
                    image.color = new Color(0.094f, 0.078f, 0.063f, 0.9f);
                }
            }

            foreach (var text in window.GetComponentsInChildren<TMP_Text>(true))
            {
                PanelSkins.SkinText(text);
            }

            if (hud.m_hoveredPieceAuthorName != null)
            {
                hud.m_hoveredPieceAuthorName.color = SettingsSkin.BrightGold;
            }
        }
    }
}
