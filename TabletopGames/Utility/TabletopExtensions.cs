using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;

namespace TabletopGames;

public static class TabletopExtensions
{
    public static bool AreStorageAttributesCompatible(this List<string> boardStorageAttributes, List<string> stackStorageAttributes)
    {
        if (boardStorageAttributes == null || !boardStorageAttributes.Any())
        {
            return true;
        }

        if (stackStorageAttributes == null || !stackStorageAttributes.Any())
        {
            return false;
        }

        return boardStorageAttributes.Any(stackStorageAttributes.Contains);
    }

    public static bool AreStorageAttributesCompatible(this BlockBoard board, ItemStack stack)
    {
        List<string> boardStorageAttributes = board.StorageAttributes;
        if (boardStorageAttributes == null || !boardStorageAttributes.Any())
        {
            return true;
        }

        List<string> stackStorageAttributes = stack?.Collectible?.Attributes?["storageAttributes"]?.AsObject<List<string>>();
        if (stackStorageAttributes == null || !stackStorageAttributes.Any())
        {
            return false;
        }

        return boardStorageAttributes.Any(stackStorageAttributes.Contains);
    }
}