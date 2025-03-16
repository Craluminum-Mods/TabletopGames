using System;
using System.Collections.Generic;
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
/// Implements stacking behavior. Renders shape and textures using attribute based type system.
/// </summary>
public class ItemPlayingCard : Item, IContainedInteractable, IContainedMeshSource, IContainedCustomName
{
    public Dictionary<string, List<object>> NameByType { get; protected set; } = new();
    public Dictionary<string, List<object>> DescriptionByType { get; protected set; } = new();
    public Dictionary<string, List<object>> ContainedDescriptionByType { get; protected set; } = new();
    public Dictionary<string, int> QuantitySlotsByType { get; protected set; } = new();
    public Dictionary<string, float> StackingTranslatonByType { get; protected set; } = new();

    protected Dictionary<string, CompositeShape> shapeByType = new();
    protected Dictionary<string, Dictionary<string, CompositeTexture>> texturesByType = new();

    private float[] predefinedRotations = new float[64] {
    -0.0167f, -0.0980f,  0.0650f, -0.0403f, -0.0263f, -0.0613f,  0.0132f, -0.0677f,
    -0.0751f, -0.0134f,  0.0124f, -0.0651f,  0.0106f, -0.0290f,  0.0916f, -0.0817f,
     0.0957f, -0.0176f,  0.0008f, -0.0704f,  0.0438f, -0.0620f, -0.0317f, -0.0953f,
    -0.0321f,  0.0935f,  0.0958f,  0.0489f, -0.0993f,  0.0880f,  0.0742f,  0.0542f,
    -0.0642f, -0.0801f, -0.0171f,  0.0771f,  0.0156f,  0.0473f, -0.0535f,  0.0047f,
     0.0419f,  0.0650f,  0.0614f, -0.0535f,  0.0746f, -0.0567f,  0.0604f,  0.0110f,
    -0.0628f,  0.0177f,  0.0036f,  0.0917f, -0.0917f, -0.0672f,  0.0967f,  0.0664f,
    -0.0699f, -0.0542f,  0.0078f, -0.0686f, -0.0352f, -0.0901f,  0.0423f, -0.0843f
    };

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        LoadTypes();
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        base.OnUnloaded(api);
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "TabletopGames_ItemPlayingCard_MeshRefs");
        meshRefs?.Foreach(meshRef => meshRef.Value?.Dispose());
        ObjectCacheUtil.Delete(api, "TabletopGames_ItemPlayingCard_MeshRefs");
    }

    public void LoadTypes()
    {
        if (Attributes != null)
        {
            NameByType = Attributes["name"].AsObject(defaultValue: new Dictionary<string, List<object>>());
            DescriptionByType = Attributes["description"].AsObject(defaultValue: new Dictionary<string, List<object>>());
            ContainedDescriptionByType = Attributes["containedDescription"].AsObject(defaultValue: new Dictionary<string, List<object>>());

            shapeByType = Attributes["shape"].AsObject(defaultValue: new Dictionary<string, CompositeShape>());
            texturesByType = Attributes["textures"].AsObject(defaultValue: new Dictionary<string, Dictionary<string, CompositeTexture>>());

            QuantitySlotsByType = Attributes["quantitySlots"].AsObject(defaultValue: new Dictionary<string, int>());
            StackingTranslatonByType = Attributes["stackingTranslaton"].AsObject(defaultValue: new Dictionary<string, float>());
        }
    }

    public override bool Equals(ItemStack thisStack, ItemStack otherStack, params string[] ignoreAttributeSubTrees)
    {
        ignoreAttributeSubTrees ??= Array.Empty<string>();
        ignoreAttributeSubTrees = ignoreAttributeSubTrees.Append("rotateYaw");
        ignoreAttributeSubTrees = ignoreAttributeSubTrees.Append("rotateY");
        ignoreAttributeSubTrees = ignoreAttributeSubTrees.Append("scale");

        if (thisStack.Id == otherStack.Id && IsEmpty(thisStack) && IsEmpty(otherStack))
        {
            ignoreAttributeSubTrees = ignoreAttributeSubTrees.Append("slots");
        }

        return base.Equals(thisStack, otherStack, ignoreAttributeSubTrees);
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.GetOrCreate(capi, "TabletopGames_ItemPlayingCard_MeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());

        string key = ((IContainedMeshSource)this).GetMeshCacheKey(itemstack);

        if (!meshRefs.TryGetValue(key, out MultiTextureMeshRef meshref) || TabletopDebug.ItemRotations)
        {
            MeshData mesh = ((IContainedMeshSource)this).GenMesh(itemstack, capi.ItemTextureAtlas, null);
            meshref = capi.Render.UploadMultiTextureMesh(mesh);
            meshRefs[key] = meshref;
        }

        renderinfo.ModelRef = meshref;
        renderinfo.NormalShaded = true;

        base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
    }

    public override string GetHeldItemName(ItemStack itemStack)
    {
        Variants variants = Variants.FromStack(itemStack);
        variants.FindByVariant(NameByType, out List<object> _langKeys);
        string defaultName = base.GetHeldItemName(itemStack);
        return variants.GetName(_langKeys, defaultName);
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        StringBuilder invDsc = GetInventoryInfo(inSlot);
        if (invDsc.Length > 0)
        {
            dsc.Append(invDsc);
            dsc.AppendLine();
        }

        if (Code != null && Code.Domain != "game")
        {
            Mod mod = api.ModLoader.GetMod(Code.Domain);
            dsc.AppendLine(Lang.Get("Mod: {0}", mod?.Info.Name ?? Code.Domain));
        }

        Variants variants = Variants.FromStack(inSlot.Itemstack);
        variants.FindByVariant(DescriptionByType, out List<object> _langKeys);
        variants.GetDescription(dsc, _langKeys);
    }

    public MeshData GetOrCreateMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
    {
        MeshData containerMesh = GenContainerMesh(itemstack, targetAtlas);
        if (GenContentMesh(itemstack, targetAtlas) is MeshData contentMesh && contentMesh != null)
        {
            containerMesh.AddMeshData(contentMesh);
        }
        return containerMesh;
    }

    public MeshData GenContainerMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
    {
        ICoreClientAPI capi = api as ICoreClientAPI;
        MeshData mesh = new MeshData(4, 3);

        Variants variants = Variants.FromStack(itemstack);
        variants.FindByVariant(shapeByType, out CompositeShape _shape);
        if (_shape == null) return mesh;

        CompositeShape rcshape = _shape.Clone();
        rcshape.Base.Path = variants.ReplacePlaceholders(rcshape.Base.Path);
        rcshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");

        Shape shape = capi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();

        variants.FindByVariant(texturesByType, out Dictionary<string, CompositeTexture> _textures);
        _textures ??= new Dictionary<string, CompositeTexture>();

        UniversalShapeTextureSource stexSource = new UniversalShapeTextureSource(capi, targetAtlas, shape, rcshape.Base.ToString());

        foreach (KeyValuePair<string, CompositeTexture> val in _textures)
        {
            CompositeTexture ctex = val.Value.Clone();
            ctex.Base.Path = variants.ReplacePlaceholders(ctex.Base.Path);
            ctex.BlendedOverlays?.Foreach(overlay => overlay.Base.Path = variants.ReplacePlaceholders(overlay.Base.Path));
            ctex.Bake(capi.Assets);
            stexSource.textures[val.Key] = ctex;
        }
        if (shape == null) return mesh;
        capi.Tesselator.TesselateShape("ItemPlayingCard item", shape, out mesh, stexSource);
        return mesh;
    }

    public MeshData GenContentMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
    {
        MeshData contentMesh = null;

        PlayingCardInventory inventory = GetInventory(itemstack);

        if (inventory.Empty)
        {
            return contentMesh;
        }

        float translation = 0;

        foreach (ItemSlot slot in inventory)
        {
            if (slot.Empty) continue;

            if (slot.Itemstack.Collectible.GetCollectibleInterface<IContainedMeshSource>() is not IContainedMeshSource icontainedMesh)
            {
                continue;
            }

            if (icontainedMesh.GenMesh(slot.Itemstack, targetAtlas, null) is not MeshData containedMesh)
            {
                continue;
            }

            int slotId = inventory.GetSlotId(slot);

            float rotation = predefinedRotations.Length > slotId ? predefinedRotations[slotId] : 0;

            containedMesh = containedMesh.Translate(-0.5f, -0.5f, -0.5f);
            containedMesh = containedMesh.Rotate(Vec3f.Zero, 0, rotation, 0);
            containedMesh = containedMesh.Translate(0.5f, 0.5f, 0.5f);

            translation += GetStackingTranslation(slot.Itemstack);
            containedMesh = containedMesh.Translate(0, translation, 0);

            if (contentMesh != null)
            {
                contentMesh.AddMeshData(containedMesh);
            }
            else
            {
                contentMesh = containedMesh;
            }
        }

        return contentMesh;
    }

    protected bool TryPut(BlockEntityContainer be, ItemSlot containerSlot, IPlayer byPlayer, BlockSelection blockSel)
    {
        bool inventoryInteractions = byPlayer.Entity.Controls.ShiftKey;
        if (!inventoryInteractions)
        {
            return false;
        }

        PlayingCardInventory inventory = GetInventory(containerSlot.Itemstack);

        ItemSlot ownSlot = null;
        if (inventory.Count(x => !x.Empty) < inventory.Count)
        {
            ownSlot = inventory[inventory.Count(x => !x.Empty)];
        }

        ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

        if (ownSlot == null || !inventory.CanContain(ownSlot, hotbarSlot) || hotbarSlot.Empty)
        {
            return false;
        }

        ItemStack movedStack = ownSlot?.Itemstack?.Clone();

        int movedQuantity = hotbarSlot.TryPutInto(api.World, ownSlot);
        if (movedQuantity <= 0)
        {
            return false;
        }

        didMoveItems(movedStack, byPlayer);

        Core.GetInstance(api).Mod.Logger.Audit(
            "{0} Put {1}x{2} into TabletopGames.ItemPlayingCard {3}.",
            byPlayer.PlayerName,
            movedQuantity,
            movedStack?.Collectible.Code,
            containerSlot?.Itemstack?.Collectible?.Code);

        ownSlot.MarkDirty();
        inventory.ToTreeAttributes(containerSlot.Itemstack.Attributes);
        containerSlot.MarkDirty();
        hotbarSlot.MarkDirty();
        return true;
    }

    protected bool TryTake(BlockEntityContainer be, ItemSlot containerSlot, IPlayer byPlayer, BlockSelection blockSel)
    {
        bool inventoryInteractions = byPlayer.Entity.Controls.ShiftKey;
        if (!inventoryInteractions)
        {
            return false;
        }

        PlayingCardInventory inventory = GetInventory(containerSlot.Itemstack);

        // default value is null, since we always need the most last slot
        ItemSlot ownSlot = inventory.LastOrDefault(x => !x.Empty, defaultValue: null);

        ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;

        if (!hotbarSlot.Empty || ownSlot == null || ownSlot.Empty)
        {
            return false;
        }

        ItemStack stack = ownSlot.TakeOutWhole();
        int movedQuantity = stack?.StackSize ?? 0;

        if (!byPlayer.InventoryManager.TryGiveItemstack(stack, slotNotifyEffect: true))
        {
            api.World.SpawnItemEntity(stack, byPlayer.Entity.SidedPos.AsBlockPos);
        }
        else
        {
            didMoveItems(stack, byPlayer);
        }

        Core.GetInstance(api).Mod.Logger.Audit("{0} Took {1}x{2} from TabletopGames.ItemPlayingCard {3}.", byPlayer.PlayerName, movedQuantity, stack?.Collectible.Code, containerSlot?.Itemstack?.Collectible?.Code);

        ownSlot.MarkDirty();
        inventory.ToTreeAttributes(containerSlot.Itemstack.Attributes);
        containerSlot.MarkDirty();
        return true;
    }

    protected void didMoveItems(ItemStack stack, IPlayer byPlayer)
    {
        AssetLocation sound = stack?.Block?.Sounds?.Place;
        api.World.PlaySoundAt(sound ?? new AssetLocation("sounds/player/build"), byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
    }

    /// <summary>
    /// Appends the content information of the inventory in the specified item containerSlot.
    /// </summary>
    protected StringBuilder GetInventoryInfo(ItemSlot containerSlot)
    {
        StringBuilder dsc = new StringBuilder();
        PlayingCardInventory inventory = GetInventory(containerSlot.Itemstack);
        if (inventory.Empty)
        {
            return dsc;
        }

        int count = 0;
        foreach (ItemSlot slot in inventory.Append(containerSlot))
        {
            count += slot.StackSize;
        }

        dsc.Append(Lang.Get("{0}x {1}", count, containerSlot.GetStackName()));
        return dsc;
    }

    /// <summary>
    /// Convenient method to check if this container contains anything
    /// </summary>
    /// <param name="containerStack">The ItemStack representing the container.</param>
    public bool IsEmpty(ItemStack containerStack) => GetInventory(containerStack).Empty;

    /// <summary>
    /// Returns the number of slots in this inventory.
    /// </summary>
    /// <param name="containerStack">The ItemStack representing the container.</param>
    public int GetQuantitySlots(ItemStack containerStack)
    {
        int quantitySlots = 0;
        if (containerStack == null)
        {
            return quantitySlots;
        }
        Variants.FromStack(containerStack).FindByVariant(QuantitySlotsByType, out quantitySlots);
        return Math.Max(quantitySlots, 1);
    }

    /// <summary>
    /// Returns the mesh translation when stacking meshes on top of each other.
    /// </summary>
    public float GetStackingTranslation(ItemStack stack)
    {
        float stackingTranslation = 0;
        if (stack == null)
        {
            return stackingTranslation;
        }
        Variants.FromStack(stack).FindByVariant(StackingTranslatonByType, out stackingTranslation);
        return stackingTranslation;
    }

    /// <summary>
    /// Retrieves the inventory stored within the attributes of the container item.
    /// </summary>
    /// <param name="containerStack">The ItemStack representing the container.</param>
    /// <returns>The inventory associated with the container.</returns>
    public PlayingCardInventory GetInventory(ItemStack containerStack)
    {
        int qslots = GetQuantitySlots(containerStack);
        PlayingCardInventory inv = new PlayingCardInventory(api, qslots);
        inv.FromTreeAttributes(containerStack.Attributes);
        return inv;
    }

    MeshData IContainedMeshSource.GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        return GetOrCreateMesh(itemstack, targetAtlas);
    }

    string IContainedMeshSource.GetMeshCacheKey(ItemStack itemstack)
    {
        StringBuilder stringBuilder = new StringBuilder();

        stringBuilder.Append(itemstack.Collectible.Code);
        stringBuilder.Append('-');
        stringBuilder.Append(Variants.FromStack(itemstack));

        PlayingCardInventory inventory = GetInventory(itemstack);

        if (!inventory.Empty)
        {
            stringBuilder.Append("-inv:");
            foreach (ItemSlot slot in inventory)
            {
                stringBuilder.Append('-');

                int slotId = inventory.GetSlotId(slot);
                if (slot.Empty)
                {
                    stringBuilder.Append($"{slotId}:empty");
                    continue;
                }

                if (slot.Itemstack.Collectible.GetCollectibleInterface<IContainedMeshSource>() is IContainedMeshSource meshSource)
                {
                    stringBuilder.Append($"{slotId}:");
                    stringBuilder.Append(meshSource.GetMeshCacheKey(slot.Itemstack));
                }
            }
        }
        return stringBuilder.ToString();
    }

    string IContainedCustomName.GetContainedName(ItemSlot inSlot, int quantity)
    {
        return GetHeldItemName(inSlot.Itemstack);
    }

    string IContainedCustomName.GetContainedInfo(ItemSlot inSlot)
    {
        return GetInventoryInfo(inSlot).ToString();
    }

    bool IContainedInteractable.OnContainedInteractStart(BlockEntityContainer be, ItemSlot containerSlot, IPlayer byPlayer, BlockSelection blockSel)
    {
        return TryPut(be, containerSlot, byPlayer, blockSel) || TryTake(be, containerSlot, byPlayer, blockSel);
    }

    bool IContainedInteractable.OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) => false;
    void IContainedInteractable.OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) { }
}