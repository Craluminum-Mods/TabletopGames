using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;

namespace TabletopGames;

public static class TabletopExtensions
{
    public static bool AreTagsCompatible(this List<string> boardTags, List<string> boardTagsIgnored, ItemStack stack)
    {
        List<string> stackTags = stack?.ItemAttributes?["tabletopTags"]?.AsObject<List<string>>();
        stackTags ??= new();
        boardTags ??= new();
        boardTagsIgnored ??= new();

        if (stackTags.Count == 0)
        {
            return false;
        }
        if (stackTags.Any(tag => boardTagsIgnored.Contains(tag)))
        {
            return false;
        }
        if (stackTags.Any(tag => boardTags.Contains(tag)))
        {
            return true;
        }
        return false;
    }

    public static bool AreTagsCompatible(this BlockBoard board, ItemStack stack)
    {
        List<string> boardTags = board.TabletopTags;
        List<string> boardTagsIgnored = board.TabletopTagsIgnored;
        return boardTags.AreTagsCompatible(boardTagsIgnored, stack);
    }
}