using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Auga
{
    [HarmonyPatch]
    public static class DamageText_Setup
    {
        [HarmonyPatch(typeof(DamageText), nameof(DamageText.Awake))]
        public static class DamageText_Awake_Patch
        {
            public static bool Prefix(TextInput __instance)
            {
                return !SetupHelper.DirectObjectReplace(__instance.transform, Auga.Assets.DamageText, "DamageText");
            }
        }

        [HarmonyPatch(typeof(DamageText), nameof(DamageText.AddInworldText))]
        [HarmonyPostfix]
        public static void AddInworldText_Postfix(DamageText __instance, DamageText.TextType type, string text, bool mySelf)
        {
            var worldTextInstance = __instance.m_worldTexts.LastOrDefault();
            if (worldTextInstance == null)
            {
                return;
            }

            Color color;
            if (type == DamageText.TextType.Heal)
            {
                color = Auga.Colors.Healing;
            }
            else if (mySelf && type <= DamageText.TextType.Immune)
            {
                // Valheim 1.0: the amount arrives as text; vanilla tests it against "0" the same way.
                color = text != "0" ? Auga.Colors.PlayerDamage : Auga.Colors.PlayerNoDamage;
            }
            else
            {
                switch (type)
                {
                    case DamageText.TextType.Normal:
                        color = Auga.Colors.NormalDamage;
                        break;
                    case DamageText.TextType.Resistant:
                        color = Auga.Colors.ResistDamage;
                        break;
                    case DamageText.TextType.Weak:
                        color = Auga.Colors.WeakDamage;
                        break;
                    case DamageText.TextType.Immune:
                        color = Auga.Colors.ImmuneDamage;
                        break;
                    case DamageText.TextType.TooHard:
                        color = Auga.Colors.TooHard;
                        break;
                    default:
                        // Valheim 1.0 types Auga has no colour for (Bonus, Blocked...): keep what vanilla just set.
                        color = worldTextInstance.m_textField.color;
                        break;
                }
            }
            worldTextInstance.m_textField.color = color;
        }
    }
}
