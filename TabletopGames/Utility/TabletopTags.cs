using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;

namespace TabletopGames;

public class TabletopTags
{
    public Dictionary<string, List<string>> TagsPerSlot { get; set; } = new();
    public Dictionary<string, List<string>> TagsIgnoredPerSlot { get; set; } = new();

    public List<string> Tags { get; set; } = new();
    public List<string> TagsIgnored { get; set; } = new();

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
        TabletopTags stackTags = stack?.ItemAttributes?["tabletopTags"]?.AsObject(new TabletopTags());
        return AreTagsCompatible(boardTags, stackTags);
    }
}