using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TabletopGames;

/// <summary>
/// <para> Renders shape and textures using attribute based type system. </para>
/// <para> Used for boards. </para>
/// <para> Has rotation. </para>
/// <para> Has "automatic" localization. </para>
/// <para> Has inventory and displays stored items. </para>
/// </summary>
public class BlockEntityBoard : BlockEntityDisplay, IRotatable
{
    public BlockBoard OwnBlock => Block as BlockBoard;
    public BoardData BoardData => OwnBlock?.GetBoardData(Variants);

    public override InventoryBase Inventory => inventory;
    public override string InventoryClassName => TabletopConstants.boardInvClassName;
    public override string AttributeTransformCode => BoardData.AttributeTransformCode;

    public Variants Variants { get; protected set; } = new Variants();
    public float MeshAngleRad { get; set; }

    private MeshData mesh;
    private float[] mat;
    private InventoryBase inventory;
    private Cuboidf[] selectionBoxes;

    public override void Initialize(ICoreAPI api)
    {
        InitInventory();
        base.Initialize(api);
        inventory.LateInitialize($"{InventoryClassName}-1", api);
        if (mesh == null)
        {
            Init();
        }
    }

    protected virtual void Init()
    {
        if (Api == null || OwnBlock == null)
        {
            return;
        }

        if (Api.Side == EnumAppSide.Client)
        {
            GetOrCreateSelectionBoxes(forceNew: true);
            mesh = OwnBlock.GetOrCreateMesh(Variants);
            mat = Matrixf.Create().Translate(0.5f, 0.5f, 0.5f).RotateY(MeshAngleRad).Translate(-0.5f, -0.5f, -0.5f).Values;
        }
    }

    protected virtual void InitInventory()
    {
        if (inventory == null || inventory.Count == 0)
        {
            inventory = new InventoryGeneric(BoardData.QuantitySlots, $"{InventoryClassName}-1", null, Api, (slotid, _inv) =>
            {
                return OwnBlock.CreateSlot(Variants, _inv, slotid);
            });
        }
    }

    public override void OnBlockUnloaded()
    {
        base.OnBlockUnloaded();
        mesh?.Dispose();
        selectionBoxes = null;
    }

    public override void OnBlockRemoved()
    {
        base.OnBlockRemoved();
        selectionBoxes = null;
    }

    public override void OnBlockPlaced(ItemStack byItemStack = null)
    {
        base.OnBlockPlaced(byItemStack);
        if (byItemStack != null)
        {
            Variants = Variants.FromStack(byItemStack);
        }

        InitInventory();
        Init();
        MarkDirty(redrawOnClient: true);
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);
        Variants.ToTreeAttribute(tree);
        tree.SetFloat("meshAngleRad", MeshAngleRad);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        Variants = Variants.FromTreeAttribute(tree);
        MeshAngleRad = tree.GetFloat("meshAngleRad");

        InitInventory();

        base.FromTreeAttributes(tree, worldForResolving);
        RedrawAfterReceivingTreeAttributes(worldForResolving);
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
    {
        mesher.AddMeshData(mesh, mat);

        float[][] _tfMatrices = genTransformationMatrices();

        for (int i = 0; i < DisplayedItems; i++)
        {
            ItemSlot itemSlot = Inventory[i];
            if (!itemSlot.Empty && _tfMatrices != null)
            {
                MeshData stackMesh = getMesh(itemSlot.Itemstack);
                ApplyPieceMeshRotation(itemSlot, ref stackMesh);
                mesher.AddMeshData(stackMesh, _tfMatrices[i]);
            }
        }

        foreach (BlockEntityBehavior behavior in Behaviors)
        {
            behavior.OnTesselation(mesher, tesselator);
        }

        return true;
    }

    public override void updateMeshes()
    {
        for (int i = 0; i < DisplayedItems; i++)
        {
            updateMesh(i);
        }
    }

    protected override string getMeshCacheKey(ItemStack stack)
    {
        return $"{AttributeTransformCode}-{base.getMeshCacheKey(stack)}";
    }

    protected override float[][] genTransformationMatrices()
    {
        Cuboidf[] _selBoxes = GetOrCreateSelectionBoxes();
        float[][] _tfMatrices = new float[DisplayedItems][];

        for (int i = 0; i < DisplayedItems; i++)
        {
            Cuboidf hitbox = _selBoxes[i] ??= new Cuboidf();
            float x = hitbox.MidX;
            float y = hitbox.MinY;
            float z = hitbox.MidZ;
            _tfMatrices[i] = new Matrixf().Translate(new Vec3f(x, y, z)).Values;
        }
        return _tfMatrices;
    }

