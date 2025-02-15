using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TabletopGames;

public static class ChiselExtensions
{
    public static MeshData CreateChiseledMesh(this ItemStack chiseledStack, ICoreAPI api)
    {
        ITreeAttribute tree = chiseledStack.Attributes;
        if (tree == null)
        {
            tree = new TreeAttribute();
        }
        int[] materials = BlockEntityMicroBlock.MaterialIdsFromAttributes(tree, api.World);
        uint[] cuboids = (tree["cuboids"] as IntArrayAttribute)?.AsUint;
        if (cuboids == null)
        {
            cuboids = (tree["cuboids"] as LongArrayAttribute)?.AsUint;
        }
        List<uint> voxelCuboids = ((cuboids == null) ? new List<uint>() : new List<uint>(cuboids));
        Block firstblock = api.World.Blocks[materials[0]];
        bool num = firstblock.Attributes?.IsTrue("chiselShapeFromCollisionBox") ?? false;
        uint[] originalCuboids = null;
        if (num)
        {
            Cuboidf[] collboxes = firstblock.CollisionBoxes;
            originalCuboids = new uint[collboxes.Length];
            for (int i = 0; i < collboxes.Length; i++)
            {
                Cuboidf box = collboxes[i];
                originalCuboids[i] = BlockEntityMicroBlock.ToUint((int)(16f * box.X1), (int)(16f * box.Y1), (int)(16f * box.Z1), (int)(16f * box.X2), (int)(16f * box.Y2), (int)(16f * box.Z2), 0);
            }
        }
        MeshData mesh = BlockEntityMicroBlock.CreateMesh(api as ICoreClientAPI, voxelCuboids, materials, null, null, originalCuboids);
        mesh.Rgba.Fill(byte.MaxValue);
        return mesh;
    }

    public static bool ConsumeChiseledBlockAndGiveStack(AdvancedToolMode mode, IPlayer byPlayer, ItemSlot inputSlot)
    {
        JsonItemStack giveStack = mode?.SlotParams?.First()?.GiveStack;
        if (inputSlot.Itemstack.Collectible is not BlockChisel || giveStack == null || !giveStack.Resolve(byPlayer.Entity.World, ""))
        {
            return false;
        }

        ItemStack finalStack = giveStack.ResolvedItemstack.Clone();
        switch (finalStack.Collectible)
        {
            case ItemChiseledPiece itemChiseledPiece:
                {
                    ItemStack removedMouseStack = byPlayer.Entity.Controls.ShiftKey ? inputSlot.TakeOutWhole() : inputSlot.TakeOut(1);
                    finalStack.StackSize = removedMouseStack.StackSize;
                    removedMouseStack.StackSize = 1;
                    ItemChiseledPiece.SetChiseledStack(finalStack, inputStack: removedMouseStack, Vec3i.Zero);
                    break;
                }
            case BlockChiseledBoard blockChiseledBoard:
                {
                    ItemStack removedMouseStack = byPlayer.Entity.Controls.ShiftKey ? inputSlot.TakeOutWhole() : inputSlot.TakeOut(1);
                    finalStack.StackSize = removedMouseStack.StackSize;
                    removedMouseStack.StackSize = 1;
                    BlockChiseledBoard.SetChiseledStack(finalStack, inputStack: removedMouseStack, BlockChiseledBoard.EnumStackType.HitBoxes);
                    break;
                }
            default:
                return false;
        }

        if (!byPlayer.InventoryManager.TryGiveItemstack(finalStack))
        {
            byPlayer.Entity.World.SpawnItemEntity(finalStack, byPlayer.Entity.SidedPos.AsBlockPos);
        }

        inputSlot.MarkDirty();
        byPlayer.InventoryManager.BroadcastHotbarSlot();
        return true;
    }
}
