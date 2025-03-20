
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace TabletopGames;

[HarmonyPatch(typeof(BlockEntityGroundStorage), nameof(BlockEntityGroundStorage.putOrGetItemSingle))]
public static class putOrGetItemSinglePatch
{
    [HarmonyPrefix]
    public static void Prefix(BlockEntityGroundStorage __instance, ItemSlot ourSlot, IPlayer player, BlockSelection bs)
    {
        if (!player.Entity.World.Side.IsServer())
        {
            return;
        }

        bool flipCard = player.Entity.Controls.CtrlKey;
        ItemSlot hotbarSlot = player.InventoryManager.ActiveHotbarSlot;

        if (flipCard && hotbarSlot?.Itemstack?.Collectible is ItemPlayingCard card)
        {
            PlayingCardInventory cardInventory = card.GetInventory(hotbarSlot.Itemstack);
            if (cardInventory.Empty)
            {
                card?.FlipCard(hotbarSlot);
            }
        }
    }
}