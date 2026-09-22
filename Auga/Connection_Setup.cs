using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Auga
{
    [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
    public static class ZNet_Awake_Patch
    {
        [UsedImplicitly]
        public static void Postfix(ZNet __instance)
        {
            // Valheim 1.0 port: hidden donors instead of destroyed objects (see PortCarryOver), each dialog under its own parent.
            var passwordParent = __instance.m_passwordDialog.parent;
            var passwordDonor = PortCarryOver.MakeDonor(__instance.m_passwordDialog.gameObject);
            var newPasswordDialog = PortCarryOver.InstantiateFilled(Auga.Assets.PasswordDialog, passwordParent, passwordDonor);
            newPasswordDialog.gameObject.SetActive(false);
            __instance.m_passwordDialog = newPasswordDialog.GetComponent<RectTransform>();

            var connectingParent = __instance.m_connectingDialog.parent;
            var connectingDonor = PortCarryOver.MakeDonor(__instance.m_connectingDialog.gameObject);
            var newConnectingDialog = PortCarryOver.InstantiateFilled(Auga.Assets.ConnectingDialog, connectingParent, connectingDonor);
            newConnectingDialog.gameObject.SetActive(false);
            __instance.m_connectingDialog = newConnectingDialog.GetComponent<RectTransform>();
        }
    }
}
