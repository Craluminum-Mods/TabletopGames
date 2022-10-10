using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using TabletopGames.ModUtils;
using Vintagestory.API.Datastructures;

namespace TabletopGames
{
    public class BEDominoBoard : BEBoard
    {
        public string woodType;

        public override string InventoryClassName => "ttgdominoboard";

        public BEDominoBoard()
        {
            inventory = new InventoryGeneric(64, "ttgdominoboard-1", Api, (f, f2) => new ItemSlotDominoBoard(f2));
            meshes = new MeshData[64];
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            inventory.LateInitialize("ttgdominoboard-1", api);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            woodType = tree.GetString("wood");
            base.FromTreeAttributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("wood", woodType);
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);

            var clonedItemstack = byItemStack?.Clone();
            if (clonedItemstack == null) return;

            woodType = clonedItemstack.Attributes?.GetString("wood");

            clonedItemstack?.TransferInventory(inventory, Api);

            MarkDirty(true);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.AppendWoodText(wood: woodType);
            dsc.AppendLine().AppendSelectedSlotText(Block, forPlayer, inventory, withSlotId: false, withStackName: true);
        }
    }
}