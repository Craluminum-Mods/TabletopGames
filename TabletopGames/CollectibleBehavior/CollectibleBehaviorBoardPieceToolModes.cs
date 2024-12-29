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
    private SkillItem[] toolModes;
    private Dictionary<string, List<Dictionary<string, string>>> setAttributesByType;

    public CollectibleBehaviorBoardPieceToolModes(CollectibleObject collObj) : base(collObj) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        setAttributesByType = properties["setAttributes"].AsObject(defaultValue: new Dictionary<string, List<Dictionary<string, string>>>());
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        if (toolModes != null && toolModes.Any())
        {
            for (int i = 0; i < toolModes.Length; i++)
            {
                toolModes[i]?.Dispose();
            }
        }
        toolModes = null;
    }

    public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
    {
        if (slot.Empty || toolModes == null || toolModes.Length <= toolMode)
        {
            return;
        }

        Dictionary<string, string> attributes = toolModes[toolMode].Data as Dictionary<string, string>;
        attributes ??= new Dictionary<string, string>();
        Materials materials = Materials.FromStack(slot.Itemstack);

        foreach ((string key, string value) in attributes)
        {
            materials.SetValue(key, value);
        }
        materials.ToStack(slot.Itemstack);
        slot.MarkDirty();
    }

    public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
    {
        if (slot.Empty)
        {
            return null;
        }

        Materials materials = Materials.FromStack(slot.Itemstack);
        if (!materials.FindByMaterial(setAttributesByType, out List<Dictionary<string, string>> setAttributes))
        {
            return null;
        }
        
        SkillItem[] _toolModes = Array.Empty<SkillItem>();

        foreach (Dictionary<string, string> attributes in setAttributes)
        {
            ItemStack newStack = slot.Itemstack.Clone();
            Materials _materials = materials.Clone();

            foreach ((string key, string value) in attributes)
            {
                _materials.SetValue(key, value);
            }

            _materials.ToStack(newStack);

            SkillItem toolMode = new()
            {
                Name = newStack.GetName(),
                RenderHandler = newStack.RenderItemStack(forPlayer.Entity.Api as ICoreClientAPI, showStackSize: false),
                Data = attributes
            };
            _toolModes = _toolModes.Append(toolMode);
        }
        toolModes = _toolModes;
        return toolModes;
    }
}
