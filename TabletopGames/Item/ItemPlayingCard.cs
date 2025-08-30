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
public class ItemPlayingCard : Item, IContainedInteractable, IContainedMeshSource, IShufflable
{
    public Dictionary<string, string> PackCodeByType { get; protected set; } = new();
    public Dictionary<string, List<object>> NameByType { get; protected set; } = new();
    public Dictionary<string, List<object>> DescriptionByType { get; protected set; } = new();
    public Dictionary<string, List<object>> ContainedDescriptionByType { get; protected set; } = new();
    public Dictionary<string, int> QuantitySlotsByType { get; protected set; } = new();
    public Dictionary<string, float> StackingTranslatonByType { get; protected set; } = new();

    public Dictionary<string, CompositeShape> ShapeByType { get; protected set; } = new();
    public Dictionary<string, Dictionary<string, CompositeTexture>> TexturesByType { get; protected set; } = new();
    public Dictionary<string, Dictionary<string, CompositeTexture>> SafeTexturesByType { get; protected set; } = new();

    public ICoreAPI Api => api;
    public ICoreClientAPI clientApi => api as ICoreClientAPI;

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

            ShapeByType = Attributes["shape"].AsObject(defaultValue: new Dictionary<string, CompositeShape>());
            TexturesByType = Attributes["textures"].AsObject(defaultValue: new Dictionary<string, Dictionary<string, CompositeTexture>>());
            SafeTexturesByType = Attributes["safeTextures"].AsObject(defaultValue: new Dictionary<string, Dictionary<string, CompositeTexture>>());

