using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;

namespace Auga;

[HarmonyPatch(typeof(SkillsDialog))]
public static class SkillsDialog_Patch
{
    // Stacked [HarmonyPatch] attributes on one method only ever patched one target, so each gets its own prefix.
    [HarmonyPatch(nameof(SkillsDialog.Update))]
    [HarmonyPrefix]
    public static bool Update_Prefix(SkillsDialog __instance)
    {
        return false;
    }

    [HarmonyPatch(nameof(SkillsDialog.OnClose))]
    [HarmonyPrefix]
    public static bool OnClose_Prefix(SkillsDialog __instance)
    {
        return false;
    }

    [HarmonyPatch(nameof(SkillsDialog.SkillClicked))]
    [HarmonyPrefix]
    public static bool SkillClicked_Prefix(SkillsDialog __instance)
    {
        return false;
    }

    public static void SetupOverride(SkillsDialog instance, Player player)
    {
    }
    
    [HarmonyPatch(nameof(SkillsDialog.Setup))]
    [HarmonyTranspiler]
    [HarmonyPriority(Priority.Last)]
    public static IEnumerable<CodeInstruction> Setup_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var instrs = instructions.ToList();

        var counter = 0;

        CodeInstruction LogMessage(CodeInstruction instruction)
        {
            //Debug.LogWarning($"IL_{counter}: Opcode: {instruction.opcode} Operand: {instruction.operand}");
            return instruction;
        }
        
        yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_0));
        counter++;

        yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_1));
        counter++;

        yield return LogMessage(new CodeInstruction(OpCodes.Call,AccessTools.DeclaredMethod(typeof(SkillsDialog_Patch), nameof(SetupOverride))));
        counter++;

        yield return LogMessage(new CodeInstruction(OpCodes.Ret));
        counter++;
    }

    
}