using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace TabletopGames;

public class CollectibleBehaviorBoardPieceToolModes : CollectibleBehavior
{
    private Dictionary<string, List<Dictionary<string, string>>> setAttributesByType;

    public CollectibleBehaviorBoardPieceToolModes(CollectibleObject collObj) : base(collObj) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        setAttributesByType = properties["setAttributes"].AsObject(defaultValue: new Dictionary<string, List<Dictionary<string, string>>>());
    }

    public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
    {
        if (slot.Empty)
        {
            return;
        }

        Materials materials = Materials.FromStack(slot.Itemstack);
        if (!materials.FindByMaterial(setAttributesByType, out List<Dictionary<string, string>> setAttributes) || setAttributes == null || !setAttributes.Any())
        {
            return;
        }

        if (setAttributes.Count <= toolMode || setAttributes[toolMode] == null)
        {
            return;
        }

        ItemStack newStack = slot.Itemstack.Clone();
        foreach ((string key, string value) in setAttributes[toolMode])
        {
            materials.SetValue(key, value);
        }

        materials.ToStack(newStack);
        slot.Itemstack.SetFrom(newStack);
        slot.MarkDirty();
    }

    public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
    {
        if (slot.Empty)
        {
            return null;
        }

        Materials materials = Materials.FromStack(slot.Itemstack);
        if (!materials.FindByMaterial(setAttributesByType, out List<Dictionary<string, string>> setAttributes) || setAttributes == null || !setAttributes.Any())
        {
            return null;
        }
        
        SkillItem[] _toolModes = Array.Empty<SkillItem>();

        for (int i = 0; i < setAttributes.Count; i++)
        {
            ItemStack newStack = slot.Itemstack.Clone();
            Materials _materials = materials.Clone();

            foreach ((string key, string value) in setAttributes[i])
            {
                _materials.SetValue(key, value);
            }

            _materials.ToStack(newStack);

            SkillItem toolMode = new()
            {
                Name = newStack.GetName(),
                RenderHandler = newStack.RenderItemStack(forPlayer.Entity.Api as ICoreClientAPI, showStackSize: false)
            };

            _toolModes = _toolModes.Append(toolMode);
        }
        return _toolModes;
    }
}
