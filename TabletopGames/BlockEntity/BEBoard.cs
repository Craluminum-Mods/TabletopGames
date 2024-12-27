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
    public override string InventoryClassName => Constants.boardInvClassName;

    public Materials Materials { get; protected set; } = new Materials();
    public MeshData Mesh { get; protected set; }
    public float MeshAngleRad { get; set; }
    public int QuantitySlots { get; protected set; }

    private InventoryBase inventory;
    private float[] mat;

    public override void Initialize(ICoreAPI api)
    {
        InitInventory();
        base.Initialize(api);
        if (Mesh == null)
        {
            Init();
        }
        inventory.LateInitialize($"{InventoryClassName}-0", api);
    }

    protected void Init()
    {
        if (Api == null || OwnBlock == null)
        {
            return;
        }

        if (Api.Side == EnumAppSide.Client)
        {
            Mesh = OwnBlock.GetOrCreateMesh(Materials);
            mat = Matrixf.Create().Translate(0.5f, 0.5f, 0.5f).RotateY(MeshAngleRad).Translate(-0.5f, -0.5f, -0.5f).Values;
        }
    }

    public virtual void InitInventory()
    {
        if (inventory == null || inventory.Count == 0)
        {
            inventory = new InventoryGeneric(QuantitySlots, $"{InventoryClassName}-0", null, Api, OnNewSlot);
        }
    }

    public virtual ItemSlot OnNewSlot(int slotId, InventoryGeneric self)
    {
        return new ItemSlotTabletop(self, OwnBlock.TabletopTags, OwnBlock.TabletopTagsIgnored);
    }

    public void ReplaceProperties(Materials materials)
    {
        Materials = materials;
        MarkDirty(redrawOnClient: true);
    }

    public override void OnBlockUnloaded()
    {
        base.OnBlockUnloaded();
        Mesh?.Dispose();
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

    public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
    {
        QuantitySlots = tree.GetInt("quantitySlots");
        Materials = Materials.FromTreeAttribute(tree);
        MeshAngleRad = tree.GetFloat("meshAngleRad");
        InitInventory();
        base.FromTreeAttributes(tree, worldAccessForResolve);
        RedrawAfterReceivingTreeAttributes(worldAccessForResolve);
    }

    public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tesselator)
    {
        mesher.AddMeshData(Mesh, mat);
        base.OnTesselation(mesher, tesselator);
        return true;
    }

    public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
    {
        base.GetBlockInfo(forPlayer, dsc);
        Materials.GetDescription(dsc, OwnBlock?.LangKeys, withDebugInfo: true);
        dsc.AppendLine(Lang.Get("Quantity slots: {0}", QuantitySlots));
    }

    public void OnTransformed(IWorldAccessor worldAccessor, ITreeAttribute tree, int degreeRotation,
        Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, EnumAxis? flipAxis)
    {
        MeshAngleRad = tree.GetFloat("meshAngleRad");
        MeshAngleRad -= degreeRotation * GameMath.DEG2RAD;
        tree.SetFloat("meshAngleRad", MeshAngleRad);
    }

    protected override float[][] genTransformationMatrices()
    {
        return System.Array.Empty<float[]>();
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
            MarkDirty();
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

            MarkDirty();
            return true;
        }

        return false;
    }
}