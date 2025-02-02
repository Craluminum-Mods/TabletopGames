using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;

namespace TabletopGames;

/// <summary>
/// Represents a basic board block for tabletop games.
/// This class extends <see cref="BlockShapeTexturesFromAttributes"/> to manage board-specific properties,
/// including board data and tabletop tags.
/// </summary>
public class BlockBoard : BlockShapeTexturesFromAttributes
{
    public Dictionary<string, BoardData> BoardDataByType { get; protected set; } = new();
    public Dictionary<string, TabletopTags> TabletopTagsByType { get; protected set; } = new();

    public override void LoadTypes()
    {
        base.LoadTypes();

        if (Attributes != null)
        {
            BoardDataByType = Attributes["boardData"].AsObject(defaultValue: new Dictionary<string, BoardData>());
            TabletopTagsByType = Attributes["tabletopTags"].AsObject(defaultValue: new Dictionary<string, TabletopTags>());
        }

        foreach (BoardData boardData in BoardDataByType.Values)
        {
            if (!GuiDialogTransformEditor.extraTransforms.Any(x => x.AttributeName == boardData.AttributeTransformCode))
            {
                GuiDialogTransformEditor.extraTransforms.Add(new TransformConfig()
                {
                    Title = Lang.Get($"{TabletopConstants.ModID}:transform-{boardData.AttributeTransformCode}"),
                    AttributeName = boardData.AttributeTransformCode
                });
            }
        }
    }

    public virtual BoardData GetBoardData(Variants variants)
    {
        return variants.FindByVariant(BoardDataByType, out BoardData value) ? value : new BoardData();
    }

    public virtual TabletopTags GetTags(Variants variants, int slotId, bool resolveTags = true)
    {
        if (variants.FindByVariant(TabletopTagsByType, out TabletopTags tags))
        {
            return resolveTags ? tags.GetResolvedTags(slotId) : tags;
        }
        return new TabletopTags();
    }

    public virtual ItemSlot CreateSlot(Variants variants, InventoryBase inventory, int slotId)
    {
        BoardData boardData = GetBoardData(variants);
        TabletopTags tags = GetTags(variants, slotId);
        EnumSlotType slotType = EnumSlotType.Normal;

        if (boardData.SlotTypes.Any())
        {
            string id = slotId.ToString();
            foreach ((string wildcard, EnumSlotType _slotType) in boardData.SlotTypes)
            {
                if (WildcardUtil.Match(wildcard, id))
                {
                    slotType = _slotType;
                    break;
                }
            }
        }
        return new ItemSlotTabletop(inventory, tags, slotType);
    }

    public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) => TabletopDebug.BoardParticleSelection;
    public override Vec4f GetSelectionColor(ICoreClientAPI capi, BlockPos pos) => TabletopDebug.BoardSelectionColor;
}