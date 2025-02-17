using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TabletopGames;

/// <summary>
/// Handles inventory and rendering of board pieces
/// </summary>
public class BlockEntityBoard : BlockEntityDisplayShapeTexturesFromAttributes, IBoardPreviewRendererHelper
{
    public BlockBoard OwnBlock => Block as BlockBoard;

    public BoardData BoardData
    {
        get
        {
            IBoardDataSupplier supplier = OwnBlock.GetInterface<IBoardDataSupplier>(Api?.World, Pos);
            if (supplier == null) return new BoardData();
            return supplier.GetBoardData(Variants);
        }
    }

    public TabletopTags Tags
    {
        get
        {
            IBoardTagsSupplier supplier = OwnBlock.GetInterface<IBoardTagsSupplier>(Api?.World, Pos);
            if (supplier == null) return new TabletopTags();
            return supplier.GetUnresolvedTags(Variants);
        }
    }

    public override InventoryBase Inventory => inventory;

    public override string InventoryClassName => TabletopConstants.boardInvClassName;

    public override string AttributeTransformCode => BoardData.AttributeTransformCode;

    protected override void InitInventory()
    {
        if (inventory == null || inventory.Count == 0)
        {
            inventory = new InventoryGeneric(BoardData.QuantitySlots, $"{InventoryClassName}-1", null, Api, (slotId, _inv) =>
            {
                TabletopTags tags = Tags.GetResolvedTags(slotId);
                EnumSlotType slotType = EnumSlotType.Normal;

                if (BoardData.SlotTypes.Any())
                {
                    string id = slotId.ToString();
                    foreach ((string wildcard, EnumSlotType _slotType) in BoardData.SlotTypes)
                    {
                        if (WildcardUtil.Match(wildcard, id))
                        {
                            slotType = _slotType;
                            break;
                        }
                    }
                }
                return new ItemSlotTabletop(_inv, slotType, tags);
            });
        }
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
    {
        for (int i = 0; i < DisplayedItems; i++)
        {
            ItemSlot itemSlot = Inventory[i];
            if (!itemSlot.Empty && tfMatrices != null)
            {
                MeshData stackMesh = getMesh(itemSlot.Itemstack);
                BEBehaviorBoardInteractions.ApplyPieceMeshRotation(itemSlot.Itemstack, ref stackMesh);
                mesher.AddMeshData(stackMesh, tfMatrices[i]);
            }
        }

        return base.OnTesselation(mesher, tesselator);
    }

    protected override float[][] genTransformationMatrices()
    {
        Cuboidf[] _selBoxes = GetBehavior<BEBehaviorBoardSelection>()?.GetOrCreateSelectionBoxes();
        float[][] _tfMatrices = new float[DisplayedItems][];

        if (_selBoxes == null || !_selBoxes.Any()) return _tfMatrices;

        for (int i = 0; i < DisplayedItems; i++)
        {
            Cuboidf hitbox = _selBoxes[i] ??= new Cuboidf();
            float x = hitbox.MidX;
            float z = hitbox.MidZ;

            float extraY = 0.03125f;
            float offset = (hitbox.Y1 / extraY) + 1f;
            float y = hitbox.Y1 - (extraY * offset) + hitbox.Y2;

            _tfMatrices[i] = new Matrixf().Translate(new Vec3f(x, y, z)).Values;
        }
        return _tfMatrices;
    }

    protected override void GetOrCreateSelectionBoxes(bool forceNew = false) => GetBehavior<BEBehaviorBoardSelection>().GetOrCreateSelectionBoxes(forceNew);

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        int index = forPlayer.CurrentBlockSelection.SelectionBoxIndex;
        if (inventory.Count > index)
        {
            ItemSlot slot = inventory[index];

            int displayedIndex = TabletopDebug.BoardDataDebugInfo ? index : index + 1;
            dsc.Append(displayedIndex + ": ");

            if (slot?.Itemstack?.Collectible?.GetCollectibleInterface<IContainedCustomName>() is IContainedCustomName containedCustomName)
            {
                dsc.Append($"{slot.StackSize}x ");
                dsc.AppendLine(containedCustomName.GetContainedInfo(slot));
            }
            else
            {
                dsc.AppendLine(slot.Empty ? Lang.Get("Empty") : $"{slot.StackSize}x " + slot.GetStackName());
            }
        }

        BoardData.GetDescription(dsc, index);
        foreach (BlockEntityBehavior behavior in Behaviors)
        {
            behavior.GetBlockInfo(forPlayer, dsc);
        }
    }
}