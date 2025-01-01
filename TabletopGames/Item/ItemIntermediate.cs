using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace TabletopGames;

public class ItemIntermediate : ItemShapeTexturesFromAttributes, IContainedInteractable
{
    public Dictionary<string, List<CraftingStep>> InWorldCraftingPropsByType { get; set; } = new();

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        if (Attributes != null)
        {
            InWorldCraftingPropsByType = Attributes["inWorldCraftingProps"].AsObject(defaultValue: new Dictionary<string, List<CraftingStep>>());
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
        if (!materials.FindByMaterial(InWorldCraftingPropsByType, out List<CraftingStep> steps) || steps == null || !steps.Any())
        {
            return false;
        }
        return slot.HandleInWorldCrafting(byPlayer, activeSlot, materials, steps);
    }

    public bool OnContainedInteractStep(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) => false;
    public void OnContainedInteractStop(float secondsUsed, BlockEntityContainer be, ItemSlot slot, IPlayer byPlayer, BlockSelection blockSel) { }
}

public class CraftingStep
{
    public CraftingRecipeIngredient TriggerBy { get; set; }
    public Dictionary<string, string> SetStackMaterials { get; set; } = new();
    public List<string> RemoveStackMaterials { get; set; } = new();

    public JsonItemStack ConvertTo { get; set; }
    public bool CopyAttributes { get; set; }
    public bool ConsumeIngredient { get; set; } = true;

    public JsonItemStack GiveStack { get; set; }
}