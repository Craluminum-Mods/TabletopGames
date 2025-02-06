
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace TabletopGames;

[HarmonyPatch(typeof(BlockGroundStorage), nameof(BlockGroundStorage.OnBlockInteractStart))]
public static class GroundStorageInteractionFix
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (blockSel == null || world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BlockEntityGroundStorage begs || begs.Inventory == null || begs.Inventory.Empty)
        {
            return true;
        }

        ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        ItemSlot targetSlot = begs.GetSlotAt(blockSel);

        if (hotbarSlot?.Empty == true || targetSlot?.Empty == true) return true;

        if (!byPlayer.Entity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
        {
            world.BlockAccessor.MarkBlockDirty(blockSel.Position.AddCopy(blockSel.Face));
            byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
            return true;
        }

        bool isGroundStorable = hotbarSlot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorGroundStorable>();
        if (!isGroundStorable && (ProcessInWorldCrafting(byPlayer, blockSel, begs, targetSlot) || ProcessContainerInteractions(byPlayer, blockSel, begs, targetSlot)))
        {
            begs.MarkDirty(true);
            __result = true;
            return false;
        }

        if (targetSlot?.Itemstack?.ItemAttributes != null && targetSlot.Itemstack.ItemAttributes.KeyExists("tabletopGames.inWorldCraftingProps"))
        {
            List<CraftingStep> steps = targetSlot.Itemstack.ItemAttributes["tabletopGames.inWorldCraftingProps"].AsObject(defaultValue: new List<CraftingStep>());
            if (steps.Any() && byPlayer.HandleInWorldCrafting(targetSlot, inputSlot: hotbarSlot, null, steps))
            {
                begs.MarkDirty(true);
                __result = true;
                return false;
            }
        }

        return true;
    }

    private static bool ProcessInWorldCrafting(IPlayer byPlayer, BlockSelection blockSel, BlockEntityGroundStorage begs, ItemSlot targetSlot)
    {
        return targetSlot?.Itemstack?.Collectible is ItemIntermediate itemIntermediate && itemIntermediate.OnContainedInteractStart(begs, targetSlot, byPlayer, blockSel);
    }

    private static bool ProcessContainerInteractions(IPlayer byPlayer, BlockSelection blockSel, BlockEntityGroundStorage begs, ItemSlot targetSlot)
    {
        return targetSlot?.Itemstack?.Collectible is ItemContainer itemContainer && itemContainer.OnContainedInteractStart(begs, targetSlot, byPlayer, blockSel);
    }
}