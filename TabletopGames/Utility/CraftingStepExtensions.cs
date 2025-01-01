using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.ServerMods;

namespace TabletopGames;

public static class CraftingStepExtensions
{
    public static bool HandleInWorldCrafting(this ItemSlot targetSlot, IPlayer byPlayer, ItemSlot inputSlot, Materials targetMaterials, List<CraftingStep> steps)
    {
        foreach (CraftingStep step in steps)
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

            targetSlot.Itemstack.SetStackMaterials(out ItemStack finalStack, setAttributes: setStackMaterials, removeAttributes: step.RemoveStackMaterials, targetMaterials);

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

    public static bool HandleGiveStack(this IPlayer byPlayer, ItemSlot inputSlot, Materials targetMaterials, List<CraftingStep> steps)
    {
        foreach (CraftingStep step in steps)
        {
            CraftingRecipeIngredient ingred = step.TriggerBy.Clone();
            JsonItemStack output = step.GiveStack?.Clone();
            ingred?.Resolve(byPlayer.Entity.World, "");
            output?.Resolve(byPlayer.Entity.World, "");

            if (output == null || output.ResolvedItemstack == null || !ingred.SatisfiesAsIngredient(inputSlot.Itemstack))
            {
                continue;
            }

            Dictionary<string, string> setStackMaterials = step.SetStackMaterials.ShallowClone();
            if (!string.IsNullOrEmpty(ingred.Name) && ingred.IsWildCard)
            {
                string value = WildcardUtil.GetWildcardValue(ingred.Code, inputSlot.Itemstack.Collectible.Code);
                setStackMaterials = setStackMaterials.ToDictionary(x => x.Key, x => x.Value.Replace("{" + ingred.Name + "}", value));
            }

            output.ResolvedItemstack.SetStackMaterials(out ItemStack finalStack, setAttributes: setStackMaterials, removeAttributes: step.RemoveStackMaterials, targetMaterials);

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
