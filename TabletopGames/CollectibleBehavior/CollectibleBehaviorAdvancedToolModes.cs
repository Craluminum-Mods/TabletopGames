using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace TabletopGames;

public class CollectibleBehaviorAdvancedToolModes : CollectibleBehavior
{
    private Dictionary<string, List<Dictionary<string, string>>> setStackMaterialsByType;

    public CollectibleBehaviorAdvancedToolModes(CollectibleObject collObj) : base(collObj) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        setStackMaterialsByType = properties["setStackMaterials"].AsObject(defaultValue: new Dictionary<string, List<Dictionary<string, string>>>());
    }

    public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
    {
        if (slot.Empty)
        {
            return;
        }

        Materials materials = Materials.FromStack(slot.Itemstack);
        if (!materials.FindByMaterial(setStackMaterialsByType, out List<Dictionary<string, string>> newAttributes) || newAttributes == null || !newAttributes.Any())
        {
            return;
        }

        if (newAttributes.Count <= toolMode || newAttributes[toolMode] == null)
        {
            return;
        }

        SetStackMaterials(slot.Itemstack.Clone(), materials.Clone(), newAttributes[toolMode], out ItemStack newStack);
        slot.Itemstack.SetFrom(newStack);
        slot.MarkDirty();
    }

    public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
    {
        SkillItem[] _toolModes = Array.Empty<SkillItem>();

        if (slot.Empty)
        {
            return null;
        }

        Materials materials = Materials.FromStack(slot.Itemstack);
        if (materials.FindByMaterial(setStackMaterialsByType, out List<Dictionary<string, string>> newAttributes) && newAttributes != null && newAttributes.Any())
        {
            for (int i = 0; i < newAttributes.Count; i++)
            {
                SetStackMaterials(slot.Itemstack.Clone(), materials.Clone(), newAttributes[i], out ItemStack newStack);

                SkillItem toolMode = new()
                {
                    Name = newStack.GetName(),
                    RenderHandler = newStack.RenderItemStack(forPlayer.Entity.Api as ICoreClientAPI, showStackSize: false)
                };

                _toolModes = _toolModes.Append(toolMode);
            }
        }

        return _toolModes;
    }

    private void SetStackMaterials(ItemStack oldStack, Materials materials, Dictionary<string, string> attributes, out ItemStack newStack)
    {
        Materials newMaterials = materials.Clone();
        newStack = oldStack.Clone();

        foreach ((string key, string value) in attributes)
        {
            newMaterials.SetValue(key, value);
        }

        newMaterials.ToStack(newStack);
    }
}