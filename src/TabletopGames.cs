using Vintagestory.API.Common;

[assembly: ModInfo("Tabletop Games",
Authors = new[] { "Craluminum2413" })]

namespace TabletopGames
{
    class TabletopGames : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockClass("TabletopGames_DominoBox", typeof(BlockDominoBox));
            api.RegisterBlockClass("TabletopGames_ChessBoard", typeof(BlockChessBoard));
            api.RegisterBlockClass("TabletopGames_DominoBoard", typeof(BlockDominoBoard));
            api.RegisterBlockEntityClass("TabletopGames_BEDominoBox", typeof(BEDominoBox));
            api.RegisterBlockEntityClass("TabletopGames_BEChessBoard", typeof(BEChessBoard));
            api.RegisterBlockEntityClass("TabletopGames_BEDominoBoard", typeof(BEDominoBoard));
            api.RegisterItemClass("TabletopGames_ItemChecker", typeof(ItemChecker));
            api.RegisterItemClass("TabletopGames_ItemChessPiece", typeof(ItemChessPiece));
            api.RegisterItemClass("TabletopGames_ItemDominoPiece", typeof(ItemDominoPiece));
            api.RegisterItemClass("TabletopGames_ItemBox", typeof(ItemBox));
            api.World.Logger.Event("started 'Tabletop Games' mod");
        }
    }
}