    public virtual Cuboidf[] GetOrCreateSelectionBoxes(bool forceNew = false)
    {
        if (forceNew || selectionBoxes == null)
        {
            if (BoardData.SlotsHitboxes.Any())
            {
                return selectionBoxes = BoardData.SlotsHitboxes.Select(x => x.RotatedCopy(0, MeshAngleRad * GameMath.RAD2DEG, 0, new Vec3d(0.5, 0.5, 0.5))).ToArray();
            }
            if (BoardData.Size == null)
            {
                return selectionBoxes;
            }

            GenerateSelection();
        }
        return selectionBoxes;
    }
    
    public virtual void SetSelectionBoxes(Cuboidf[] cuboids)
    {
        selectionBoxes = cuboids;
    }

    protected virtual void GenerateSelection()
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
                selectionBoxes[index] = newCuboid.RotatedCopy(0, MeshAngleRad * GameMath.RAD2DEG, 0, new Vec3d(0.5, 0.5, 0.5));
            }
        }
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        int index = forPlayer.CurrentBlockSelection.SelectionBoxIndex;
        if (inventory.Count > index)
        {
            ItemSlot slot = inventory[index];
            int displayedIndex = TabletopDebug.TagsDebugInfo ? index : index + 1;
            if (slot?.Itemstack?.Collectible?.GetCollectibleInterface<IContainedCustomName>() is IContainedCustomName containedCustomName)
            {
                dsc.AppendLine(containedCustomName.GetContainedInfo(slot));
            }
            else
            {
                dsc.AppendLine(string.Format(displayedIndex + ": {0}", slot.Empty ? Lang.Get("Empty") : slot.GetStackName()));
            }
        }

        if (TabletopDebug.TagsDebugInfo && inventory.Count > index) // twice check to avoid constant iterations in ByType
        {
            OwnBlock.GetTags(Variants, index, resolve: false)?.GetDescription(dsc, index, verbose: true);
        }

        BoardData.GetDescription(dsc, index);
        foreach (BlockEntityBehavior behavior in Behaviors)
        {
            behavior.GetBlockInfo(forPlayer, dsc);
        }
    }

    public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation,
        Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
    {
        MeshAngleRad = tree.GetFloat("meshAngleRad");
        MeshAngleRad -= degreeRotation * GameMath.DEG2RAD;
        tree.SetFloat("meshAngleRad", MeshAngleRad);
    }

    public virtual bool OnInteract(IPlayer byPlayer, BlockSelection blockSel)
    {
        ItemSlot slot = byPlayer.InventoryManager.ActiveHotbarSlot;

        TabletopTags boardTags = OwnBlock.GetTags(Variants, slotId: blockSel.SelectionBoxIndex);
        bool placeable = TabletopTags.AreTagsCompatible(boardTags, slot.Itemstack);

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

    public virtual bool TryPut(IPlayer byPlayer, ItemSlot slot, BlockSelection blockSel)
    {
        int index = blockSel.SelectionBoxIndex;
        if (index < 0 || index >= inventory.Count || !inventory[index].Empty)
        {
            return false;
        }
        SetPieceRotation(slot, byPlayer);
        int moved = slot.TryPutInto(Api.World, inventory[index]);
        MarkDirty();
        RemovePieceRotation(slot);
        return moved > 0;
    }

    public virtual bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
    {
        int index = blockSel.SelectionBoxIndex;
        if (index < 0 || index >= inventory.Count || inventory[index].Empty)
        {
            return false;
        }

        ItemStack stack = inventory[index].TakeOut(1);
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

    public virtual void SetPieceRotation(ItemSlot slot, IPlayer player)
    {
        if (slot.Itemstack.ItemAttributes.KeyExists("rotateWhenPlacedOnBoard"))
        {
            float rotateYaw = player.Entity.Pos.Yaw;
            slot.Itemstack.Attributes.SetFloat("rotateYaw", rotateYaw);
        }
    }
    
    public virtual void RemovePieceRotation(ItemSlot slot)
    {
        slot?.Itemstack?.Attributes?.RemoveAttribute("rotateYaw");
    }

    public virtual void ApplyPieceMeshRotation(ItemSlot slot, ref MeshData stackMesh)
    {
        if (slot.Itemstack.Attributes.HasAttribute("rotateYaw"))
        {
            float rotateYaw = slot.Itemstack.Attributes.GetFloat("rotateYaw");
            stackMesh = stackMesh?.Clone().Rotate(Vec3f.Zero, 0, rotateYaw, 0);
        }
    }
}