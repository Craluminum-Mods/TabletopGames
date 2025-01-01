using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace TabletopGames;

public class ItemIntermediate : ItemShapeTexturesFromAttributes, IContainedInteractable
{
    public Dictionary<string, InWorldCraftingStep[]> MultiStepCraftingByType { get; set; } = new();

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        if (Attributes != null)
        {
            MultiStepCraftingByType = Attributes["multiStepCrafting"].AsObject(defaultValue: new Dictionary<string, InWorldCraftingStep[]>());
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
        if (!materials.FindByMaterial(MultiStepCraftingByType, out InWorldCraftingStep[] steps) || steps == null || !steps.Any())
        {
            return false;
        }

        foreach (var step in steps)
        {
            CraftingRecipeIngredient ingred = step.TriggerBy.Clone();
            JsonItemStack output = step.ConvertTo?.Clone();
            ingred?.Resolve(api.World, "");
            output?.Resolve(api.World, "");

            if (!ingred.SatisfiesAsIngredient(activeSlot.Itemstack))
            {
                continue;
            }

            if (ingred.IsTool && ingred.ToolDurabilityCost > activeSlot.Itemstack.Collectible.GetRemainingDurability(activeSlot.Itemstack))
            {
                return false;
            }

            Dictionary<string, string> setStackMaterials = step.SetStackMaterials.ShallowClone();
            if (!string.IsNullOrEmpty(ingred.Name) && ingred.IsWildCard)
            {
                string value = WildcardUtil.GetWildcardValue(ingred.Code, activeSlot.Itemstack.Collectible.Code);
                setStackMaterials = setStackMaterials.ToDictionary(x => x.Key, x => x.Value.Replace("{" + ingred.Name + "}", value));
            }

            CollectibleBehaviorAdvancedToolModes.SetStackMaterials(slot.Itemstack, materials, setStackMaterials, out ItemStack intermediateStack);
            CollectibleBehaviorAdvancedToolModes.RemoveStackMaterials(intermediateStack, Materials.FromStack(intermediateStack), step.RemoveStackMaterials, out ItemStack finalStack);

            if (output != null)
            {
                if (step.CopyAttributes && finalStack.Attributes != null)
                {
                    output.ResolvedItemstack.Attributes = finalStack.Attributes.Clone();
                }

                slot.Itemstack.SetFrom(output.ResolvedItemstack?.Clone() ?? finalStack);
            }
            else
            {
                slot.Itemstack.SetFrom(finalStack);
            }

            switch (ingred.IsTool)
            {
                case true:
                    activeSlot.Itemstack.Collectible.DamageItem(byPlayer.Entity.World, byPlayer.Entity, activeSlot, ingred.ToolDurabilityCost);
                    break;
                case false when !step.ConsumeIngredient:
                    activeSlot.TakeOut(ingred.Quantity);
                    break;
            }

            slot.MarkDirty();
            activeSlot.MarkDirty();
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
    public bool CopyAttributes { get; set; } = false;
    public bool ConsumeIngredient { get; set; } = false;
}