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
            if (TagsIgnored.Any())
            {
                dsc.AppendLine("DEBUG::" + nameof(TagsIgnored) + ": " + string.Join(", ", TagsIgnored));
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
            if (TagsIgnoredPerSlot.Any())
            {
                dsc.AppendLine($"DEBUG::{nameof(TagsIgnoredPerSlot)}: ");
                foreach ((string id, List<string> tags) in TagsIgnoredPerSlot)
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
            if (tabletopTags.TagsIgnored.Any())
            {
                dsc.AppendLine("DEBUG::Slot Ignored tags: " + string.Join(", ", tabletopTags.TagsIgnored));
            }
            return;
        }

        if (tabletopTags.Tags.Any())
        {
            dsc.AppendLine("DEBUG::Tags: " + string.Join(", ", tabletopTags.Tags));
        }
        if (tabletopTags.TagsIgnored.Any())
        {
            dsc.AppendLine("DEBUG::Ignored tags: " + string.Join(", ", tabletopTags.TagsIgnored));
        }
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