using System;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TabletopGames;

/// <summary>
/// Represents an item that can store multiple ItemStacks within a single ItemStack.  
/// Provides an inventory that can be accessed from ground storage.  
/// </summary>
public class ItemContainerWithDetachableLid : ItemContainer, IContainedInteractable
{
    public const string LidAttributeName = "containedLidStack";

    public override bool Equals(ItemStack thisStack, ItemStack otherStack, params string[] ignoreAttributeSubTrees)
    {
        if (thisStack.Id == otherStack.Id && HasLid(thisStack) && HasLid(otherStack))
        {
            ignoreAttributeSubTrees ??= Array.Empty<string>();
            ignoreAttributeSubTrees = ignoreAttributeSubTrees.Append(LidAttributeName);
        }
        return base.Equals(thisStack, otherStack, ignoreAttributeSubTrees);
    }

    public override bool OnContainedInteractStart(BlockEntityContainer be, ItemSlot containerSlot, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (containerSlot.Empty) return false;

        bool lidInteractions = byPlayer.Entity.Controls.ShiftKey;
        if (!lidInteractions)
        {
            return !HasLid(containerSlot.Itemstack) && base.OnContainedInteractStart(be, containerSlot, byPlayer, blockSel);
        }

        return HasLid(containerSlot.Itemstack) ? DetachLid(containerSlot, byPlayer) : TryAttachLid(containerSlot, byPlayer);
    }

    public bool HasLid(ItemStack containerStack)
    {
        return GetLid(containerStack) != null;
    }

    public ItemStack GetLid(ItemStack containerStack)
    {
        if (containerStack == null)
        {
            return null;
        }
        ItemStack giveStack = containerStack.Attributes.GetItemstack(LidAttributeName);
        giveStack?.ResolveBlockOrItem(api.World);
        return giveStack;
    }

    public bool TryAttachLid(ItemSlot containerSlot, IPlayer byPlayer)
    {
        ItemSlot hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        string containerKey = GetContainerKey(containerSlot.Itemstack);

        if (hotbarSlot?.Itemstack?.Collectible.GetCollectibleInterface<IDetachableLid>() is not IDetachableLid detachableLid ||
            !detachableLid.IsSuitableForContainer(containerKey))
        {
            return false;
        }

        if (hotbarSlot.Itemstack.Collectible is ItemContainer itemContainer && !itemContainer.IsEmpty(hotbarSlot.Itemstack))
        {
            return false;
        }

        ItemStack lidStack = hotbarSlot.TakeOut(1);
        containerSlot.Itemstack.Attributes.SetItemstack(LidAttributeName, lidStack);
        containerSlot.MarkDirty();
        hotbarSlot.MarkDirty();
        PlayCloseSound(containerSlot, byPlayer);
        return true;
    }

    public bool DetachLid(ItemSlot containerSlot, IPlayer byPlayer)
    {
        ItemStack giveStack = containerSlot.Itemstack.Attributes.GetItemstack(LidAttributeName);
        if (giveStack == null) return false;

        giveStack.ResolveBlockOrItem(api.World);

        if (!byPlayer.InventoryManager.TryGiveItemstack(giveStack, slotNotifyEffect: true))
        {
            api.World.SpawnItemEntity(giveStack, byPlayer.Entity.SidedPos.AsBlockPos);
        }

        containerSlot.Itemstack.Attributes.RemoveAttribute(LidAttributeName);
        containerSlot.MarkDirty();
        PlayOpenSound(containerSlot, byPlayer);
        return true;
    }

    /// <summary>
    /// Plays the sound associated with opening the item based on its variants.
    /// </summary>
    /// <param name="containerSlot">The slot containing the item.</param>
    /// <param name="byPlayer">The player interacting with the item.</param>
    public void PlayOpenSound(ItemSlot containerSlot, IPlayer byPlayer)
    {
        if (Variants.FromStack(containerSlot.Itemstack).FindByVariant(openSoundByType, out string sound))
        {
            api.World.PlaySoundAt(sound, byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
        }
    }

    /// <summary>
    /// Plays the sound associated with closing the item based on its variants.
    /// </summary>
    /// <param name="containerSlot">The slot containing the item.</param>
    /// <param name="byPlayer">The player interacting with the item.</param>
    public void PlayCloseSound(ItemSlot containerSlot, IPlayer byPlayer)
    {
        if (Variants.FromStack(containerSlot.Itemstack).FindByVariant(closeSoundByType, out string sound))
        {
            api.World.PlaySoundAt(sound, byPlayer.Entity, byPlayer, randomizePitch: true, 16f);
        }
    }

    public override MeshData GetOrCreateMesh(ItemStack containerStack, ITextureAtlasAPI targetAtlas)
    {
        MeshData containerMesh = base.GetOrCreateMesh(containerStack, targetAtlas).Clone();

        if (GetOrCreateLidMesh(containerStack, targetAtlas) is MeshData lidMesh && lidMesh != null)
        {
            containerMesh.AddMeshData(lidMesh);
        }
        return containerMesh;
    }

    public virtual MeshData GetOrCreateLidMesh(ItemStack containerStack, ITextureAtlasAPI targetAtlas)
    {
        ItemStack lidStack = GetLid(containerStack);

        if (lidStack?.Collectible?.GetCollectibleInterface<IDetachableLid>() is IDetachableLid ilid)
        {
            string containerKey = GetContainerKey(containerStack);
            return ilid.GenContentMesh(containerKey, lidStack, targetAtlas);
        }
        return null;
    }

    public override string GetMeshCacheKey(ItemStack itemstack)
    {
        StringBuilder stringBuilder = new StringBuilder(base.GetMeshCacheKey(itemstack));

        ItemStack lidStack = GetLid(itemstack);
        if (lidStack != null)
        {
            stringBuilder.Append("-lid:");
            stringBuilder.Append(lidStack.Collectible.Code);
            stringBuilder.Append('-');
            stringBuilder.Append(lidStack.Attributes.ToJsonToken());
        }
        return stringBuilder.ToString();
    }
}
