using AugaUnity;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Auga
{
    [HarmonyPatch]
    public static class MessageHud_Setup
    {
        [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.Awake))]
        [HarmonyPrefix]
        public static bool MessageHud_Awake_Prefix(MessageHud __instance)
        {
            return !SetupHelper.IndirectTwoObjectReplace(__instance.transform, Auga.Assets.MessageHud, "HudMessage", "TopLeftMessage", "AugaMessageHud");
        }

        [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.Awake))]
        [HarmonyPostfix]
        public static void MessageHud_Awake_Postfix(MessageHud __instance)
        {
            if (__instance == null)
                return;

            var controller = __instance.GetComponent<AugaTopLeftMessageController>();
            if (controller)
                controller.LogContainer.parent.gameObject.AddComponent<MovableHudElement>().Init(TextAnchor.UpperLeft, 55, -115);
            __instance.m_messageCenterText.gameObject.AddComponent<MovableHudElement>().Init(TextAnchor.MiddleCenter, 0, 150);

            // AUDIT2 text-5: 1.0 puts the item / station name in UnlockDescription, which Auga styles as a faint 20pt grey
            // parenthetical with an auto-size floor of 1 over a 56% backdrop. Lift it (in the loaded prefab, so every instance).
            var unlock = __instance.m_unlockMsgPrefab;
            if (unlock != null)
            {
                var description = unlock.transform.Find("UnlockMessage/UnlockDescription")?.GetComponent<TMP_Text>();
                if (description != null)
                {
                    description.color = new Color(0.84f, 0.81f, 0.78f, 1f);
                    if (description.enableAutoSizing && description.fontSizeMin < 16f)
                        description.fontSizeMin = 16f;
                }

                var backdrop = unlock.transform.Find("UnlockMessage/bkg")?.GetComponent<Image>();
                if (backdrop != null && backdrop.color.a < 0.75f)
                    backdrop.color = new Color(backdrop.color.r, backdrop.color.g, backdrop.color.b, 0.8f);
            }

            // AUDIT2 text-6: the biome banner clip fades a CanvasGroup on 'Title', which Auga's prefab lacks, so the name
            // popped in and out at full alpha. Add the component before the Animator binds (it binds on instantiate).
            var biomeTitle = __instance.m_biomeFoundPrefab != null ? __instance.m_biomeFoundPrefab.transform.Find("UnlockMessage/Title") : null;
            if (biomeTitle != null && biomeTitle.GetComponent<CanvasGroup>() == null)
                biomeTitle.gameObject.AddComponent<CanvasGroup>();
        }

        [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.ShowMessage))]
        [HarmonyPostfix]
        public static void MessageHud_ShowMessage_Postfix(MessageHud __instance, MessageHud.MessageType type, string text, int amount, Sprite icon)
        {
            if (Hud.IsUserHidden())
            {
                return;
            }

            text = Localization.instance.Localize(text);
            if (type == MessageHud.MessageType.TopLeft)
            {
                var controller = __instance.GetComponent<AugaTopLeftMessageController>();
                controller.AddMessage(text, icon, amount);
            }
        }
    }
}
