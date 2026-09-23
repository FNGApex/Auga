using System.Collections.Generic;
using AugaUnity;
using HarmonyLib;
using UnityEngine;

namespace Auga
{
    [HarmonyPatch(typeof(Game), nameof(Game.SpawnPlayer))]
    public static class AugaLog_Hooks
    {
        public static void Postfix(Game __instance)
        {
            if (__instance.m_firstSpawn)
            {
                AugaMessageLog.instance.AddArrivalLog(Player.m_localPlayer);
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnDeath))]
    public static class Player_OnDeath_Patch
    {
        public static void Postfix(Player __instance)
        {
            var hitTracker = __instance.RequireComponent<LastHitTracker>();
            var lastHit = hitTracker != null ? hitTracker.LastHit : null;
            AugaMessageLog.instance.AddDeathLog(__instance, lastHit);
        }
    }

    [HarmonyPatch(typeof(TombStone), nameof(TombStone.UpdateDespawn))]
    public static class TombStone_UpdateDespawn_Patch
    {
        public static bool Prefix(TombStone __instance)
        {
            if (__instance.m_nview.IsValid() && __instance.IsOwner())
            {
                if (!__instance.m_container.IsInUse() && __instance.m_container.GetInventory().NrOfItems() <= 0)
                {
                    AugaMessageLog.instance.AddTombstoneLog(Player.m_localPlayer);
                }
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.AddKnownItem))]
    public static class Player_AddKnownItem_Patch
    {
        public static bool Prefix(Player __instance, ItemDrop.ItemData item)
        {
            if (__instance.m_knownMaterial.Contains(item.m_shared.m_name))
            {
                return true;
            }

            if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Material)
            {
                AugaMessageLog.instance.AddNewMaterialLog(item);
            }
            else if (item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Trophy)
            {
                AugaMessageLog.instance.AddNewTrophyLog(item);
            }
            else
            {
                AugaMessageLog.instance.AddNewItemLog(item);
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.UpdateKnownRecipesList))]
    public static class Player_UpdateKnownRecipesList_Patch
    {
        public static bool Prefix(Player __instance)
        {
            // Valheim 1.0 port (harmony-10): vanilla returns early without a Game instance; mirror it.
            if (Game.instance == null || ObjectDB.instance == null)
            {
                return true;
            }

            var currentSeason = __instance.m_currentSeason;
            var newRecipes = new List<Recipe>();
            foreach (var recipe in ObjectDB.instance.m_recipes)
            {
                // Valheim 1.0 port (harmony-10): mirror 1.0's conditions - seasonal recipes count as enabled, and skip recipes with a null m_item.
                var seasonal = currentSeason != null && currentSeason.Recipes.Contains(recipe);
                if ((recipe.m_enabled || seasonal) && recipe.m_item != null && !__instance.m_knownRecipes.Contains(recipe.m_item.m_itemData.m_shared.m_name) && __instance.HaveRequirements(recipe, true, 0))
                {
                    newRecipes.Add(recipe);
                }
            }

            if (newRecipes.Count > 0)
            {
                AugaMessageLog.instance.AddNewRecipesLog(newRecipes);
            }

            var pieceTables = new List<PieceTable>();
            var newPieces = new List<Piece>();
            __instance.m_inventory.GetAllPieceTables(pieceTables);
            foreach (var pieceTable in pieceTables)
            {
                foreach (var gameObject in pieceTable.m_pieces)
                {
                    var piece = gameObject.GetComponent<Piece>();
                    // Valheim 1.0 port (harmony-10): seasonal pieces count as enabled, as in 1.0's UpdateKnownRecipesList.
                    var seasonal = currentSeason != null && currentSeason.Pieces.Contains(gameObject);
                    if (piece != null && (piece.m_enabled || seasonal) && !__instance.m_knownRecipes.Contains(piece.m_name) && __instance.HaveRequirements(piece, Player.RequirementMode.IsKnown))
                    {
                        newPieces.Add(piece);
                    }
                }
            }

            if (newPieces.Count > 0)
            {
                AugaMessageLog.instance.AddNewPieceLog(newPieces);
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.AddKnownStation))]
    public static class Player_AddKnownStation_Patch
    {
        public static bool Prefix(Player __instance, CraftingStation station)
        {
            if (!__instance.m_knownStations.ContainsKey(station.m_name))
            {
                AugaMessageLog.instance.AddNewStationLog(station);
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.AddKnownBiome))]
    public static class Player_AddKnownBiome_Patch
    {
        public static bool Prefix(Player __instance, BiomeSector biome)
        {
            // Valheim 1.0 port (harmony-15): 1.0 keys known biomes by sector name (alt-biome sectors have their own name) and
            // suppresses the plain Meadows/None discovery; log the sector name under the same condition vanilla shows its banner.
            if (biome != null && !__instance.IsBiomeKnown(biome)
                && ((biome.Biome != Heightmap.Biome.Meadows && biome.Biome != Heightmap.Biome.None) || biome.AltBiomes.Count > 0))
            {
                AugaMessageLog.instance.AddNewBiomeLog(biome.GetName(), biome.Biome);
            }

            return true;
        }
    }

    //Teleport(Player player)
    [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Teleport))]
    public static class TeleportWorld_Teleport_Patch
    {
        public static void Postfix(TeleportWorld __instance, Player player)
        {
            if (!__instance.TargetFound() || !player.IsTeleportable(__instance.m_allowAllItems))
            {
                return;
            }

            AugaMessageLog.instance.AddTeleportLog(__instance.GetText(), player);
        }
    }

    // Valheim 1.0 port (harmony-11): the old prefix copied DoCrafting's eligibility checks, which drifted from 1.0 (upgrader
    // stations, NoCraftCost, multicraft, CanAddItem, requireOnlyOneIngredient), so some crafts were never logged. Log from the
    // actual outcome instead: compare the inventory before and after vanilla runs.
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.DoCrafting))]
    public static class InventoryGui_DoCrafting_Patch
    {
        public class CraftState
        {
            public Recipe Recipe;
            public ItemDrop.ItemData UpgradeItem;
            public int UpgradeQuality;
            public int CountBefore;
        }

        public static void Prefix(InventoryGui __instance, Player player, out CraftState __state)
        {
            __state = null;
            var recipe = __instance.m_craftRecipe;
            if (recipe == null || recipe.m_item == null || player == null)
            {
                return;
            }

            var upgradeItem = __instance.m_craftUpgradeItem;
            __state = new CraftState
            {
                Recipe = recipe,
                UpgradeItem = upgradeItem,
                UpgradeQuality = upgradeItem?.m_quality ?? 0,
                CountBefore = upgradeItem == null ? player.GetInventory().CountItems(recipe.m_item.m_itemData.m_shared.m_name, -1, false) : 0
            };
        }

        public static void Postfix(Player player, CraftState __state)
        {
            if (__state == null || player == null || AugaMessageLog.instance == null)
            {
                return;
            }

            var inventory = player.GetInventory();
            if (__state.UpgradeItem != null)
            {
                // Upgrades replace the item in the same slot; log only when the quality actually went up (upgrader stations can fail or break it).
                if (inventory.ContainsItem(__state.UpgradeItem))
                {
                    return;
                }

                var pos = __state.UpgradeItem.m_gridPos;
                var upgraded = inventory.GetItemAt(pos.x, pos.y);
                if (upgraded != null && upgraded.m_shared.m_name == __state.UpgradeItem.m_shared.m_name && upgraded.m_quality > __state.UpgradeQuality)
                {
                    AugaMessageLog.instance.AddUpgradeItemLog(upgraded, upgraded.m_quality);
                }

                return;
            }

            var crafted = inventory.CountItems(__state.Recipe.m_item.m_itemData.m_shared.m_name, -1, false) - __state.CountBefore;
            if (crafted > 0)
            {
                AugaMessageLog.instance.AddCraftItemLog(__state.Recipe, crafted);
            }
        }
    }

    //RaiseSkill
    [HarmonyPatch(typeof(Player), nameof(Player.RaiseSkill))]
    public static class Player_RaiseSkill_Patch
    {
        public static Skills.SkillType Skill;
        public static int LevelBefore;

        public static bool Prefix(Player __instance, Skills.SkillType skill)
        {
            Skill = skill;
            LevelBefore = Mathf.FloorToInt(__instance.m_skills.GetSkill(skill)?.m_level ?? 0);
            return true;
        }

        public static void Postfix(Player __instance, Skills.SkillType skill)
        {
            if (Skill != skill)
            {
                return;
            }

            var levelNow = Mathf.FloorToInt(__instance.m_skills.GetSkill(skill)?.m_level ?? 0);
            if (LevelBefore != levelNow)
            {
                AugaMessageLog.instance.AddSkillUpLog(skill, levelNow);
            }
        }
    }

    //Character.ShowPickupMessage
    [HarmonyPatch(typeof(Character), nameof(Character.ShowPickupMessage))]
    public static class Character_ShowPickupMessage_Patch
    {
        public static void Postfix(ItemDrop.ItemData item, int amount)
        {
            if (item != null && amount > 0)
            {
                AugaMessageLog.instance.AddItemPickupLog(item, amount);
            }
        }
    }
}
