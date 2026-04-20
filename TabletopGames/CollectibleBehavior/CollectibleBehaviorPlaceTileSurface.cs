using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TabletopGames;

/// <summary>
/// Spawns "tilesurface" block on top of the solid surface, while also placing held item in the center
/// </summary>
public class CollectibleBehaviorPlaceTileSurface(CollectibleObject collObj) : CollectibleBehavior(collObj)
{
    public string interactionLangCode;

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        interactionLangCode = properties["interactionLangCode"].AsString("");
    }

    public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
    {
        if (blockSel == null) return;
        if (!byEntity.Controls.ShiftKey) return;
        if (blockSel.Face != BlockFacing.UP) return;

        Block targetBlock = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
        if (!targetBlock.SideIsSolid(blockSel.Position, BlockFacing.indexUP)) return;

        BlockSelection newBlockSelection = blockSel.AddPosCopy(0, 1, 0);
        if (byEntity.World.BlockAccessor.GetBlock(newBlockSelection.Position).BlockId != 0) return;

        Block? newBlock = byEntity.Api.World.GetBlock("tabletopgames:tilesurface");
        if (newBlock == null) return;

        byEntity.World.BlockAccessor.SetBlock(newBlock.Id, newBlockSelection.Position);
        byEntity.World.BlockAccessor.TriggerNeighbourBlockUpdate(newBlockSelection.Position);
        
        if (byEntity.World.BlockAccessor.GetBlockEntity(newBlockSelection.Position)?.GetBehavior<BEBehaviorDisplay>() is { } bebehavior)
        {
            BlockSelection selectionWithCenterId = newBlockSelection.Clone();

            // TODO: dynamically change SelectionBoxId based on item size and blockSel.HitPosition
            selectionWithCenterId.SelectionBoxId = "0-8-8";

            EnumHandling tempHandling = EnumHandling.PassThrough;
            bebehavior.OnBlockInteractStart(byEntity.World, (byEntity as EntityPlayer)?.Player, selectionWithCenterId, ref tempHandling);
        }

        handHandling = EnumHandHandling.PreventDefault;
        handling = EnumHandling.PreventSubsequent;
    }

    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
    {
        handling = EnumHandling.Handled;

        return base.GetHeldInteractionHelp(inSlot, ref handling).Append(new WorldInteraction
        {
            ActionLangCode = interactionLangCode,
            HotKeyCode = "shift"
        });
    }
}