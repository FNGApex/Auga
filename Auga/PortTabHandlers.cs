using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

namespace Auga
{
    /// <summary>
    /// Valheim 1.0 port (widgets-6 / pause-texts-14). 1.0's TabHandler reads the Tab key outside the
    /// m_gamepadInput guard, gated only by the new m_tabKeyInput, whose C# default is true. Auga's prefabs were
    /// serialized in 2023 without that field, so every Auga TabHandler (compendium, new-character panel, main
    /// menu, settings) would cycle tabs on Tab. Before 1.0 the Tab key only worked when m_gamepadInput was on,
    /// so that is carried over: m_tabKeyInput = m_gamepadInput.
    /// Auga's TabHandlers are recognised by editing the bundle's prefab assets (the GameObjects held in
    /// <see cref="AugaAssets"/>), never live instances, so vanilla's own TabHandlers are untouched and every
    /// later Instantiate of an Auga screen inherits the value.
    /// </summary>
    [HarmonyPatch(typeof(TabHandler), "Start")]
    public static class PortTabHandlers
    {
        private static bool _applied;

        // Runs while Harmony patches the plugin, i.e. right after Auga.LoadAssets and before any Auga screen exists.
        [UsedImplicitly]
        public static void Prepare()
        {
            ApplyToAugaPrefabs();
        }

        // Fallback in case the assets were not loaded yet at patch time; one bool check once applied.
        [HarmonyPrefix]
        [UsedImplicitly]
        public static void Prefix()
        {
            ApplyToAugaPrefabs();
        }

        public static void ApplyToAugaPrefabs()
        {
            if (_applied || Auga.Assets == null || Auga.Assets.MenuPrefab == null)
            {
                return;
            }

            _applied = true;
            var count = 0;
            foreach (var field in typeof(AugaAssets).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                if (field.FieldType != typeof(GameObject))
                {
                    continue;
                }

                var prefab = field.GetValue(Auga.Assets) as GameObject;
                if (prefab == null)
                {
                    continue;
                }

                foreach (var tabHandler in prefab.GetComponentsInChildren<TabHandler>(true))
                {
                    tabHandler.m_tabKeyInput = tabHandler.m_gamepadInput;
                    count++;
                }
            }

            Auga.Log($"Valheim 1.0 port: set m_tabKeyInput from m_gamepadInput on {count} Auga TabHandler(s)");
        }
    }
}
