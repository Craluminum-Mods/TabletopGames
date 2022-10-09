using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using TabletopGames.ModUtils;

namespace TabletopGames
{
    public class BEGoBoard : BEBoard
    {
        public int quantitySlots;
        public string woodType;

        public override string InventoryClassName => "ttggoboard";

        public override void Initialize(ICoreAPI api)
        {
            if (inventory == null || inventory.Count == 0)
            {
                inventory = new InventoryGeneric(quantitySlots, "ttggoboard-1", Api, (f, f2) => new ItemSlotGoBoard(f2));
            }

            if (meshes == null || meshes.Length == 0 || meshes.Length != quantitySlots)
            {
                meshes = new MeshData[quantitySlots];
                updateMeshes();
            }

            base.Initialize(api);
            inventory.LateInitialize("ttgboard-1", api);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            quantitySlots = tree.GetInt("quantitySlots");
            woodType = tree.GetString("wood");

            if (inventory == null || inventory.Count == 0)
            {
                inventory = new InventoryGeneric(quantitySlots, "ttggoboard-1", Api, (f, f2) => new ItemSlotGoBoard(f2));
            }

            if (meshes == null || meshes.Length == 0 || meshes.Length != quantitySlots)
            {
                meshes = new MeshData[quantitySlots];
                updateMeshes();
            }

            base.FromTreeAttributes(tree, worldAccessForResolve);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("quantitySlots", quantitySlots);
            tree.SetString("wood", woodType);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.AppendWoodDescription(wood: woodType);
            dsc.AppendLine().AppendSelectedSlotText(Block, forPlayer, inventory, withSlotId: false, withStackName: true);
        }

        public override void OnBlockPlaced(ItemStack byItemStack = null)
        {
            base.OnBlockPlaced(byItemStack);

            var clonedItemstack = byItemStack?.Clone();
            if (clonedItemstack == null) return;

            woodType = clonedItemstack.Attributes?.GetString("wood");
            quantitySlots = clonedItemstack.Attributes.GetAsInt("quantitySlots");

            if (inventory == null || inventory.Count == 0)
            {
                inventory = new InventoryGeneric(quantitySlots, "ttggoboard-1", Api, (f, f2) => new ItemSlotGoBoard(f2));
            }

            if (meshes == null || meshes.Length == 0 || meshes.Length != quantitySlots)
            {
                meshes = new MeshData[quantitySlots];
            }

            clonedItemstack?.SaveInventoryToBlock(inventory, Api);

            updateMeshes();
            MarkDirty(true);
        }
    }
}