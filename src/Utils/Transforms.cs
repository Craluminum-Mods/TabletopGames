using System.Collections.Generic;
using Vintagestory.Client.NoObf;

namespace TabletopGames;

public static class Constants
{
    public static readonly List<TransformConfig> TransformConfigs = new()
    {
        new() { AttributeName = "onTabletopGamesChessBoard5x5Transform", Title = "Chessboard 5x5" },
        new() { AttributeName = "onTabletopGamesChessBoard8x8Transform", Title = "Chessboard 8x8" },
        new() { AttributeName = "onTabletopGamesChessBoard10x10Transform", Title = "Chessboard 10x10" },
        new() { AttributeName = "onTabletopGamesChessBoard12x12Transform", Title = "Chessboard 12x12" },
        new() { AttributeName = "onTabletopGamesChessBoard14x14Transform", Title = "Chessboard 14x14" },
        new() { AttributeName = "onTabletopGamesChessBoard16x16Transform", Title = "Chessboard 16x16" },
        new() { AttributeName = "onTabletopGamesChessBoard91hexTransform", Title = "Chessboard 91hex" },
        new() { AttributeName = "onTabletopGamesDominoBoardTransform", Title = "Domino board" },
        new() { AttributeName = "onTabletopGames_GoBoard_9x9_Transform", Title = "Go board 9x9" },
        new() { AttributeName = "onTabletopGames_GoBoard_19x19_Transform", Title = "Go board 19x19" },
        new() { AttributeName = "onTabletopGamesShogiBoard9x9Transform", Title = "Shogi board 19x19" },
    };
}
