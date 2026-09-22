using System.Linq;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Auga
{
    [HarmonyPatch]
    public static class Chat_Setup
    {
        [HarmonyPatch(typeof(Chat), nameof(Chat.Awake))]
        public static class Chat_Awake_Patch
        {
            public static bool Prefix(Chat __instance)
            {
                if (Auga.HasChatter)
                    return true;

                if (!Auga.AugaChatShow.Value )
                    return true;

                return !SetupHelper.IndirectTwoObjectReplace(__instance.transform, Auga.Assets.AugaChat, "Chat", "Chat_box", "AugaChat");
            }

            public static void Postfix(Chat __instance)
            {
                if (Auga.HasChatter)
                    return;

                if (!Auga.AugaChatShow.Value )
                    return;

                if (__instance.name.EndsWith(PortCarryOver.DonorSuffix))
                    return;

                if (__instance.m_input != null)
                    __instance.m_input.transform.parent.gameObject.AddComponent<MovableHudElement>().Init(TextAnchor.LowerRight, 0, 67);

                // AUDIT2 text-7: the small NpcDialog/Text (Haldor, Hildir, RandomSpeak) is a stretched rect with a positive
                // width delta (766x0 under a 450x120 blob), so the line drew past both edges of its backdrop. Match NpcDialogLarge.
                var smallText = __instance.m_npcTextBase != null ? __instance.m_npcTextBase.transform.Find("Text") as RectTransform : null;
                if (smallText != null && smallText.anchorMin == Vector2.zero && smallText.anchorMax == Vector2.one && smallText.sizeDelta.x > 0f)
                    smallText.sizeDelta = new Vector2(-35f, -30f);

                // AUDIT2 text-8: the 'Topic' children are legacy UI.Text, so Chat's Find("Topic").GetComponent<TextMeshProUGUI>()
                // left m_topicField null (the topic is drawn inline anyway). Swap them for inactive TMP objects.
                ReplaceLegacyTopic(__instance.m_npcTextBase);
                ReplaceLegacyTopic(__instance.m_npcTextBaseLarge);
            }

            private static void ReplaceLegacyTopic(GameObject dialog)
            {
                var topic = dialog != null ? dialog.transform.Find("Topic") : null;
                if (topic == null || topic.GetComponent<TMPro.TMP_Text>() != null)
                    return;

                var legacy = topic.GetComponent<UnityEngine.UI.Text>();
                var replacement = new GameObject("Topic", typeof(RectTransform), typeof(CanvasRenderer), typeof(TMPro.TextMeshProUGUI));
                replacement.transform.SetParent(dialog.transform, false);
                replacement.transform.SetSiblingIndex(topic.GetSiblingIndex());
                var rt = (RectTransform)replacement.transform;
                var old = (RectTransform)topic;
                rt.anchorMin = old.anchorMin;
                rt.anchorMax = old.anchorMax;
                rt.pivot = old.pivot;
                rt.anchoredPosition = old.anchoredPosition;
                rt.sizeDelta = old.sizeDelta;
                var text = replacement.GetComponent<TMPro.TextMeshProUGUI>();
                SettingsSkin.Load();
                SettingsSkin.SetFont(text, SettingsSkin._bold);
                text.fontSize = legacy != null ? legacy.fontSize : 18f;
                text.alignment = TMPro.TextAlignmentOptions.Center;
                if (ColorUtility.TryParseHtmlString(Auga.Colors.Topic, out var topicColor))
                    text.color = topicColor;
                replacement.SetActive(false);
                topic.name = "Topic_Legacy";
                Object.Destroy(topic.gameObject);
            }
        }

        [HarmonyPatch(typeof(Chat), nameof(Chat.SetNpcText))]
        public static class Chat_SetNpcText_Patch
        {
            public static void Postfix(Chat __instance)
            {
                if (Auga.HasChatter)
                    return;

                if (!Auga.AugaChatShow.Value)
                    return;

                var latestChatMessage = __instance.m_npcTexts.LastOrDefault();
                if (latestChatMessage != null)
                {
                    var text = latestChatMessage.m_textField.text;
                    text = text.Replace("<color=orange>", $"<color={Auga.Colors.Topic}>");
                    text = text.Replace("<color=yellow>", $"<color={Auga.Colors.Emphasis}>");
                    latestChatMessage.m_textField.text = text;
                }
            }
        }
    }
}