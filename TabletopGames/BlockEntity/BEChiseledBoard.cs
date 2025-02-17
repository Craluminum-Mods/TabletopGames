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

public class BlockEntityChiseledBoard : BlockEntityDisplay, IRotatable, IBoardPreviewRendererHelper
{
    public BlockChiseledBoard OwnBlock => Block as BlockChiseledBoard;

    public ItemStack ChiseledStackHitboxes { get; set; }
    public ItemStack ChiseledStackTextures { get; set; }
    public float MeshAngleRad { get; set; }
    public float[] Mat { get; protected set; }

    protected MeshData mesh;
    protected InventoryBase inventory;

    public override InventoryBase Inventory => inventory;

    public override string InventoryClassName => TabletopConstants.boardInvClassName;

    public override string AttributeTransformCode => OwnBlock.AttributeTransformCode;

    public TabletopTags Tags
    {
        get
        {
            IBoardTagsSupplier supplier = OwnBlock.GetInterface<IBoardTagsSupplier>(Api?.World, Pos);
            if (supplier == null) return new TabletopTags();
            return supplier.GetUnresolvedTags(null);
        }
    }

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

    protected void Init()
    {
        if (Api == null || OwnBlock == null) return;

        if (Api.Side == EnumAppSide.Client)
        {
            GetOrCreateSelectionBoxes(forceNew: true);
            mesh = OwnBlock.GetOrCreateMesh(this);
            Mat = Matrixf.Create().Translate(0.5f, 0.5f, 0.5f).RotateY(MeshAngleRad).Translate(-0.5f, -0.5f, -0.5f).Values;
        }
    }

    protected void InitInventory()
    {
        // hardcoded amount of slots because when initializing the inventory, ChiseledStackHitboxes is always null for some reason
        int quantitySlots = 256;
        if (inventory == null || inventory.Count == 0)
        {
            inventory = new InventoryGeneric(quantitySlots, $"{InventoryClassName}-1", null, Api, (slotid, _inv) =>
            {
                return new ItemSlotTabletop(_inv, EnumSlotType.Random, Tags);
            });
        }
    }

    public override void OnBlockPlaced(ItemStack byItemStack = null)
    {
        base.OnBlockPlaced(byItemStack);
        InitInventory();
        Init();
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        base.ToTreeAttributes(tree);

        tree.SetFloat("meshAngleRad", MeshAngleRad);
        tree.SetItemstack(BlockChiseledBoard.ChiseledStackHitboxesAttributeName, ChiseledStackHitboxes);
        tree.SetItemstack(BlockChiseledBoard.ChiseledStackTexturesAttributeName, ChiseledStackTextures);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        MeshAngleRad = tree.GetFloat("meshAngleRad");

        ChiseledStackHitboxes = tree.GetItemstack(BlockChiseledBoard.ChiseledStackHitboxesAttributeName);
        ChiseledStackTextures = tree.GetItemstack(BlockChiseledBoard.ChiseledStackTexturesAttributeName);
        ChiseledStackHitboxes?.ResolveBlockOrItem(worldForResolving);
        ChiseledStackTextures?.ResolveBlockOrItem(worldForResolving);

        InitInventory();

        base.FromTreeAttributes(tree, worldForResolving);
        Init();
        RedrawAfterReceivingTreeAttributes(worldForResolving);
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
    {
        mesher.AddMeshData(mesh, Mat);

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

            tfMatrices = genTransformationMatrices();
        }
    }

    protected override string getMeshCacheKey(ItemStack stack)
    {
        return $"{AttributeTransformCode}-{base.getMeshCacheKey(stack)}";
    }

    protected override float[][] genTransformationMatrices()
    {
        float[][] _tfMatrices = new float[DisplayedItems][];
        if (_tfMatrices.Length == 0) return _tfMatrices;

        Cuboidf[] _selBoxes = GetBehavior<BEBehaviorChiseledBoardSelection>()?.GetOrCreateSelectionBoxes();
        if (_selBoxes == null || !_selBoxes.Any()) return _tfMatrices;

        for (int i = 0; i < DisplayedItems; i++)
        {
            if (_selBoxes.Length <= i) continue;

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

        foreach (BlockEntityBehavior behavior in Behaviors)
        {
            behavior.GetBlockInfo(forPlayer, dsc);
        }
    }

    public MeshData GetOrCreateMesh(ItemStack stack, int index) => getOrCreateMesh(stack, index);
    public float[][] GenTransformationMatrices() => genTransformationMatrices();

    protected void GetOrCreateSelectionBoxes(bool forceNew = false) => GetBehavior<BEBehaviorBoardSelection>()?.GetOrCreateSelectionBoxes(forceNew);

    public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation,
        Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
    {
        MeshAngleRad = tree.GetFloat("meshAngleRad");
        MeshAngleRad -= degreeRotation * GameMath.DEG2RAD;
        tree.SetFloat("meshAngleRad", MeshAngleRad);
    }
}