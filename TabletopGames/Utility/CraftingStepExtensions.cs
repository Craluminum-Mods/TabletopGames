using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

namespace TabletopGames;

public static class CraftingStepExtensions
{
    public static bool HandleToolModeCrafting(this AdvancedToolMode mode, IPlayer byPlayer, ItemSlot targetSlot, ItemSlot inputSlot, Variants targetVariants)
    {
        return ItemChiseledPiece.ConsumeChiseledBlockAndGiveStack(mode, byPlayer, inputSlot)
            || byPlayer.ConsumeIngredientAndGiveStack(inputSlot, targetVariants, mode.SlotParams)
            || byPlayer.HandleInWorldCrafting(targetSlot, inputSlot, targetVariants, mode.SlotParams);
    }

    public static bool HandleInWorldCrafting(this IPlayer byPlayer, ItemSlot targetSlot, ItemSlot inputSlot, Variants targetVariants, List<CraftingStep> steps)
    {
        foreach (CraftingStep step in steps)
        {
            CraftingRecipeIngredient ingred = step.TriggerBy?.Clone();
            JsonItemStack output = step.ConvertTo?.Clone();
            ingred?.Resolve(byPlayer.Entity.World, "");
            output?.Resolve(byPlayer.Entity.World, "");

            if (ingred == null || !ingred.SatisfiesAsIngredient(inputSlot.Itemstack))
            {
                continue;
            }

            if (ingred.IsTool && ingred.ToolDurabilityCost > inputSlot.Itemstack.Collectible.GetRemainingDurability(inputSlot.Itemstack))
            {
                return false;
            }

            Dictionary<string, string> setVariants = step.SetVariants.ShallowClone();
            if (!string.IsNullOrEmpty(ingred.Name) && ingred.IsWildCard)
            {
                string value = WildcardUtil.GetWildcardValue(ingred.Code, inputSlot.Itemstack.Collectible.Code);
                setVariants = setVariants.ToDictionary(x => x.Key, x => x.Value.Replace("{" + ingred.Name + "}", value));
            }

            targetSlot.Itemstack.OverwriteVariants(out ItemStack finalStack, setVariants: setVariants, removeVariants: step.RemoveVariants, targetVariants);

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

    public static bool ConsumeIngredientAndGiveStack(this IPlayer byPlayer, ItemSlot inputSlot, Variants targetVariants, List<CraftingStep> steps)
    {
        foreach (CraftingStep step in steps)
        {
            CraftingRecipeIngredient ingred = step.TriggerBy?.Clone();
            JsonItemStack output = step.GiveStack?.Clone();
            ingred?.Resolve(byPlayer.Entity.World, "");
            output?.Resolve(byPlayer.Entity.World, "");

            if (output == null || output.ResolvedItemstack == null || ingred == null || !ingred.SatisfiesAsIngredient(inputSlot.Itemstack))
            {
                continue;
            }

            Dictionary<string, string> setVariants = step.SetVariants.ShallowClone();
            if (!string.IsNullOrEmpty(ingred.Name) && ingred.IsWildCard)
            {
                string value = WildcardUtil.GetWildcardValue(ingred.Code, inputSlot.Itemstack.Collectible.Code);
                setVariants = setVariants.ToDictionary(x => x.Key, x => x.Value.Replace("{" + ingred.Name + "}", value));
            }

            output.ResolvedItemstack.OverwriteVariants(out ItemStack finalStack, setVariants: setVariants, removeVariants: step.RemoveVariants, targetVariants);

            if (!byPlayer.InventoryManager.TryGiveItemstack(finalStack))
            {
                byPlayer.Entity.World.SpawnItemEntity(finalStack, byPlayer.Entity.SidedPos.AsBlockPos);
            }

            if (step.ConsumeIngredient)
            {
                inputSlot.TakeOut(ingred.Quantity);
            }

            inputSlot.MarkDirty();
            byPlayer.InventoryManager.BroadcastHotbarSlot();
            return true;
        }
        return false;
    }
}
