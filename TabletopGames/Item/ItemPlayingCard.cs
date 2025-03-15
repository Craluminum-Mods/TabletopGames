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
/// Implements stacking behavior.
/// <inheritdoc/>
/// </summary>
public class ItemPlayingCard : ItemShapeTexturesFromAttributes, IContainedInteractable
{
    public Dictionary<string, int> QuantitySlotsByType { get; protected set; } = new();
    public Dictionary<string, float> StackingTranslatonByType { get; protected set; } = new();

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

    public override void LoadTypes()
    {
        base.LoadTypes();

        if (Attributes != null)
        {
            QuantitySlotsByType = Attributes["quantitySlots"].AsObject(defaultValue: new Dictionary<string, int>());
            StackingTranslatonByType = Attributes["stackingTranslaton"].AsObject(defaultValue: new Dictionary<string, float>());
        }
    }

    public override bool Equals(ItemStack thisStack, ItemStack otherStack, params string[] ignoreAttributeSubTrees)
    {
        if (thisStack.Id == otherStack.Id && IsEmpty(thisStack) && IsEmpty(otherStack))
        {
            ignoreAttributeSubTrees ??= Array.Empty<string>();
            ignoreAttributeSubTrees = ignoreAttributeSubTrees.Append("slots");
        }
        return base.Equals(thisStack, otherStack, ignoreAttributeSubTrees);
    }

    public bool OnContainedInteractStart(BlockEntityContainer be, ItemSlot containerSlot, IPlayer byPlayer, BlockSelection blockSel)
    {
        return TryPut(containerSlot, byPlayer) || TryTake(containerSlot, byPlayer);
    }

    public virtual bool OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) => false;
    public virtual void OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) { }

    protected bool TryPut(ItemSlot containerSlot, IPlayer byPlayer)
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
    
    protected bool TryTake(ItemSlot containerSlot, IPlayer byPlayer)
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

    public override MeshData GetOrCreateMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
    {
        MeshData containerMesh = base.GetOrCreateMesh(itemstack, targetAtlas);

        if (GenContentMesh(itemstack, targetAtlas) is MeshData contentMesh && contentMesh != null)
        {
            containerMesh.AddMeshData(contentMesh);
        }

        return containerMesh;
    }

    public virtual MeshData GenContentMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
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

    public override string GetMeshCacheKey(ItemStack itemstack)
    {
        StringBuilder stringBuilder = new StringBuilder(base.GetMeshCacheKey(itemstack));

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

    public override string GetContainedInfo(ItemSlot inSlot)
    {
        return GetInventoryInfo(inSlot).ToString();
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
}