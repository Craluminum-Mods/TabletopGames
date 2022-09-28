using Vintagestory.API.Common;

[assembly: ModInfo("Tabletop Games",
Authors = new[] { "Unknown" })]

namespace TabletopGames
{
    class TabletopGames : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterItemClass("TabletopGames_ItemChecker", typeof(ItemChecker));
            api.RegisterItemClass("TabletopGames_ItemChessPiece", typeof(ItemChessPiece));
            api.RegisterItemClass("TabletopGames_ItemDominoPiece", typeof(ItemDominoPiece));
            api.RegisterBlockClass("TabletopGames_Board", typeof(BlockBoard));
            api.RegisterBlockClass("TabletopGames_DominoBoard", typeof(BlockDominoBoard));
            api.RegisterBlockEntityClass("TabletopGames_BEBoard", typeof(BlockEntityBoard));
            api.RegisterBlockEntityClass("TabletopGames_BEDominoBoard", typeof(BlockEntityDominoBoard));
            api.World.Logger.Event("started 'Tabletop Games' mod");
        }
    }
}