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
/// Implements stacking and container behavior. Renders shape and textures using attribute based type system.
/// </summary>
public class ItemPlayingCard : Item, IContainedInteractable, IContainedMeshSource
{
    public Dictionary<string, string> PackCodeByType { get; protected set; } = new();
    public Dictionary<string, List<object>> NameByType { get; protected set; } = new();
    public Dictionary<string, List<object>> DescriptionByType { get; protected set; } = new();
    public Dictionary<string, List<object>> ContainedDescriptionByType { get; protected set; } = new();
    public Dictionary<string, int> QuantitySlotsByType { get; protected set; } = new();
    public Dictionary<string, float> StackingTranslatonByType { get; protected set; } = new();

    protected Dictionary<string, CompositeShape> shapeByType = new();
    protected Dictionary<string, Dictionary<string, CompositeTexture>> texturesByType = new();
    protected Dictionary<string, Dictionary<string, CompositeTexture>> safeTexturesByType = new();

    /// <summary>
    /// Predefined random rotations for a stack of cards
    /// </summary>
    private float[] stackRotations = new float[128] {
    -0.0167f, -0.0980f,  0.0650f, -0.0403f, -0.0263f, -0.0613f,  0.0132f, -0.0677f,
    -0.0751f, -0.0134f,  0.0124f, -0.0651f,  0.0106f, -0.0290f,  0.0916f, -0.0817f,
     0.0957f, -0.0176f,  0.0008f, -0.0704f,  0.0438f, -0.0620f, -0.0317f, -0.0953f,
    -0.0321f,  0.0935f,  0.0958f,  0.0489f, -0.0993f,  0.0880f,  0.0742f,  0.0542f,
    -0.0642f, -0.0801f, -0.0171f,  0.0771f,  0.0156f,  0.0473f, -0.0535f,  0.0047f,
     0.0419f,  0.0650f,  0.0614f, -0.0535f,  0.0746f, -0.0567f,  0.0604f,  0.0110f,
    -0.0628f,  0.0177f,  0.0036f,  0.0917f, -0.0917f, -0.0672f,  0.0967f,  0.0664f,
    -0.0699f, -0.0542f,  0.0078f, -0.0686f, -0.0352f, -0.0901f,  0.0423f, -0.0843f,

    -0.0167f, -0.0980f,  0.0650f, -0.0403f, -0.0263f, -0.0613f,  0.0132f, -0.0677f,
    -0.0751f, -0.0134f,  0.0124f, -0.0651f,  0.0106f, -0.0290f,  0.0916f, -0.0817f,
     0.0957f, -0.0176f,  0.0008f, -0.0704f,  0.0438f, -0.0620f, -0.0317f, -0.0953f,
    -0.0321f,  0.0935f,  0.0958f,  0.0489f, -0.0993f,  0.0880f,  0.0742f,  0.0542f,
    -0.0642f, -0.0801f, -0.0171f,  0.0771f,  0.0156f,  0.0473f, -0.0535f,  0.0047f,
     0.0419f,  0.0650f,  0.0614f, -0.0535f,  0.0746f, -0.0567f,  0.0604f,  0.0110f,
    -0.0628f,  0.0177f,  0.0036f,  0.0917f, -0.0917f, -0.0672f,  0.0967f,  0.0664f,
    -0.0699f, -0.0542f,  0.0078f, -0.0686f, -0.0352f, -0.0901f,  0.0423f, -0.0843f,
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
            safeTexturesByType = Attributes["safeTextures"].AsObject(defaultValue: new Dictionary<string, Dictionary<string, CompositeTexture>>());

            PackCodeByType = Attributes["packCode"].AsObject(defaultValue: new Dictionary<string, string>());
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

        EnumCardRenderType renderType = target switch
        {
            EnumItemRenderTarget.Gui => EnumCardRenderType.Gui,
            _ when renderinfo.DoesPlayerHaveThisSlot(capi) => EnumCardRenderType.Hand,
            _ => EnumCardRenderType.HandSafe
        };

        string key = ((IContainedMeshSource)this).GetMeshCacheKey(itemstack) + '-' + renderType.ToString();

        if (!meshRefs.TryGetValue(key, out MultiTextureMeshRef meshref) || TabletopDebug.DebugOnBeforeRender)
        {
            MeshData mesh = GetOrCreateMesh(itemstack, capi.ItemTextureAtlas, renderType);
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

    /// <summary>
    /// Unique identifier for item packs and shufflers
    /// </summary>
    public string GetPackCode(ItemStack itemStack)
    {
        Variants variants = Variants.FromStack(itemStack);
        variants.FindByVariant(PackCodeByType, out string packCode);
        return !string.IsNullOrEmpty(packCode) ? packCode : "card-default";
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

    public static bool IsCardFlipped(ItemStack stack)
    {
        return Variants.FromStack(stack).Get("flipped") == "true";
    }

    public static void FlipCard(ItemSlot inSlot, bool unflip = false)
    {
        if (inSlot.Empty)
        {
            return;
        }

        Variants variants = Variants.FromStack(inSlot.Itemstack);

        if (unflip)
        {
            variants.RemoveKey("flipped");
        }
        else
        {
            variants.Set("flipped", "true");
        }

        variants.ToStack(inSlot.Itemstack);
        inSlot.MarkDirty();
    }

    public static bool CanShuffle(ItemSlot inSlot)
    {
        return !inSlot.Empty && inSlot.Itemstack.Collectible is ItemPlayingCard;
    }

    /// <summary>
    /// Shuffle cards inside card inventory
    /// </summary>
    public static void Shuffle(ItemSlot inSlot, IWorldAccessor world)
    {
        if (inSlot.Empty || inSlot.Itemstack.Collectible is not ItemPlayingCard card)
        {
            return;
        }

        PlayingCardInventory inventory = card.GetInventory(inSlot.Itemstack);
        if (inventory.Empty) return;

        ItemStack firstStack = inSlot.Itemstack.Clone();
        firstStack.Attributes.RemoveAttribute("slots");
        DummySlot firstSlot = new DummySlot(firstStack);
        ItemSlot[] slots = new ItemSlot[] { firstSlot }.Append(inventory.Slots).Select(x => new DummySlot(x?.Itemstack?.Clone())).ToArray();

        slots = slots.Shuffle(world.Rand).OrderBy(x => x.Empty).ToArray();
        slots.Foreach(slot => FlipCard(slot));

        if (slots.Length > 0 && !slots[0].Empty)
        {
            ItemStack newFirstStack = slots[0].Itemstack.Clone();
            ItemSlot[] newSlots = slots.Skip(1).ToArray();

            // Update inventory
            for (int i = 0; i < inventory.Count && i < slots.Length; i++)
            {
                inventory[i] = newSlots[i];
            }

            inSlot.Itemstack = newFirstStack;
            inventory.ToTreeAttributes(inSlot.Itemstack.Attributes);
        }

        inSlot.MarkDirty();
    }

    public override void OnModifiedInInventorySlot(IWorldAccessor world, ItemSlot slot, ItemStack extractedStack)
    {
        // Moved from container to hotbar slot. I hope so
        if (slot?.Inventory is InventoryBasePlayer && extractedStack != null)
        {
            PlayingCardInventory cardInventory = GetInventory(extractedStack);
            if (cardInventory.Empty)
            {
                FlipCard(slot, unflip: true);
            }
        }
    }

    public override void TryMergeStacks(ItemStackMergeOperation op)
    {
        if (op?.SinkSlot?.Itemstack?.Collectible is not ItemPlayingCard sinkCard
            || op?.SourceSlot?.Itemstack?.Collectible is not ItemPlayingCard sourceCard)
        {
            base.TryMergeStacks(op);
            return;
        }

        PlayingCardInventory sinkInventory = sinkCard.GetInventory(op.SinkSlot.Itemstack);
        PlayingCardInventory sourceInventory = sourceCard.GetInventory(op.SourceSlot.Itemstack);

        // combine two single cards only!
        if (!sinkInventory.Empty
            || !sourceInventory.Empty
            || !sinkInventory.CanContain(sinkInventory[0], op.SourceSlot)
            || op.SourceSlot.TryPutInto(api.World, sinkInventory[0]) <= 0)
        {
            base.TryMergeStacks(op);
            return;
        }

        sinkInventory.ToTreeAttributes(op.SinkSlot.Itemstack.Attributes);
        op.SinkSlot.MarkDirty();
        op.SourceSlot.MarkDirty();
    }

    public override int GetMergableQuantity(ItemStack sinkStack, ItemStack sourceStack, EnumMergePriority priority)
    {
        if (priority != EnumMergePriority.DirectMerge)
        {
            return base.GetMergableQuantity(sinkStack, sourceStack, priority);
        }

        if (sinkStack?.Collectible is not ItemPlayingCard sinkCard
            || sourceStack?.Collectible is not ItemPlayingCard sourceCard)
        {
            return base.GetMergableQuantity(sinkStack, sourceStack, priority);
        }

        PlayingCardInventory sinkInventory = sinkCard.GetInventory(sinkStack);
        PlayingCardInventory sourceInventory = sourceCard.GetInventory(sourceStack);

        // combine two single cards only!
        if (sinkInventory.Empty && sourceInventory.Empty)
        {
            return 1;
        }
        return 0;
    }

    public MeshData GetOrCreateMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, EnumCardRenderType renderType) => renderType switch
    {
        EnumCardRenderType.Gui => GenGuiMesh(itemstack, targetAtlas, renderType),
        EnumCardRenderType.Stack => GenStackMesh(itemstack, targetAtlas, renderType),
        EnumCardRenderType.Hand => GenHandMesh(itemstack, targetAtlas, renderType),
        EnumCardRenderType.HandSafe => GenHandMesh(itemstack, targetAtlas, renderType),
        _ => new MeshData(32, 32).WithXyzFaces().WithRenderpasses().WithColorMaps(),
    };

    /// <summary>
    /// Generates mesh for a single card
    /// </summary>
    /// <param name="itemstack">First card item</param>
    /// <returns>Single card mesh</returns>
    public MeshData GenOneMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, EnumCardRenderType renderType)
    {
        ICoreClientAPI capi = api as ICoreClientAPI;
        MeshData mesh = new MeshData(32, 32).WithXyzFaces().WithRenderpasses().WithColorMaps();

        Variants variants = Variants.FromStack(itemstack);
        bool isFlipped = IsCardFlipped(itemstack);

        CompositeShape _shape = null;
        variants.FindByVariant(shapeByType, out _shape);

        if (_shape == null) return mesh;

        CompositeShape rcshape = _shape.Clone();
        rcshape.Base.Path = variants.ReplacePlaceholders(rcshape.Base.Path);
        rcshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");

        Shape shape = capi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();

        Dictionary<string, CompositeTexture> _textures = null;
        if (isFlipped || renderType == EnumCardRenderType.HandSafe)
        {
            variants.FindByVariant(safeTexturesByType, out _textures);
        }
        else
        {
            variants.FindByVariant(texturesByType, out _textures);
        }

        _textures ??= new Dictionary<string, CompositeTexture>();

        UniversalShapeTextureSource stexSource = new UniversalShapeTextureSource(capi, targetAtlas, shape, rcshape.Base.ToString());

        foreach (KeyValuePair<string, CompositeTexture> val in _textures)
        {
            CompositeTexture ctex = val.Value.Clone();
            ctex = variants.ReplacePlaceholders(ctex);
            ctex.Bake(capi.Assets);
            stexSource.textures[val.Key] = ctex;
        }

        if (shape == null) return mesh;
        capi.Tesselator.TesselateShape("ItemPlayingCard item", shape, out mesh, stexSource);
        return mesh;
    }

    /// <summary>
    /// Generates mesh for card in gui slot
    /// </summary>
    /// <param name="itemstack">First card item</param>
    /// <returns>Mesh of hand of cards</returns>
    public MeshData GenGuiMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, EnumCardRenderType renderType)
    {
        MeshData mesh = GenOneMesh(itemstack, targetAtlas, renderType);
        MeshData contentMesh = null;
        PlayingCardInventory inventory = GetInventory(itemstack);

        if (inventory.Empty) return mesh;

        const float BASE_ROTATION = GameMath.DEG2RAD * 5.0f;
        const float ROTATION_STEP = GameMath.DEG2RAD * 8.5f;
        const float MAGIC_SCALAR = 8f;

        float translationY = 0;
        float previousFanStartingTranslationY = 0;

        float cardHeight = 0;
        float rotation = BASE_ROTATION;

        Vec3f rotationOrigin = new Vec3f(0, 0, 0.35f);
        mesh = mesh.Translate(-0.5f, -0.5f, -0.5f);
        mesh = mesh.Rotate(rotationOrigin, 0, rotation, 0);
        mesh = mesh.Translate(0.5f, 0.5f, 0.5f);

        for (int cardIndex = 0; cardIndex < inventory.Slots.Length; cardIndex++)
        {
            ItemSlot slot = inventory.Slots[cardIndex];
            if (slot.Empty
                || slot.Itemstack.Collectible is not ItemPlayingCard otherCard
                || otherCard.GetOrCreateMesh(slot.Itemstack, targetAtlas, renderType) is not MeshData containedMesh
                || containedMesh == null)
            {
                continue;
            }

            float cardThickness = GetStackingTranslation(slot.Itemstack);

            // not adding 1 breaks things, since 0 slot is the 2nd card
            bool isStartingNewFan = (cardIndex + 1) % 16 == 0;
            if (isStartingNewFan)
            {
                rotation = BASE_ROTATION;
                cardHeight -= 0.2f;
                translationY = previousFanStartingTranslationY - (cardThickness * MAGIC_SCALAR);
                previousFanStartingTranslationY = translationY;
            }
            else
            {
                translationY += cardThickness;
            }

            rotation -= ROTATION_STEP;
            containedMesh = containedMesh.Translate(-0.5f, -0.5f, -0.5f);
            containedMesh = containedMesh.Rotate(rotationOrigin, 0, rotation, 0);
            containedMesh = containedMesh.Translate(0.5f, 0.5f, 0.5f);

            containedMesh = containedMesh.Translate(0, translationY, cardHeight);

            if (contentMesh != null) contentMesh.AddMeshData(containedMesh);
            else contentMesh = containedMesh;
        }

        if (contentMesh != null) mesh.AddMeshData(contentMesh);
        return mesh;
    }

