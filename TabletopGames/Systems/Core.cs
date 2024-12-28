using Vintagestory.API.Common;

namespace TabletopGames;

public class Core : ModSystem
{
    private ICoreAPI api;

    public override void StartPre(ICoreAPI api)
    {
        if (api.ModLoader.IsModEnabled("configlib"))
        {
            _ = new ConfigLibCompatibility(api);
        }
    }

    public override void Start(ICoreAPI api)
    {
        this.api = api;
        RegisterBlocks();
        RegisterItems();
        RegisterBlockEntities();
        api.World.Logger.Event("started '{0}' mod", Mod.Info.Name);
    }

    private void RegisterBlocks()
    {
        api.RegisterBlockClass("TabletopGames.BlockShapeTexturesFromAttributes", typeof(BlockShapeTexturesFromAttributes));
        api.RegisterBlockClass("TabletopGames.BlockBoard", typeof(BlockBoard));
    }

    private void RegisterBlockEntities()
    {
        api.RegisterBlockEntityClass("TabletopGames.ShapeTexturesFromAttributes", typeof(BEShapeTexturesFromAttributes));
        api.RegisterBlockEntityClass("TabletopGames.Board", typeof(BlockEntityBoard));
    }

    private void RegisterItems()
    {
        api.RegisterItemClass("TabletopGames.ItemShapeTexturesFromAttributes", typeof(ItemShapeTexturesFromAttributes));
        api.RegisterItemClass("TabletopGames.ItemBoardPiece", typeof(ItemBoardPiece));
    }
}
