using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace TabletopGames;

public class TabletopTags
{
    /// <summary>
    /// When set, normal tabletopTags are ignored
    /// </summary>
    public Dictionary<string, List<string>> TagsPerSlot { get; set; } = new();

    /// <summary>
    /// When set, normal tabletopTags are ignored
    /// </summary>
    public Dictionary<string, List<string>> TagsIgnoredPerSlot { get; set; } = new();

    public List<string> Tags { get; set; } = new();
    public List<string> TagsIgnored { get; set; } = new();

    public TabletopTags GetResolvedTags(int slotId = -1)
    {
        if (slotId >= 0 && (TagsPerSlot.Any() || TagsIgnoredPerSlot.Any()))
        {
            TabletopTags newTags = new();
            string id = slotId.ToString();

            newTags.Tags = GetTagsForSlot(TagsPerSlot, slotId.ToString());
            newTags.TagsIgnored = GetTagsForSlot(TagsIgnoredPerSlot, slotId.ToString());
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

    public void GetDescription(StringBuilder dsc, int slotIndex = -1)
    {
        if (!TabletopDebug.TagsDebugInfo)
        {
            return;
        }

        TabletopTags tabletopTags = GetResolvedTags(slotIndex);
        if (slotIndex >= 0)
        {
            dsc.AppendLine("Slot Tags: " + string.Join(", ", tabletopTags.Tags));
            dsc.AppendLine("Slot Ignored tags: " + string.Join(", ", tabletopTags.TagsIgnored));
            return;
        }

        dsc.AppendLine("Tags: " + string.Join(", ", tabletopTags.Tags));
        dsc.AppendLine("Ignored tags: " + string.Join(", ", tabletopTags.TagsIgnored));
    }

    public static TabletopTags FromStack(ItemStack stack)
    {
        return stack?.ItemAttributes?["tabletopTags"]?.AsObject(new TabletopTags());
    }

    public static bool AreTagsCompatible(TabletopTags boardTags, TabletopTags stackTags)
    {
        stackTags ??= new();
        boardTags ??= new();
        if (stackTags.Tags.Count == 0)
        {
            return false;
        }
        if (stackTags.Tags.Any(boardTags.TagsIgnored.Contains))
        {
            return false;
        }
        return stackTags.Tags.Any(boardTags.Tags.Contains);
    }

    public static bool AreTagsCompatible(TabletopTags boardTags, ItemStack stack)
    {
        return AreTagsCompatible(boardTags, FromStack(stack));
    }
}