            PackCodeByType = Attributes["packCode"].AsObject(defaultValue: new Dictionary<string, string>());
            QuantitySlotsByType = Attributes["quantitySlots"].AsObject(defaultValue: new Dictionary<string, int>());
            StackingTranslatonByType = Attributes["stackingTranslaton"].AsObject(defaultValue: new Dictionary<string, float>());
        }
    }

    public override bool Equals(ItemStack thisStack, ItemStack otherStack, params string[] ignoreAttributeSubTrees)
    {
        ignoreAttributeSubTrees ??= Array.Empty<string>();

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
            _ when DoesPlayerHaveThisSlot(renderinfo, capi) => EnumCardRenderType.Hand,
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

    public static bool DoesPlayerHaveThisSlot(ItemRenderInfo renderinfo, ICoreClientAPI capi)
    {
        return (renderinfo?.InSlot?.Inventory as InventoryBasePlayer)?.Player.PlayerUID == capi.World.Player.PlayerUID;
    }

    public override string GetHeldItemName(ItemStack itemStack)
    {
        if (NameByType == null || NameByType.Count == 0)
        {
            return base.GetHeldItemName(itemStack);
        }

        Variants variants = Variants.FromStack(itemStack);
        variants.FindByVariant(NameByType, out List<object> _langKeys);

        string name = variants.GetName(_langKeys);
        if (string.IsNullOrEmpty(name))
        {
            name = base.GetHeldItemName(itemStack);
        }
        return name;
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

    public static void TryFlipCard(ItemSlot inSlot, IPlayer byPlayer)
    {
        if (inSlot.Empty || inSlot.Itemstack.Collectible is not ItemPlayingCard card)
        {
            return;
        }

        bool flipCard = byPlayer.Entity.Controls.CtrlKey;
        if (flipCard)
        {
            PlayingCardInventory cardInventory = card.GetInventory(inSlot.Itemstack);
            if (cardInventory.Empty)
            {
                ItemPlayingCard.FlipCard(inSlot);
            }
        }
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
            variants.RemoveKeys("flipped");
        }
        else
        {
            variants.Set("flipped", "true");
        }

        variants.ToStack(inSlot.Itemstack);
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
        EnumCardRenderType.Gui => this.GenGuiMesh(itemstack, targetAtlas, renderType),
        EnumCardRenderType.Stack => this.GenStackMesh(itemstack, targetAtlas, renderType),
        EnumCardRenderType.Hand => this.GenHandMesh(itemstack, targetAtlas, renderType),
        EnumCardRenderType.HandSafe => this.GenHandMesh(itemstack, targetAtlas, renderType),
        _ => RenderExtensions.GenEmptyMesh(),
    };

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

    /// <summary>
    /// Combine all cards inside player inventory
    /// </summary>
    public static void CombineAllInInventory(IPlayer byPlayer)
    {
        IEnumerable<ItemSlot> inventorySlots = byPlayer.InventoryManager.GetOwnInventory("backpack").Concat(byPlayer.InventoryManager.GetOwnInventory("hotbar"));

        foreach (ItemSlot? mainSlot in inventorySlots)
        {
            if (mainSlot.Empty) continue;
            if (mainSlot.Itemstack.Collectible is not ItemPlayingCard firstCard) continue;

            foreach (ItemSlot? otherSlot in inventorySlots)
            {
                if (mainSlot == otherSlot) continue;
                if (otherSlot.Empty) continue;
                if (otherSlot.Itemstack.Collectible is not ItemPlayingCard secondCard) continue;
                if (!secondCard.IsEmpty(otherSlot.Itemstack)) continue;

                if (firstCard.TryAddCardToInventory(mainSlot.Itemstack, otherSlot.Itemstack.Clone(), out int movedQuantity))
                {
                    otherSlot.TakeOut(movedQuantity);
                    otherSlot.MarkDirty();
                }
            }
            mainSlot.MarkDirty();
        }
    }

    public static void SetCardRotation(BlockEntityGroundStorage blockEntityGroundStorage, IPlayer byPlayer, ItemSlot inSlot)
    {
        if (inSlot.Empty || inSlot.Itemstack.Collectible is not ItemPlayingCard)
        {
            return;
        }

        float meshAngle = blockEntityGroundStorage.MeshAngle;
        float rotateYaw = (byPlayer.Entity.Pos.Yaw - meshAngle - (GameMath.DEG2RAD * 45f)) + (GameMath.DEG2RAD * 180f);
        rotateYaw = GameMath.Mod(rotateYaw, GameMath.TWOPI);
        inSlot.Itemstack.Attributes.SetFloat("rotateYaw", rotateYaw);
        inSlot.MarkDirty();
    }

    MeshData IContainedMeshSource.GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        MeshData mesh = GetOrCreateMesh(itemstack, targetAtlas, EnumCardRenderType.Stack);

        // Rotations are currently implemented for BlockEntityGroundStorage only
        if (itemstack.Attributes.TryGetFloat("rotateYaw") is float rotateYaw)
        {
            mesh = mesh.Rotate(Vec3f.Half, 0, rotateYaw, 0);
        }

        return mesh;
    }

    string IContainedMeshSource.GetMeshCacheKey(ItemStack itemstack)
    {
        StringBuilder stringBuilder = new StringBuilder();

        stringBuilder.Append(itemstack.Collectible.Code);
        stringBuilder.Append('-');
        stringBuilder.Append(Variants.FromStack(itemstack));
        stringBuilder.Append("-rotateyaw:");
        stringBuilder.Append(itemstack.Attributes.GetFloat("rotateYaw"));

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
    /// Temporary stub until base game starts using GetCollectibleInterface in BlockEntityGroundStorage.OnPlayerInteractStart
    /// </summary>
    bool IContainedInteractable.OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (GetCollectibleInterface<IPlayingCardInteractions>() is IPlayingCardInteractions interactions)
        {
            return interactions.OnContainedInteractStart(be, slot, byPlayer, blockSel);
        }
        return false;
    }

    /// <summary>
    /// Temporary stub until base game starts using GetCollectibleInterface in BlockEntityGroundStorage.OnPlayerInteractStep
    /// </summary>
    bool IContainedInteractable.OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (GetCollectibleInterface<IPlayingCardInteractions>() is IPlayingCardInteractions interactions)
        {
            return interactions.OnContainedInteractStep(secondsUsed, be, slot, byPlayer, blockSel);
        }
        return false;
    }

    /// <summary>
    /// Temporary stub until base game starts using GetCollectibleInterface in BlockEntityGroundStorage.OnPlayerInteractStop
    /// </summary>
    void IContainedInteractable.OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (GetCollectibleInterface<IPlayingCardInteractions>() is IPlayingCardInteractions interactions)
        {
            interactions.OnContainedInteractStop(secondsUsed, be, slot, byPlayer, blockSel);
        }
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    bool IShufflable.CanShuffle(ItemSlot inSlot)
    {
        return !inSlot.Empty && inSlot.Itemstack.Collectible is ItemPlayingCard;
    }

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    void IShufflable.Shuffle(ItemSlot inSlot, IWorldAccessor world)
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
}