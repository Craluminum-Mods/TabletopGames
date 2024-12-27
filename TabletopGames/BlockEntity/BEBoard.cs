using System.Collections.Generic;
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
    public override InventoryBase Inventory => inventory;
    public override string InventoryClassName => TabletopConstants.boardInvClassName;
    public override string AttributeTransformCode => OwnBlock.GetAttributeTransformCode(this) ?? base.AttributeTransformCode;

    public Materials Materials { get; protected set; } = new Materials();
    public float MeshAngleRad { get; set; }
    public int QuantitySlots { get; protected set; }

    private MeshData mesh;
    private float[] mat;
    private InventoryBase inventory;

    private Cuboidf[] selectionBoxes;
    public override void Initialize(ICoreAPI api)
    {
        InitInventory();
        base.Initialize(api);
        if (mesh == null)
        {
            Init();
        }
    }

    protected void Init()
    {
        if (Api == null || OwnBlock == null)
        {
            return;
        }

        if (Api.Side == EnumAppSide.Client)
        {
            GetOrCreateSelectionBoxes(forceNew: true);
            mesh = OwnBlock.GetOrCreateMesh(Materials);
            mat = Matrixf.Create().Translate(0.5f, 0.5f, 0.5f).RotateY(MeshAngleRad).Translate(-0.5f, -0.5f, -0.5f).Values;
        }
    }

    public virtual void InitInventory()
    {
        if (inventory == null || inventory.Count == 0)
        {
            inventory = new InventoryGeneric(QuantitySlots, $"{InventoryClassName}-0", null, Api, (slotid, _inv) =>
            {
                return new ItemSlotTabletop(_inv, OwnBlock.TabletopTags, OwnBlock.TabletopTagsIgnored);
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
            Materials = Materials.FromStack(byItemStack);
            QuantitySlots = byItemStack.Attributes.GetAsInt("quantitySlots");
        }
        Init();
        MarkDirty(redrawOnClient: true);
    }

    public override void ToTreeAttributes(ITreeAttribute tree)
    {
        tree.SetInt("quantitySlots", QuantitySlots);
        Materials.ToTreeAttribute(tree);
        tree.SetFloat("meshAngleRad", MeshAngleRad);
        base.ToTreeAttributes(tree);
    }

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
    {
        QuantitySlots = tree.GetInt("quantitySlots");
        Materials = Materials.FromTreeAttribute(tree);
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
                mesher.AddMeshData(getMesh(itemSlot.Itemstack), _tfMatrices[i]);
            }
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

    protected override float[][] genTransformationMatrices()
    {
        Cuboidf[] _selBoxes = GetOrCreateSelectionBoxes();
        float[][] _tfMatrices = new float[DisplayedItems][];

        for (int i = 0; i < DisplayedItems; i++)
        {
            Cuboidf hitbox = _selBoxes[i];
            float x = hitbox.MidX;
            float y = hitbox.MinY;
            float z = hitbox.MidZ;
            _tfMatrices[i] = new Matrixf() .Translate(new Vec3f(x, y, z)).Values;
        }
        return _tfMatrices;
    }

    public Cuboidf[] GetOrCreateSelectionBoxes(bool forceNew = false)
    {
        if (forceNew || selectionBoxes == null)
        {
            float width = (float)OwnBlock.Attributes["width"].AsInt(8);
            float height = (float)OwnBlock.Attributes["height"].AsInt(8);

            selectionBoxes = new Cuboidf[(int)(width * height)];

            for (int dx = 0; dx < width; dx++)
            {
                for (int dz = 0; dz < height; dz++)
                {
                    int num = (dz * (int)height) + dx;

                    Cuboidf newCuboid = new Cuboidf()
                    {
                        X1 = dx / width,
                        Y1 = 0 / 16f,
                        Z1 = dz / height,
                        X2 = (1 + dx) / width,
                        Y2 = 1 / 16f,
                        Z2 = (1 + dz) / height,
                    };

                    selectionBoxes[num] = newCuboid.RotatedCopy(0, MeshAngleRad * GameMath.RAD2DEG, 0, new Vec3d(0.5, 0.5, 0.5));
                }
            }
        }
        return selectionBoxes;
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        int i = forPlayer.CurrentBlockSelection.SelectionBoxIndex;
        if (inventory.Count >= i)
        {
            ItemSlot slot = inventory[i];
            dsc.AppendLine(string.Format(i + ": {0}", slot.Empty ? Lang.Get("Empty") : slot.GetStackName()));
        }

        dsc.AppendLine(Lang.Get("Quantity slots: {0}", QuantitySlots));

        List<string> _langKeys = new();
        if (!Materials.FindByMaterial(OwnBlock?.LangKeysBy, out _langKeys))
        {
            _langKeys = OwnBlock?.LangKeys;
        }
        Materials.GetDescription(dsc, _langKeys, withDebugInfo: true);
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

        bool placeable = OwnBlock.AreTagsCompatible(slot.Itemstack);

        if (slot.Empty || !placeable)
        {
            return TryTake(byPlayer, blockSel);
        }

        if (placeable)
        {
            AssetLocation sound = slot.Itemstack?.Block?.Sounds?.Place;
            if (TryPut(slot, blockSel))
            {
                Api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, true, 16);
                return true;
            }

            return false;
        }

        return false;
    }

    public virtual bool TryPut(ItemSlot slot, BlockSelection blockSel)
    {
        int index = blockSel.SelectionBoxIndex;
        if (index < 0 || index >= inventory.Count) return false;

        if (inventory[index].Empty)
        {
            int moved = slot.TryPutInto(Api.World, inventory[index]);
            MarkDirty(redrawOnClient: true);
            return moved > 0;
        }

        return false;
    }

    public virtual bool TryTake(IPlayer byPlayer, BlockSelection blockSel)
    {
        int index = blockSel.SelectionBoxIndex;
        if (index < 0 || index >= inventory.Count) return false;

        if (!inventory[index].Empty)
        {
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

            MarkDirty(redrawOnClient: true);
            return true;
        }

        return false;
    }
}