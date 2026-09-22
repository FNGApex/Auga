using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using AugaUnity;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Auga
{
    public class StoreMethods
    {
        public StoreGui SetupAugaStoreGui(StoreGui instance)
        {
            if (instance.name.StartsWith("Auga")) return instance;
            
            // AUDIT2 misc-4: decide before building anything, so a refused or failed swap leaves no orphan Auga canvas.
            if (!instance.transform.name.Equals("Store_Screen") || instance.m_rootPanel == null || !instance.m_rootPanel.name.Equals("Store"))
            {
                Debug.LogWarning($"[Auga] StoreGui '{instance.name}' is not the vanilla store screen, leaving it vanilla");
                return instance;
            }

            StoreGui newStoreGui = null;
            try
            {
                var originalTransform = instance.transform;
                var parent = originalTransform.parent;
                newStoreGui = GetAugaStoreGui(parent);
                newStoreGui.transform.SetAsLastSibling();
                // Valheim 1.0 port: every screen root is its own canvas now; without one the shop is never drawn.
                PortCarryOver.WrapInCanvas(newStoreGui.gameObject, originalTransform.gameObject);

                originalTransform.gameObject.SetActive(false);
                return newStoreGui;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Auga] Error In Store, leaving the vanilla store: {e}");
                if (newStoreGui != null)
                {
                    var canvasHolder = newStoreGui.transform.parent != null && newStoreGui.transform.parent.name.EndsWith("_AugaCanvas")
                        ? newStoreGui.transform.parent.gameObject
                        : newStoreGui.gameObject;
                    Object.Destroy(canvasHolder);
                }
            }

            return instance;
        }

        private StoreGui GetAugaStoreGui(Transform parent)
        {
            var newStore = Object.Instantiate(Auga.Assets.StoreGui, parent, false);
            var newStoreGui = newStore.GetComponent<StoreGui>();
            
            newStoreGui.m_coinPrefab = ObjectDB.instance.GetItemPrefab("Coins").GetComponent<ItemDrop>();
            newStoreGui.transform.Find("Store").gameObject.AddComponent<MovableHudElement>().Init(TextAnchor.UpperLeft, 140, -180);

            return newStoreGui;
        }
        
    }
    
    [HarmonyPatch]
    public class Store_Setup
    {

        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.Awake))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> Awake_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGenerator)
        {
            var instrs = instructions.ToList();

            var counter = 0;

            CodeInstruction LogMessage(CodeInstruction instruction)
            {
                //Debug.LogWarning($"IL_{counter}: Opcode: {instruction.opcode} Operand: {instruction.operand}");
                return instruction;
            }

            for (int i = 0; i < instrs.Count; ++i)
            {
                if (i == 0)
                {
                    yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_0));
                    counter++;
                    
                    yield return LogMessage(new CodeInstruction(OpCodes.Ldarg_0));
                    counter++;
                    
                    yield return LogMessage(new CodeInstruction(OpCodes.Call, AccessTools.DeclaredMethod(typeof(StoreMethods), nameof(StoreMethods.SetupAugaStoreGui))));
                    counter++;
                    
                    //skip next this;
                    i++;

                }
                
                yield return LogMessage(instrs[i]);
                counter++;
            }
        }

        [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.FillList))]
        [HarmonyPostfix]
        public static void FillList_Postfix(StoreGui __instance)
        {
            if (Auga.HasBetterTrader)
            {
                return;
            }

            var items = __instance.m_trader.GetAvailableItems();
            for (var index = 0; index < __instance.m_itemList.Count; index++)
            {
                var item = items[index];
                var itemElement = __instance.m_itemList[index];
                // Valheim 1.0 port: shop entries that grant a player key have no item prefab.
                var itemTooltip = itemElement.GetComponent<ItemTooltip>();
                if (itemTooltip != null)
                {
                    itemTooltip.Item = item.m_prefab != null ? item.m_prefab.m_itemData : null;
                }
            }
        }
    }
}
