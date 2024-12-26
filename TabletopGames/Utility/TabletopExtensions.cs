using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;

namespace TabletopGames;

public static class TabletopExtensions
{
    public static bool AreTagsCompatible(this List<string> boardTags, List<string> boardTagsIgnored, ItemStack stack)
    {
        if (!boardTags?.Any() ?? true)
        {
            return true;
        }

        List<string> stackTags = stack?.ItemAttributes?["tabletopTags"]?.AsObject<List<string>>();
        if (!stackTags?.Any() ?? true)
        {
            return false;
        }

        HashSet<string> tags = new HashSet<string>(boardTags.Intersect(boardTagsIgnored ??= new List<string>()));
        return !stackTags.Any(tags.Contains);
    }

    public static bool AreTagsCompatible(this BlockBoard board, ItemStack stack)
    {
        List<string> boardTags = board.TabletopTags;
        List<string> boardTagsIgnored = board.TabletopTagsIgnored;
        return boardTags.AreTagsCompatible(boardTagsIgnored, stack);
    }
}