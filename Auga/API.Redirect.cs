#if API
// Valheim 1.0 port. Compiled only into AugaAPI.dll (AugaAPI/AugaAPI.csproj, API defined) - the stub other mods
// reference and merge into their own dll. There every API method body is "return null"; this static constructor
// rewrites each of them into a call to the same method of the real Auga.API when Auga is loaded. It replaces the
// external APIManager.dll the 2023 build used (never in the repo) and, unlike the old hand-written API.External.cs,
// is generated from API.cs itself, so the stub and the implementation cannot drift apart.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Auga
{
    public static partial class API
    {
        private static readonly Assembly _targetAssembly;
        private static readonly Dictionary<MethodBase, MethodInfo> _targets = new Dictionary<MethodBase, MethodInfo>();

        static API()
        {
            _targetAssembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Auga");
            var targetType = _targetAssembly?.GetType("Auga.API");
            if (targetType == null)
            {
                return;
            }

            var harmony = new Harmony("mods.randyknapp.auga.API");
            var transpiler = new HarmonyMethod(AccessTools.DeclaredMethod(typeof(API), nameof(Redirect)));
            foreach (var method in typeof(API).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public))
            {
                // Types declared in this stub (PlayerPanelTabData, RequirementWireState...) exist under the same name in Auga.
                var parameters = method.GetParameters().Select(p => MapType(p.ParameterType)).ToArray();
                var target = targetType.GetMethod(method.Name, BindingFlags.Static | BindingFlags.Public, null, parameters, null);
                if (target == null)
                {
                    // An older Auga without this method: the stub's "return null" stays.
                    continue;
                }

                _targets[method] = target;
                harmony.Patch(method, transpiler: transpiler);
            }
        }

        private static Type MapType(Type type)
        {
            if (type.IsArray)
            {
                return MapType(type.GetElementType()).MakeArrayType();
            }

            if (type.Assembly != typeof(API).Assembly)
            {
                return type;
            }

            return _targetAssembly.GetType(type.FullName ?? string.Empty) ?? type;
        }

        private static IEnumerable<CodeInstruction> Redirect(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var count = original.GetParameters().Length;
            for (var i = 0; i < count; i++)
            {
                yield return i == 0 ? new CodeInstruction(OpCodes.Ldarg_0)
                    : i == 1 ? new CodeInstruction(OpCodes.Ldarg_1)
                    : i == 2 ? new CodeInstruction(OpCodes.Ldarg_2)
                    : i == 3 ? new CodeInstruction(OpCodes.Ldarg_3)
                    : new CodeInstruction(OpCodes.Ldarg_S, (byte)i);
            }

            yield return new CodeInstruction(OpCodes.Call, _targets[original]);
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }
}
#endif
