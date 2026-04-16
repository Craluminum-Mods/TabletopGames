using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace TabletopGames;

public class CollectibleBehaviorIntermediate(CollectibleObject collObj) : CollectibleBehavior(collObj), IContainedInteractable
{
    public Dictionary<string, List<CraftingStep>> InWorldCraftingPropsByType { get; set; } = new();

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        InWorldCraftingPropsByType = properties["inWorldCraftingProps"].AsObject(defaultValue: new Dictionary<string, List<CraftingStep>>());
    }

    public bool OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        ItemSlot hotbarSlot = byPlayer.Entity.RightHandItemSlot;
        if (be is not BlockEntityGroundStorage gs || hotbarSlot.Empty)
        {
            return false;
        }

        Variants variants = Variants.FromStack(slot.Itemstack);
        if (!variants.FindByVariant(InWorldCraftingPropsByType, out List<CraftingStep> steps) || steps == null || !steps.Any())
        {
            return false;
        }
        if (byPlayer.HandleInWorldCrafting(slot, inputSlot: hotbarSlot, variants, steps))
        {
            be.MarkDirty();
            return true;
        }
        return false;
    }

    public bool OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) => false;
    public void OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) { }

    WorldInteraction[] IContainedInteractable.GetContainedInteractionHelp(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        if (slot?.Itemstack?.Collectible.GetBehavior<CollectibleBehaviorInteractionHelpConstructor>()?.GetInteractionHelp(slot.Itemstack) is WorldInteraction[] interactions)
        {
            return interactions;
        }
        return [];
    }
}