using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace TabletopGames;

public class ItemIntermediate : ItemShapeTexturesFromAttributes, IContainedInteractable
{
    public Dictionary<string, List<InWorldCraftingStep>> InWorldCraftingPropsByType { get; set; } = new();

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        if (Attributes != null)
        {
            InWorldCraftingPropsByType = Attributes["inWorldCraftingProps"].AsObject(defaultValue: new Dictionary<string, List<InWorldCraftingStep>>());
        }
    }

    public bool OnContainedInteractStart(BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel)
    {
        ItemSlot activeSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
        if (be is not BlockEntityGroundStorage gs || activeSlot.Empty)
        {
            return false;
        }

        Materials materials = Materials.FromStack(slot.Itemstack);
        if (!materials.FindByMaterial(InWorldCraftingPropsByType, out List<InWorldCraftingStep> steps) || steps == null || !steps.Any())
        {
            return false;
        }
        return HandleInWorldCrafting(slot, byPlayer, activeSlot, materials, steps);
    }

    public static bool HandleInWorldCrafting(ItemSlot targetSlot, IPlayer byPlayer, ItemSlot inputSlot, Materials targetMaterials, List<InWorldCraftingStep> steps)
    {
        foreach (InWorldCraftingStep step in steps)
        {
            CraftingRecipeIngredient ingred = step.TriggerBy.Clone();
            JsonItemStack output = step.ConvertTo?.Clone();
            ingred?.Resolve(byPlayer.Entity.World, "");
            output?.Resolve(byPlayer.Entity.World, "");

            if (!ingred.SatisfiesAsIngredient(inputSlot.Itemstack))
            {
                continue;
            }

            if (ingred.IsTool && ingred.ToolDurabilityCost > inputSlot.Itemstack.Collectible.GetRemainingDurability(inputSlot.Itemstack))
            {
                return false;
            }

            Dictionary<string, string> setStackMaterials = step.SetStackMaterials.ShallowClone();
            if (!string.IsNullOrEmpty(ingred.Name) && ingred.IsWildCard)
            {
                string value = WildcardUtil.GetWildcardValue(ingred.Code, inputSlot.Itemstack.Collectible.Code);
                setStackMaterials = setStackMaterials.ToDictionary(x => x.Key, x => x.Value.Replace("{" + ingred.Name + "}", value));
            }

            CollectibleBehaviorAdvancedToolModes.SetStackMaterials(targetSlot.Itemstack, out ItemStack finalStack, setAttributes: setStackMaterials, removeAttributes: step.RemoveStackMaterials, targetMaterials);

            if (output != null && output.ResolvedItemstack != null)
            {
                if (step.CopyAttributes && finalStack.Attributes != null)
                {
                    output.ResolvedItemstack.Attributes = finalStack.Attributes.Clone();
                }

                targetSlot.Itemstack.SetFrom(output.ResolvedItemstack?.Clone() ?? finalStack);
            }
            else
            {
                targetSlot.Itemstack.SetFrom(finalStack);
            }

            switch (ingred.IsTool)
            {
                case true:
                    inputSlot.Itemstack.Collectible.DamageItem(byPlayer.Entity.World, byPlayer.Entity, inputSlot, ingred.ToolDurabilityCost);
                    break;
                case false when step.ConsumeIngredient:
                    inputSlot.TakeOut(ingred.Quantity);
                    break;
            }

            targetSlot.MarkDirty();
            inputSlot.MarkDirty();
            byPlayer.InventoryManager.BroadcastHotbarSlot();
            return true;
        }
        return false;
    }

    public bool OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) => false;
    public void OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) { }
}

public class InWorldCraftingStep
{
    public CraftingRecipeIngredient TriggerBy { get; set; }
    public Dictionary<string, string> SetStackMaterials { get; set; } = new();
    public List<string> RemoveStackMaterials { get; set; } = new();

    public JsonItemStack ConvertTo { get; set; }
    public bool CopyAttributes { get; set; }
    public bool ConsumeIngredient { get; set; } = true;
}