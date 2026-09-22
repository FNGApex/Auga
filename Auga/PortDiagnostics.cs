using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Auga
{
    /// <summary>
    /// Valheim 1.0 port aid. Auga swaps whole branches of the vanilla UI for its own prefabs and then
    /// re-points the fields of the vanilla components (Hud, InventoryGui...) at the new objects. Fields the
    /// game gained after Auga 1.3.12 are not re-pointed, so if they referenced something inside a replaced
    /// branch they now reference a destroyed object and throw the first time vanilla code touches them.
    /// After each screen's setup this logs every such field once, so the missing prefab work is a list
    /// instead of a hunt through null reference exceptions. [Debug] PortDiagnostics switches it off. Writes
    /// through Unity's log directly: Auga's own logging is disabled by default.
    /// </summary>
    [HarmonyPatch]
    public static class PortDiagnostics
    {
        private static readonly HashSet<Type> Reported = new HashSet<Type>();

        private static IEnumerable<MethodBase> TargetMethods()
        {
            // AUDIT2 hud-4: the 1.0 build menu is skinned in place; its serialized fields are worth a look too.
            var buildUi = AccessTools.Method(typeof(BuildUi), "Awake") ?? AccessTools.Method(typeof(BuildUi), "Start");
            if (buildUi != null)
            {
                yield return buildUi;
            }

            yield return AccessTools.Method(typeof(Hud), "Awake");
            yield return AccessTools.Method(typeof(InventoryGui), "Awake");
            yield return AccessTools.Method(typeof(Minimap), "Start");
            yield return AccessTools.Method(typeof(Menu), "Start");
            yield return AccessTools.Method(typeof(FejdStartup), "Start");
            yield return AccessTools.Method(typeof(Chat), "Awake");
            yield return AccessTools.Method(typeof(StoreGui), "Awake");
            yield return AccessTools.Method(typeof(TextInput), "Awake");
            yield return AccessTools.Method(typeof(EnemyHud), "Awake");
            yield return AccessTools.Method(typeof(MessageHud), "Start");
            yield return AccessTools.Method(typeof(DamageText), "Awake");
            yield return AccessTools.Method(typeof(TextViewer), "Awake");
            yield return AccessTools.Method(typeof(KeyHints), "Awake");
        }

        // Last, so every Auga setup postfix on the same method has already run.
        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(MonoBehaviour __instance)
        {
            if (Auga.PortDiagnosticsEnabled == null || !Auga.PortDiagnosticsEnabled.Value || __instance == null)
            {
                return;
            }

            var type = __instance.GetType();
            if (!Reported.Add(type))
            {
                return;
            }

            var destroyed = new List<string>();
            var unassigned = new List<string>();
            // AUDIT2 hud-4: PortCarryOver never destroys the vanilla object, it hides it as a donor - so a field that was
            // filled from (or left pointing into) the donor is a live object the player can never see.
            var stranded = new List<string>();
            for (var t = type; t != null && t != typeof(MonoBehaviour); t = t.BaseType)
            {
                foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    if (!typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType))
                    {
                        continue;
                    }

                    // Only what Unity serializes: these are the references a prefab is expected to provide.
                    if (!field.IsPublic && field.GetCustomAttribute<SerializeField>() == null)
                    {
                        continue;
                    }

                    var value = field.GetValue(__instance);
                    if ((UnityEngine.Object)value != null)
                    {
                        var transform = value is Component component ? component.transform : (value as GameObject)?.transform;
                        if (transform != null && InDonor(transform))
                        {
                            stranded.Add(field.Name);
                        }

                        continue;
                    }

                    // A destroyed Unity object is still a live C# reference that compares equal to null.
                    (ReferenceEquals(value, null) ? unassigned : destroyed).Add(field.Name);
                }
            }

            Debug.LogWarning($"[PortDiagnostics] {type.Name}: {destroyed.Count} destroyed, {unassigned.Count} unassigned UI references");
            if (destroyed.Count > 0)
            {
                Debug.LogWarning($"[PortDiagnostics]   destroyed (pointed into a branch Auga replaced): {string.Join(", ", destroyed)}");
            }

            if (unassigned.Count > 0)
            {
                Debug.LogWarning($"[PortDiagnostics]   unassigned (may be fine - vanilla leaves some empty): {string.Join(", ", unassigned)}");
            }

            if (stranded.Count > 0)
            {
                Debug.LogWarning($"[PortDiagnostics]   stranded in a hidden vanilla donor (Auga has no counterpart yet): {string.Join(", ", stranded)}");
            }
        }

        private static bool InDonor(Transform transform)
        {
            for (var t = transform; t != null; t = t.parent)
            {
                if (t.name.EndsWith(PortCarryOver.DonorSuffix))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
