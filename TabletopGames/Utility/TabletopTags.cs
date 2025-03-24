using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace TabletopGames;

public class TabletopTags
{
    /// <summary> 
    /// When set, normal tags are ignored
    /// </summary>
    public Dictionary<string, List<string>> TagsPerSlot { get; set; } = new();

    public List<string> Tags { get; set; } = new();

    public static TabletopTags FromInterface(ItemStack stack)
    {
        return stack?.Collectible?.GetCollectibleInterface<IPieceTagsSupplier>()?.GetTags(stack) ?? new TabletopTags();
    }

    public static TabletopTags FromInterface(ItemSlot slot)
    {
        return FromInterface(slot?.Itemstack!) ?? new TabletopTags();
    }

    public static bool AreTagsCompatible(TabletopTags boardTags, TabletopTags pieceTags)
    {
        boardTags ??= new();
        pieceTags ??= new();
        return boardTags.Tags.Any(pieceTags.Tags.Contains);
    }

    public TabletopTags GetResolvedTags(int slotId = -1)
    {
        if (slotId >= 0 && TagsPerSlot.Any())
        {
            TabletopTags newTags = new();
            newTags.Tags = GetTagsForSlot(TagsPerSlot, slotId.ToString());
            return newTags;
        }
        return this;
    }

    public static List<string> GetTagsForSlot(Dictionary<string, List<string>> dict, string slotId)
    {
        foreach ((string wildcard, List<string> tags) in dict)
        {
            if (WildcardUtil.Match(wildcard, slotId))
            {
                return tags;
            }
        }
        return new List<string>();
    }

    public void GetDescription(StringBuilder dsc, int slotIndex = -1, bool verbose = false)
    {
        if (!TabletopDebug.TagsDebugInfo)
        {
            return;
        }

        if (verbose)
        {
            if (Tags.Any())
            {
                dsc.AppendLine("DEBUG::" + nameof(Tags) + ": " + string.Join(", ", Tags));
            }
            if (TagsPerSlot.Any())
            {
                dsc.AppendLine($"DEBUG::{nameof(TagsPerSlot)}: ");
                foreach ((string id, List<string> tags) in TagsPerSlot)
                {
                    if (tags.Any())
                    {
                        dsc.AppendLine($"\t[{id}] " + string.Join(", ", tags));
                    }
                }
            }
            return;
        }

        TabletopTags tabletopTags = GetResolvedTags(slotIndex);
        if (slotIndex >= 0)
        {
            if (tabletopTags.Tags.Any())
            {
                dsc.AppendLine("DEBUG::Slot Tags: " + string.Join(", ", tabletopTags.Tags));
            }
            return;
        }

        if (tabletopTags.Tags.Any())
        {
            dsc.AppendLine("DEBUG::Tags: " + string.Join(", ", tabletopTags.Tags));
        }
    }
}