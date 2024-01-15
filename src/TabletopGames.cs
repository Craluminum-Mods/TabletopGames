using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

[assembly: ModInfo(name: "Tabletop Games", modID: "tabletopgames")]

namespace TabletopGames
{
    class TabletopGames : ModSystem
    {
        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.RegisterBlockClass("TabletopGames_ChessBoard", typeof(BlockChessBoard));
            api.RegisterBlockClass("TabletopGames_DominoBoard", typeof(BlockDominoBoard));
            api.RegisterBlockClass("TabletopGames_DominoBox", typeof(BlockDominoBox));
            api.RegisterBlockClass("TabletopGames_GoBoard", typeof(BlockGoBoard));
            // api.RegisterBlockClass("TabletopGames_PlayingSurface", typeof(BlockPlayingSurface));
            api.RegisterBlockClass("TabletopGames_MorrisBoard", typeof(BlockMorrisBoard));

            api.RegisterBlockEntityClass("TabletopGames_BEChessBoard", typeof(BEChessBoard));
            api.RegisterBlockEntityClass("TabletopGames_BEDominoBoard", typeof(BEDominoBoard));
            api.RegisterBlockEntityClass("TabletopGames_BEDominoBox", typeof(BEDominoBox));
            api.RegisterBlockEntityClass("TabletopGames_BEGoBoard", typeof(BEGoBoard));
            // api.RegisterBlockEntityClass("TabletopGames_BEPlayingSurface", typeof(BEPlayingSurface));
            api.RegisterBlockEntityClass("TabletopGames_BEMorrisBoard", typeof(BEMorrisBoard));

            api.RegisterItemClass("TabletopGames_ItemBox", typeof(ItemBox));
            api.RegisterItemClass("TabletopGames_ItemChecker", typeof(ItemChecker));
            api.RegisterItemClass("TabletopGames_ItemChessPiece", typeof(ItemChessPiece));
            api.RegisterItemClass("TabletopGames_ItemDominoPiece", typeof(ItemDominoPiece));
            api.RegisterItemClass("TabletopGames_ItemGoStone", typeof(ItemGoStone));
            api.RegisterItemClass("TabletopGames_ItemShogiPiece", typeof(ItemShogiPiece));
            // api.RegisterItemClass("TabletopGames_ItemPlayingCard", typeof(ItemPlayingCard));
            // api.RegisterItemClass("TabletopGames_ItemPlayingCards", typeof(ItemPlayingCards));

            api.RegisterBlockBehaviorClass("TabletopGames_CustomPlacedWorldInteractions", typeof(BlockBehaviorCustomPlacedWorldInteractions));

            api.RegisterCollectibleBehaviorClass("TabletopGames_AppendTextToHeldInfo", typeof(CollectibleBehaviorAppendTextToHeldInfo));
            api.RegisterCollectibleBehaviorClass("TabletopGames_CustomToolModes", typeof(CollectibleBehaviorCustomToolModes));

            api.World.Logger.Event("started 'Tabletop Games' mod");
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);

            foreach (TransformConfig config in  Constants.TransformConfigs)
            {
                if (!GuiDialogTransformEditor.extraTransforms.Contains(config))
                {
                    GuiDialogTransformEditor.extraTransforms.Add(config);
                }
            }
        }
    }
}