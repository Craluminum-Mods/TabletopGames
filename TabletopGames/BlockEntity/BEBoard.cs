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
/// Represents the basic block entity of a board for tabletop games.
/// Handles inventory, mesh rendering, and item interactions.
/// </summary>
public class BlockEntityBoard : BlockEntityDisplayShapeTexturesFromAttributes
{
    public BlockBoard OwnBlock => Block as BlockBoard;

    public BoardData BoardData
    {
        get
        {
            IBoardDataSupplier supplier = OwnBlock.GetInterface<IBoardDataSupplier>(Api?.World, Pos);
            if (supplier == null) return new BoardData();

            if (Api?.World == null || Pos == null)
            {
                return supplier.GetBoardData(Variants);
            }
            return supplier.GetBoardData(Api.World, Pos);
        }
    }

    public TabletopTags Tags
    {
        get
        {
            IBoardTagsSupplier supplier = OwnBlock.GetInterface<IBoardTagsSupplier>(Api?.World, Pos);
            if (supplier == null) return new TabletopTags();

            if (Api?.World == null || Pos == null)
            {
                return supplier.GetUnresolvedTags(Variants);
            }
            return supplier.GetUnresolvedTags(Api.World, Pos);
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
                ApplyPieceMeshRotation(itemSlot.Itemstack, ref stackMesh);
                mesher.AddMeshData(stackMesh, tfMatrices[i]);
            }
        }

        return base.OnTesselation(mesher, tesselator);
    }

    protected override float[][] genTransformationMatrices()
    {
        Cuboidf[] _selBoxes = GetOrCreateSelectionBoxes();
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

    public override Cuboidf[] GetOrCreateSelectionBoxes(bool forceNew = false)
    {
        if (forceNew || selectionBoxes == null)
        {
            if (BoardData.SlotsHitboxes.Any())
            {
                return selectionBoxes = GetRotatedSelectionBoxes(BoardData.SlotsHitboxes);
            }
            if (BoardData.Size == null)
            {
                return selectionBoxes;
            }

            GenerateSelection();
        }
        return selectionBoxes;
    }

    protected override void GenerateSelection()
    {
        int width = BoardData.Size.X;
        int depth = BoardData.Size.Y;

        selectionBoxes = new Cuboidf[width * depth];

        float paddingLeft = BoardData.Padding.X;
        float paddingTop = BoardData.Padding.Y;
        float paddingRight = BoardData.Padding.Z;
        float paddingBottom = BoardData.Padding.W;

        float slotWidth = (1 - (paddingLeft + paddingRight)) / width;
        float slotDepth = (1 - (paddingTop + paddingBottom)) / depth;

        for (int dx = 0; dx < width; dx++)
        {
            for (int dz = 0; dz < depth; dz++)
            {
                float x1 = paddingLeft + dx * slotWidth;
                float z1 = paddingTop + dz * slotDepth;
                float x2 = x1 + slotWidth;
                float z2 = z1 + slotDepth;

                Cuboidf newCuboid = new Cuboidf()
                {
                    X1 = x1,
                    Y1 = BoardData.SlotMinY,
                    Z1 = z1,
                    X2 = x2,
                    Y2 = BoardData.SlotMaxY,
                    Z2 = z2,
                };

                int index = (dz * width) + dx;
                selectionBoxes[index] = GetRotatedSelectionBox(newCuboid);
            }
        }
    }

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

    public override bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
    {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

        TabletopTags boardTags = TabletopTags.FromInterface(Api.World, Pos, slotId: blockSel.SelectionBoxIndex, resolve: true);
        TabletopTags pieceTags = TabletopTags.FromInterface(slot.Itemstack);
        bool placeable = TabletopTags.AreTagsCompatible(boardTags, pieceTags);

        if (slot.Empty || !placeable)
        {
            return TryTake(byPlayer, blockSel);
        }

        if (placeable)
        {
            AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;
            if (TryPut(byPlayer, slot, blockSel))
            {
                Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                return true;
            }

            return false;
        }

        return false;
    }

    /// <summary>
    /// Attempts to place an item into the board at the selected slot.
    /// </summary>
    /// <param name="byPlayer">The player interacting with the board.</param>
    /// <param name="hotbarSlot">The item slot from the player's inventory.</param>
    /// <param name="blockSel">The block selection containing the clicked slot index.</param>
    /// <returns><c>true</c> if the item was successfully placed, otherwise <c>false</c>.</returns>
    public virtual bool TryPut(IPlayer byPlayer, ItemSlot hotbarSlot, BlockSelection blockSel)
    {
        int index = blockSel.SelectionBoxIndex;
        if (!TryGetSlot(index, out ItemSlot boardSlot) || !boardSlot.Empty)
        {
            return false;
        }
        SetPieceRotation(hotbarSlot.Itemstack, byPlayer);
        int moved = hotbarSlot.TryPutInto(Api.World, boardSlot);
        MarkDirty();
        RemovePieceRotation(hotbarSlot?.Itemstack);
        return moved > 0;
    }

    /// <summary>
    /// Attempts to take an item from the board at the selected slot.
    /// </summary>
    /// <param name="byPlayer">The player attempting to remove the item.</param>
    /// <param name="blockSel">The block selection containing the clicked slot index.</param>
    /// <returns><c>true</c> if an item was taken, otherwise <c>false</c>.</returns>
    public virtual bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
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
        MarkDirty();
        return true;
    }

    public virtual bool TryGetSlot(int index, out ItemSlot slot)
    {
        if (index >= 0 && index < inventory.Count)
        {
            slot = inventory[index];
            return true;
        }

        slot = null;
        return false;
    }

    public virtual void SetPieceRotation(ItemStack stack, IPlayer player)
    {
        if (stack.ItemAttributes.IsTrue("rotateWhenPlacedOnBoard"))
        {
            float rotateYaw = player.Entity.Pos.Yaw;
            stack.Attributes.SetFloat("rotateYaw", rotateYaw);
        }
    }
    
    public virtual void RemovePieceRotation(ItemStack stack)
    {
        stack?.Attributes?.RemoveAttribute("rotateYaw");
    }

    public virtual void ApplyPieceMeshRotation(ItemStack stack, ref MeshData stackMesh)
    {
        if (stack.Attributes.TryGetFloat("rotateYaw") is float rotateYaw)
        {
            stackMesh = stackMesh?.Clone().Rotate(Vec3f.Zero, 0, rotateYaw, 0);
        }
    }
}