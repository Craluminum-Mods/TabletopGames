using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace TabletopGames;

public class ItemIntermediate : ItemShapeTexturesFromAttributes, IContainedInteractable
{
    public Dictionary<string, List<CraftingStep>> InWorldCraftingPropsByType { get; set; } = new();

    public override void LoadTypes()
    {
        base.LoadTypes();

        if (Attributes != null)
        {
            InWorldCraftingPropsByType = Attributes["inWorldCraftingProps"].AsObject(defaultValue: new Dictionary<string, List<CraftingStep>>());
        }
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
        return byPlayer.HandleInWorldCrafting(slot, inputSlot: hotbarSlot, variants, steps);
    }

    public bool OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) => false;
    public void OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) { }
}