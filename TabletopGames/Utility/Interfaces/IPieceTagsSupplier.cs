using Vintagestory.API.Common;

namespace TabletopGames;

public interface IPieceTagsSupplier
{
    public TabletopTags GetTags(ItemStack stack);
}
