
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace TabletopGames;

[HarmonyPatch(typeof(BlockGroundStorage), nameof(BlockGroundStorage.OnBlockInteractStart))]
public static class GroundStorageInteractionFix
{
    [HarmonyPrefix]
    public static bool Prefix(ref bool __result, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (blockSel == null || world.BlockAccessor.GetBlockEntity(blockSel.Position) is not BlockEntityGroundStorage begs)
        {
            return true;
        }

        if (!byPlayer.Entity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
        {
            world.BlockAccessor.MarkBlockDirty(blockSel.Position.AddCopy(blockSel.Face));
            byPlayer.InventoryManager.ActiveHotbarSlot.MarkDirty();
            return true;
        }

        ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        ItemSlot targetSlot = begs.GetSlotAt(blockSel);

        if (hotbarSlot.Empty
            || targetSlot.Empty
            || hotbarSlot.Itemstack.Collectible.HasBehavior<CollectibleBehaviorGroundStorable>()
            || targetSlot?.Itemstack?.Collectible is not ItemIntermediate itemIntermediate)
        {
            return true;
        }

        if (itemIntermediate.OnContainedInteractStart(begs, targetSlot, byPlayer, blockSel))
        {
            begs.MarkDirty(true);
            __result = true;
            return false;
        }
        return true;
    }
}