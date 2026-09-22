using HarmonyLib;

namespace Auga
{
    [HarmonyPatch]
    public static class TextInput_Setup
    {
        [HarmonyPatch(typeof(TextInput), nameof(TextInput.Awake))]
        public static class TextInput_Awake_Patch
        {
            public static bool Prefix(TextInput __instance)
            {
                return !SetupHelper.DirectObjectReplace(__instance.transform, Auga.Assets.TextInput, "TextInput");
            }

            // Valheim 1.0 port: TextInput.Update no longer polls Enter; vanilla wires the field's submit event
            // to OnInput in its prefab, which Auga's 2023 prefab does not have.
            public static void Postfix(TextInput __instance)
            {
                if (__instance != null && __instance.m_inputField != null && !__instance.name.EndsWith(PortCarryOver.DonorSuffix))
                {
                    __instance.m_inputField.OnInputSubmit.AddListener(_ => __instance.OnInput());
                }
            }
        }
    }
}
