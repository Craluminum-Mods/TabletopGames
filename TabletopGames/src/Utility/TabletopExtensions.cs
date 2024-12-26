using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;

namespace TabletopGames;

public static class TabletopExtensions
{
    public static bool AreStorageAttributesCompatible(this List<string> boardStorageAttributes, List<string> stackStorageAttributes)
    {
        if (boardStorageAttributes?.Any() == false)
        {
            return true;
        }

        if (stackStorageAttributes?.Any() == false)
        {
            return false;
        }

        return boardStorageAttributes.Any(stackStorageAttributes.Contains);
    }

    public static bool AreStorageAttributesCompatible(this BlockBoard board, ItemStack stack)
    {
        List<string> boardStorageAttributes = board.StorageAttributes;
        if (boardStorageAttributes?.Any() == false)
        {
            return true;
        }

        List<string> stackStorageAttributes = stack?.Collectible?.Attributes?["storageAttributes"]?.AsObject<List<string>>();
        if (stackStorageAttributes?.Any() == false)
        {
            return false;
        }

        return boardStorageAttributes.Any(stackStorageAttributes.Contains);
    }
}