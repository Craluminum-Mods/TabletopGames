using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.Datastructures;
using System.Text;
using TabletopGames.ModUtils;
using System.Collections.Generic;
using System.Linq;

namespace TabletopGames
{
    public class BEDominoBox : BlockEntityContainer
    {
        internal InventoryGeneric inventory;

        public int quantitySlots = 28;
        public string woodType;

        public override InventoryBase Inventory => inventory;
        public override string InventoryClassName => "ttgdominobox";

        public BEDominoBox()
        {
            inventory = new InventoryGeneric(quantitySlots, "ttgdominobox-1", Api, (f, f2) => new ItemSlotDominoBoard(f2));
        }

        public bool TryPutAllDomino(IPlayer byPlayer)
        {
            foreach (var slot in inventory)
            {
                if (!slot.Empty) continue;

                foreach (var fromSlot in byPlayer.InventoryManager.ActiveHotbarSlot.Inventory)
                {
                    if (fromSlot.Empty || fromSlot.Itemstack == null) continue;
                    if (fromSlot.Itemstack.Collectible is not ItemDominoPiece) continue;

                    fromSlot.TryPutInto(Api.World, slot);
                    fromSlot.MarkDirty();
                    slot.MarkDirty();
                }
            }

            MarkDirty(true);
            return true;
        }

        public bool TryTakeRandomDomino(IPlayer byPlayer, IWorldAccessor world)
        {
            if (inventory.Empty) return false;

            var nonEmptySlots = new List<ItemSlot>();

            foreach (var slot in inventory)
            {
                if (slot.Empty || slot.Itemstack == null) continue;
                nonEmptySlots.Add(slot);
            }

            var randomSlotId = world.Rand.Next(0, nonEmptySlots.Count - 1);
            var randomSlot = nonEmptySlots[randomSlotId];

            ItemStack stack = randomSlot.TakeOut(1);
            if (!byPlayer.InventoryManager.TryGiveItemstack(stack, true))
            {
                Api.World.SpawnItemEntity(stack, byPlayer.Entity.BlockSelection.Position.ToVec3d().Add(0.5, 0.5, 0.5));
            }

            randomSlot.MarkDirty();
            MarkDirty(true);
            return true;
        }

        public bool MakeDominoTypesUnique()
        {
            List<string> typeSet = new();

            if (typeSet != null)
            {
                typeSet?.Clear();

                if (quantitySlots == 28)
                {
                    const int n = 7;
                    for (int i = 0; i < n; i++)
                    {
                        for (int j = 0; j <= i; j++)
                        {
                            typeSet.Add($"{i}_{j}");
                        }
                    }
                }
                if (quantitySlots == 55)
                {
                    const int n = 10;
                    for (int i = 0; i < n; i++)
                    {
                        for (int j = 0; j <= i; j++)
                        {
                            typeSet.Add($"{i}_{j}");
                        }
                    }
                }
            }

            if (HasEmptySlots(inventory)) return false;

            foreach (var slot in inventory)
            {
                var slotid = inventory.GetSlotId(slot);
                slot.Itemstack.Attributes.SetString("type", typeSet[slotid]);
            }

            MarkDirty(true);
            return true;
        }

        public static bool HasEmptySlots(InventoryBase inv) => inv.Any(slot => slot.Empty);

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

            quantitySlots = clonedItemstack.Attributes?.GetAsInt("quantitySlots") ?? quantitySlots;

            clonedItemstack?.SaveInventoryToBlock(inventory, Api);
            MarkDirty(true);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.AppendWoodDescription(woodType);
            dsc.AppendFormat("Slots: {0}", inventory.Count);
        }
    }
}