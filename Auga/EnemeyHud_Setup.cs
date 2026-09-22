using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    [HarmonyPatch]
    public static class EnemyHud_Setup
    {
        [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.Awake))]
        public static class EnemyHud_Awake_Patch
        {
            public static bool Prefix(EnemyHud __instance)
            {
                return !SetupHelper.DirectObjectReplace(__instance.transform, Auga.Assets.EnemyHud, "EnemyHud");
            }

            // AUDIT2 misc-3: EnemyHud.ShowHud(c, isMount: true) dereferences Stamina/stamina_fast, Stamina/StaminaText and
            // Health/HealthText without a guard. Auga's HudMount copied the health group into Stamina verbatim and has no
            // texts, so any caller of that (currently dead in stock 1.0) path would kill EnemyHud.LateUpdate. Give it the parts.
            public static void Postfix(EnemyHud __instance)
            {
                if (__instance == null || __instance.name.EndsWith(PortCarryOver.DonorSuffix))
                    return;

                // Upstream issue #152 (still applied on 1.0): tamed / friendly creatures kept the red bar. Vanilla swaps
                // health_fast for an optional Health/health_fast_friendly on tamed characters; Auga's templates have none.
                foreach (var template in new[] { __instance.m_baseHud, __instance.m_baseHudBoss, __instance.m_baseHudPlayer, __instance.m_baseHudMount })
                {
                    AddFriendlyBar(template);
                }

                if (__instance.m_baseHudMount == null)
                    return;

                var mount = __instance.m_baseHudMount.transform;
                var stamina = mount.Find("Stamina");
                var health = mount.Find("Health");
                if (stamina == null || health == null)
                    return;

                if (stamina.Find("stamina_fast") == null && stamina.Find("health_fast") != null)
                    stamina.Find("health_fast").name = "stamina_fast";
                if (stamina.Find("stamina_slow") == null && stamina.Find("health_slow") != null)
                    stamina.Find("health_slow").name = "stamina_slow";

                var nameText = mount.Find("Name")?.GetComponent<TextMeshProUGUI>();
                if (nameText == null)
                    return;

                AddBarText(stamina, "StaminaText", nameText);
                AddBarText(health, "HealthText", nameText);
            }

            private static void AddFriendlyBar(GameObject template)
            {
                var health = template != null ? template.transform.Find("Health") : null;
                var fast = health != null ? health.Find("health_fast") : null;
                if (fast == null || health.Find("health_fast_friendly") != null)
                    return;

                var friendly = Object.Instantiate(fast.gameObject, health, false);
                friendly.name = "health_fast_friendly";
                friendly.transform.SetSiblingIndex(fast.GetSiblingIndex() + 1);
                foreach (var image in friendly.GetComponentsInChildren<Image>(true))
                {
                    // The same bar in the green vanilla uses for tamed animals and allies.
                    image.color = new Color(0.35f, 0.8f, 0.35f, image.color.a);
                }

                friendly.SetActive(false);
            }

            private static void AddBarText(Transform bar, string name, TextMeshProUGUI template)
            {
                if (bar.Find(name) != null)
                    return;

                var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                go.transform.SetParent(bar, false);
                var rt = (RectTransform)go.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(100f, 14f);
                var text = go.GetComponent<TextMeshProUGUI>();
                text.font = template.font;
                text.fontSharedMaterial = template.fontSharedMaterial;
                text.color = template.color;
                text.fontSize = 10f;
                text.alignment = TextAlignmentOptions.Center;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.text = "";
            }
        }

        [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.ShowHud))]
        public static class EnemyHud_ShowHud_Patch
        {
            public static void Postfix(EnemyHud __instance, Character c)
            {
                if (c == null || !__instance.m_huds.TryGetValue(c, out EnemyHud.HudData hudData))
                {
                    return;
                }

                // The character's own entry: dictionary order is not insertion order once huds get removed (1.0).
                var hud = new KeyValuePair<Character, EnemyHud.HudData>(c, hudData);
                if (hud.Key != null && hud.Value != null)
                {
                    const int maxLevelForStarDisplays = 6;
                    const int firstStarDisplayLevel = 2;
                    const int lastStarDisplayLevel = 3;

                    var level = c.GetLevel();
                    if (level > lastStarDisplayLevel)
                    {
                        var hudGui = hud.Value.m_gui;
                        for (var i = firstStarDisplayLevel; i <= maxLevelForStarDisplays; i++)
                        {
                            var levelDisplay = hudGui.transform.Find($"level_{i}");
                            if (levelDisplay != null)
                            {
                                levelDisplay.gameObject.SetActive(i == level);
                            }
                        }

                        var levelDisplayX = hudGui.transform.Find("level_X");
                        if (levelDisplayX)
                        {
                            var useExtendedLevel = level > maxLevelForStarDisplays;
                            if (useExtendedLevel)
                            {
                                var levelXDisplayName = $"level_{level}";
                                var newLevelXDisplay = hudGui.transform.Find(levelXDisplayName);
                                if (newLevelXDisplay == null)
                                {
                                    newLevelXDisplay = Object.Instantiate(levelDisplayX, levelDisplayX.parent, false);
                                    newLevelXDisplay.name = levelXDisplayName;
                                }

                                // Valheim 1.0 port: level_X is an inactive template - look inside it explicitly, label the
                                // clone (not the template) and actually show it.
                                var text = newLevelXDisplay.GetComponentInChildren<Text>(true);
                                if (text != null)
                                {
                                    text.text = $"x {level - 1}";
                                }

                                newLevelXDisplay.gameObject.SetActive(true);
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(EnemyHud), nameof(EnemyHud.UpdateHuds))]
        [HarmonyAfter("org.bepinex.plugins.creaturelevelcontrol")]
        public static class EnemyHud_UpdateHuds_Patch
        {
            public static void Postfix(EnemyHud __instance)
            {
                foreach (var hud in __instance.m_huds)
                {
                    if (hud.Key != null && hud.Value != null && hud.Value.m_gui != null)
                    {
                        var name = hud.Value.m_gui.transform.Find("Name");
                        if (name != null)
                        {
                            var rt = (RectTransform)name;
                            rt.anchoredPosition = new Vector2(0, 38.5f);
                        }
                    }
                }
            }
        }
    }
}
