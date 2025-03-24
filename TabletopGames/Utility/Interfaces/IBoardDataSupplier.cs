using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TabletopGames;

public interface IBoardDataSupplier
{
    public BoardData GetBoardData(IWorldAccessor? world, BlockPos pos);
    public BoardData GetBoardData(Variants variants);
}
