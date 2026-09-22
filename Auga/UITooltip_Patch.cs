using AugaUnity;
using HarmonyLib;
using UnityEngine;

namespace Auga
{
    [HarmonyPatch(typeof(UITooltip), nameof(UITooltip.OnHoverStart))]
    public static class UITooltip_OnHoverStart_Patch
    {
        // Valheim 1.0 port: vanilla now keeps the tooltip object alive between hovers. Auga uses two different
        // tooltip prefabs (item-style ComplexTooltip and the simple one), so a leftover of the other kind is dropped.
        public static void Prefix(UITooltip __instance)
        {
            if (UITooltip.m_tooltip == null || __instance.m_tooltipPrefab == null)
            {
                return;
            }

            var liveIsComplex = UITooltip.m_tooltip.GetComponent<ComplexTooltip>() != null;
            var wantedIsComplex = __instance.m_tooltipPrefab.GetComponent<ComplexTooltip>() != null;
            if (liveIsComplex != wantedIsComplex)
            {
                UITooltip.HideTooltip();
            }
        }
    }

    [HarmonyPatch(typeof(UITooltip), nameof(UITooltip.UpdateTextElements))]
    public static class UITooltip_UpdateTextElements_Patch
    {
        public static bool Prefix(UITooltip __instance)
        {
            if (UITooltip.m_tooltip != null)
            {
                var customTooltip = UITooltip.m_tooltip.GetComponent<ComplexTooltip>();
                if (customTooltip != null)
                {
                    var itemTooltip = __instance.GetComponent<ItemTooltip>();
                    if (itemTooltip != null && itemTooltip.Item != null)
                    {
                        customTooltip.SetItem(itemTooltip.Item);
                        return false;
                    }

                    var foodTooltip = __instance.GetComponent<FoodTooltip>();
                    if (foodTooltip != null && foodTooltip.Food != null)
                    {
                        customTooltip.SetFood(foodTooltip.Food);
                        return false;
                    }

                    var statusTooltip = __instance.GetComponent<StatusTooltip>();
                    if (statusTooltip != null && statusTooltip.StatusEffect != null)
                    {
                        customTooltip.SetStatusEffect(statusTooltip.StatusEffect);
                        return false;
                    }

                    var skillTooltip = __instance.GetComponent<SkillTooltip>();
                    if (skillTooltip != null && skillTooltip.Skill != null)
                    {
                        customTooltip.SetSkill(skillTooltip.Skill, __instance);
                        return false;
                    }

                    customTooltip.SetDefault(__instance);
                    // Vanilla's text pass would write into the ComplexTooltip's templates.
                    return false;
                }
            }

            return true;
        }
    }
}
