using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TabletopGames;

public interface IBoardTagsSupplier
{
    public TabletopTags GetResolvedTags(IWorldAccessor world, BlockPos pos, int slotId);
    public TabletopTags GetUnresolvedTags(IWorldAccessor world, BlockPos pos);
}
