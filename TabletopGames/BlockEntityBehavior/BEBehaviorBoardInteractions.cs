using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TabletopGames;

public class BEBehaviorBoardInteractions : BlockEntityBehavior, IInteractable
{
    private IInventory Inventory => (Blockentity as IBlockEntityContainer)?.Inventory;

    public TabletopTags Tags
    {
        get
        {
            IBoardTagsSupplier supplier = Block?.GetInterface<IBoardTagsSupplier>(Api?.World, Pos);
            if (supplier == null) return new TabletopTags();
            return supplier.GetUnresolvedTags(Api?.World, Pos);
        }
    }

    public BEBehaviorBoardInteractions(BlockEntity blockentity) : base(blockentity) { }

    public bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
    {
        handling = EnumHandling.PreventDefault;
        return OnInteract(byPlayer, blockSel);
    }

    private bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
    {
        ItemSlot slot = byPlayer.Entity.RightHandItemSlot;

        TabletopTags boardTags = Tags.GetResolvedTags(blockSel.SelectionBoxIndex);
        TabletopTags pieceTags = TabletopTags.FromInterface(slot);
        bool placeable = TabletopTags.AreTagsCompatible(boardTags, pieceTags);

        if (slot.Empty || !placeable)
        {
            return TryTake(byPlayer, blockSel);
        }
        if (placeable)
        {
            bool flip = TryTake(byPlayer, blockSel);
            AssetLocation? sound = slot.Itemstack?.Block?.Sounds?.Place;
            if (TryPut(byPlayer, slot, blockSel))
            {
                if (!flip)
                {
                    Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                }
                return true;
            }
        }
        return false;
    }

    public bool TryPut(IPlayer byPlayer, ItemSlot hotbarSlot, BlockSelection blockSel)
    {
        int index = blockSel.SelectionBoxIndex;
        if (!TryGetSlot(index, out ItemSlot boardSlot) || !boardSlot.Empty)
        {
            return false;
        }

        SetPieceRotation(hotbarSlot.Itemstack, byPlayer);
        int moved = hotbarSlot.TryPutInto(Api.World, boardSlot);
        Blockentity.MarkDirty();
        RemovePieceRotation(hotbarSlot?.Itemstack);
        return moved > 0;
    }

    public bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
    {
        int index = blockSel.SelectionBoxIndex;
        if (!TryGetSlot(index, out ItemSlot boardSlot) || boardSlot.Empty)
        {
            return false;
        }

        ItemStack stack = boardSlot.TakeOut(1);
        if (byPlayer.InventoryManager.TryGiveItemstack(stack))
        {
            AssetLocation sound = stack.Block?.Sounds?.Place;
            Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
        }
        if (stack.StackSize > 0)
        {
            Api.World.SpawnItemEntity(stack, Pos);
        }
        Blockentity.MarkDirty();
        return true;
    }

    public bool TryGetSlot(int index, out ItemSlot slot)
    {
        if (index >= 0 && index < Inventory?.Count)
        {
            slot = Inventory[index];
            return true;
        }
        slot = null;
        return false;
    }

    public static void SetPieceRotation(ItemStack stack, IPlayer player)
    {
        if (stack.ItemAttributes.IsTrue("rotateWhenPlacedOnBoard"))
        {
            float rotateYaw = player.Entity.Pos.Yaw;
            stack.Attributes.SetFloat("rotateYaw", rotateYaw);
        }
    }

    public static void RemovePieceRotation(ItemStack stack)
    {
        stack?.Attributes?.RemoveAttribute("rotateYaw");
    }

    public static void ApplyPieceMeshRotation(ItemStack stack, ref MeshData stackMesh)
    {
        if (stack.Attributes.TryGetFloat("rotateYaw") is float rotateYaw)
        {
            stackMesh = stackMesh?.Clone().Rotate(Vec3f.Zero, 0, rotateYaw, 0);
        }
    }
}
