using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using HarmonyLib;
using UnityEngine;

namespace Auga
{
    /// <summary>
    /// Valheim 1.0 port aid. Auga finds the vanilla UI branches it replaces by path ("root/Player",
    /// "hudroot/KeyHints"...), and those paths are only knowable from the running game. Before any Auga
    /// setup touches a screen this writes the untouched vanilla tree - every transform path with its
    /// components - to BepInEx/AugaPort/&lt;Type&gt;_vanilla.txt, once per type per game start.
    /// Runs only with [Debug] PortDiagnostics on.
    /// </summary>
    [HarmonyPatch]
    public static class PortHierarchyDump
    {
        private static readonly HashSet<string> Dumped = new HashSet<string>();

        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(Hud), "Awake");
            yield return AccessTools.Method(typeof(InventoryGui), "Awake");
            yield return AccessTools.Method(typeof(Minimap), "Awake");
            yield return AccessTools.Method(typeof(Menu), "Start");
            yield return AccessTools.Method(typeof(FejdStartup), "Awake");
            yield return AccessTools.Method(typeof(Chat), "Awake");
            yield return AccessTools.Method(typeof(StoreGui), "Awake");
            yield return AccessTools.Method(typeof(TextInput), "Awake");
            yield return AccessTools.Method(typeof(EnemyHud), "Awake");
            yield return AccessTools.Method(typeof(MessageHud), "Awake");
            yield return AccessTools.Method(typeof(DamageText), "Awake");
            yield return AccessTools.Method(typeof(TextViewer), "Awake");
            yield return AccessTools.Method(typeof(PlayerCustomizaton), "OnEnable");
        }

        // First, and as a prefix: Auga's own prefixes replace whole objects before vanilla Awake runs.
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        private static void Prefix(MonoBehaviour __instance)
        {
            if (Auga.PortDiagnosticsEnabled == null || !Auga.PortDiagnosticsEnabled.Value || __instance == null)
            {
                return;
            }

            var typeName = __instance.GetType().Name;
            if (!Dumped.Add(typeName))
            {
                return;
            }

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"# {typeName} on '{GetPath(__instance.transform, null)}' - Valheim {(global::Version.GetVersionString())}");
                Write(sb, __instance.transform, __instance.transform, 0);
                var dir = Path.Combine(BepInEx.Paths.BepInExRootPath, "AugaPort");
                Directory.CreateDirectory(dir);
                File.WriteAllText(Path.Combine(dir, typeName + "_vanilla.txt"), sb.ToString());
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PortDiagnostics] hierarchy dump of {typeName} failed: {e.Message}");
            }
        }

        private static void Write(StringBuilder sb, Transform root, Transform t, int depth)
        {
            if (depth > 0)
            {
                sb.Append(GetPath(t, root));
                if (!t.gameObject.activeSelf)
                {
                    sb.Append("  (inactive)");
                }

                sb.Append("  [");
                var first = true;
                foreach (var component in t.GetComponents<Component>())
                {
                    if (component == null || component is Transform || component is CanvasRenderer)
                    {
                        continue;
                    }

                    sb.Append(first ? "" : ", ").Append(component.GetType().Name);
                    first = false;
                }

                sb.AppendLine("]");
            }

            for (var i = 0; i < t.childCount; i++)
            {
                Write(sb, root, t.GetChild(i), depth + 1);
            }
        }

        private static string GetPath(Transform t, Transform root)
        {
            var path = t.name;
            for (var p = t.parent; p != null && p != root; p = p.parent)
            {
                path = p.name + "/" + path;
            }

            return path;
        }
    }
}
