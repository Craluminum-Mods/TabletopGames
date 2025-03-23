
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace TabletopGames;

[HarmonyPatch(typeof(BlockEntityGroundStorage), nameof(BlockEntityGroundStorage.putOrGetItemSingle))]
public static class PutOrGetItemSinglePatch
{
    [HarmonyPrefix]
    public static void Prefix(BlockEntityGroundStorage __instance, ItemSlot ourSlot, IPlayer player, BlockSelection bs)
    {
        if (!player.Entity.World.Side.IsServer())
        {
            return;
        }

        ItemSlot hotbarSlot = player.Entity.RightHandItemSlot;
        ItemPlayingCard.SetCardRotation(blockEntityGroundStorage: __instance, player, hotbarSlot);
        ItemPlayingCard.TryFlipCard(hotbarSlot, player);
    }
}