    /// <summary>
    /// Generates mesh for hand of cards 
    /// </summary>
    /// <param name="itemstack">First card item</param>
    /// <returns>Mesh of hand of cards</returns>
    public MeshData GenHandMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, EnumCardRenderType renderType)
    {
        MeshData mesh = GenOneMesh(itemstack, targetAtlas, renderType);
        MeshData contentMesh = null;
        PlayingCardInventory inventory = GetInventory(itemstack);

        if (inventory.Empty) return mesh;

        const float BASE_ROTATION = GameMath.DEG2RAD * 65.0f;
        const float ROTATION_STEP = GameMath.DEG2RAD * 8.5f;
        const float MAGIC_SCALAR = 8f;

        float translationY = 0;
        float previousFanStartingTranslationY = 0;

        float cardHeight = 0;
        float rotation = BASE_ROTATION;

        Vec3f rotationOrigin = new Vec3f(0, 0, 0.35f);
        mesh = mesh.Translate(-0.5f, -0.5f, -0.5f);
        mesh = mesh.Rotate(rotationOrigin, 0, rotation, 0);
        mesh = mesh.Translate(0.5f, 0.5f, 0.5f);

        for (int cardIndex = 0; cardIndex < inventory.Slots.Length; cardIndex++)
        {
            ItemSlot slot = inventory.Slots[cardIndex];

            if (slot.Empty
                || slot.Itemstack.Collectible is not ItemPlayingCard otherCard
                || otherCard.GetOrCreateMesh(slot.Itemstack, targetAtlas, renderType) is not MeshData containedMesh
                || containedMesh == null)
            {
                continue;
            }

            float cardThickness = GetStackingTranslation(slot.Itemstack);

            // not adding 1 breaks things, since 0 slot is the 2nd card
            bool isStartingNewFan = (cardIndex + 1) % 16 == 0;
            if (isStartingNewFan)
            {
                rotation = BASE_ROTATION;
                cardHeight -= 0.2f;
                translationY = previousFanStartingTranslationY - (cardThickness * MAGIC_SCALAR);
                previousFanStartingTranslationY = translationY;
            }
            else
            {
                translationY += cardThickness;
            }

            rotation -= ROTATION_STEP;
            containedMesh = containedMesh.Translate(-0.5f, -0.5f, -0.5f);
            containedMesh = containedMesh.Rotate(rotationOrigin, 0, rotation, 0);
            containedMesh = containedMesh.Translate(0.5f, 0.5f, 0.5f);

            containedMesh = containedMesh.Translate(0, translationY, cardHeight);

            if (contentMesh != null) contentMesh.AddMeshData(containedMesh);
            else contentMesh = containedMesh;
        }

        if (contentMesh != null) mesh.AddMeshData(contentMesh);
        return mesh;
    }

    /// <summary>
    /// Generates mesh for cards stacked on each other
    /// </summary>
    /// <param name="itemstack">First card item</param>
    /// <returns>Mesh of stack of cards</returns>
    public MeshData GenStackMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, EnumCardRenderType renderType)
    {
        MeshData mesh = GenOneMesh(itemstack, targetAtlas, renderType);
        MeshData contentMesh = null;
        PlayingCardInventory inventory = GetInventory(itemstack);

        if (inventory.Empty) return mesh;

        float translationY = 0;
        int totalItems = inventory.TotalItemCount;

        for (int cardIndex = 0; cardIndex < inventory.Slots.Length; cardIndex++)
        {
            ItemSlot slot = inventory.Slots[cardIndex];

            if (slot.Empty
                || slot.Itemstack.Collectible is not ItemPlayingCard otherCard
                || otherCard.GetOrCreateMesh(slot.Itemstack, targetAtlas, renderType) is not MeshData containedMesh
                || containedMesh == null)
            {
                continue;
            }

            // 0 index is 2nd card
            bool rotateVeryfirstCard = cardIndex == 0 && !IsCardFlipped(itemstack);
            if (rotateVeryfirstCard)
            {
                mesh = mesh.Translate(-0.5f, -0.5f, -0.5f);
                mesh = mesh.Rotate(Vec3f.Zero, 0, GameMath.DEG2RAD * 90f, 0);
                mesh = mesh.Translate(0.5f, 0.5f, 0.5f);
            }
            
            float cardThickness = GetStackingTranslation(slot.Itemstack);
            float rotation = stackRotations.Length > cardIndex ? stackRotations[cardIndex] : 0;

            containedMesh = containedMesh.Translate(-0.5f, -0.5f, -0.5f);
            containedMesh = containedMesh.Rotate(Vec3f.Zero, 0, rotation, 0);
            containedMesh = containedMesh.Translate(0.5f, 0.5f, 0.5f);

            translationY += cardThickness;
            containedMesh = containedMesh.Translate(0, translationY, 0);

            if (contentMesh != null) contentMesh.AddMeshData(containedMesh);
            else contentMesh = containedMesh;
        }

        if (contentMesh != null) mesh.AddMeshData(contentMesh);
        return mesh;
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
    /// Returns the mesh translationY when stacking meshes on top of each other.
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

    /// <summary>
    /// Adds new card to inventory of main card
    /// </summary>
    /// <param name="stack">Main card with inventory</param>
    /// <param name="newStack">New card</param>
    public bool TryAddCardToInventory(ItemStack stack, ItemStack newStack, out int movedQuantity)
    {
        movedQuantity = 0;

        if (stack == null || newStack == null)
        {
            return false;
        }

        PlayingCardInventory inventory = GetInventory(stack);
        ItemSlot? invSlot = null;

        if (inventory.NonEmptyCount < inventory.Count)
        {
            invSlot = inventory[inventory.NonEmptyCount];
        }

        if (invSlot == null)
        {
            return false;
        }

        DummySlot dummySlot = new(newStack);
        movedQuantity = dummySlot.TryPutInto(api.World, invSlot);
        if (movedQuantity <= 0)
        {
            return false;
        }

        inventory.ToTreeAttributes(stack.Attributes);
        return true;
    }

    /// <summary>
    /// Takes last card from inventory
    /// </summary>
    /// <param name="stack">Card with inventory</param>
    /// <param name="newStack">New card</param>
    public ItemStack? TryTakeCardFromInventory(ItemStack stack)
    {
        if (stack == null)
        {
            return null;
        }

        PlayingCardInventory inventory = GetInventory(stack);

        // default value is null, since we always need the most last slot
        ItemSlot? invSlot = inventory.LastOrDefault(slot => !slot.Empty, defaultValue: null);
        if (invSlot == null)
        {
            return null;
        }

        ItemStack giveStack = invSlot.TakeOutWhole();
        inventory.ToTreeAttributes(stack.Attributes);
        return giveStack;
    }

    MeshData IContainedMeshSource.GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        return GetOrCreateMesh(itemstack, targetAtlas, EnumCardRenderType.Stack);
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

    /// <summary>
    /// Temporary stub until base game starts using GetCollectibleInterface in BlockEntityGroundStorage.OnPlayerInteractStep
    /// </summary>
    bool IContainedInteractable.OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (GetCollectibleInterface<IPlayingCardInteractions>() is IPlayingCardInteractions cardInteractions)
        {
            return cardInteractions.OnContainedInteractStart(be, slot, byPlayer, blockSel);
        }
        return false;
    }

    /// <summary>
    /// Temporary stub until base game starts using GetCollectibleInterface in BlockEntityGroundStorage.OnPlayerInteractStep
    /// </summary>
    bool IContainedInteractable.OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (GetCollectibleInterface<IPlayingCardInteractions>() is IPlayingCardInteractions cardInteractions)
        {
            return cardInteractions.OnContainedInteractStep(secondsUsed, be, slot, byPlayer, blockSel);
        }
        return false;
    }

    /// <summary>
    /// Temporary stub until base game starts using GetCollectibleInterface in BlockEntityGroundStorage.OnPlayerInteractStep
    /// </summary>
    void IContainedInteractable.OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (GetCollectibleInterface<IPlayingCardInteractions>() is IPlayingCardInteractions cardInteractions)
        {
            cardInteractions.OnContainedInteractStop(secondsUsed, be, slot, byPlayer, blockSel);
        }
    }
}