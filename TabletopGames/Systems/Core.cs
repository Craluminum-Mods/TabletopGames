using Vintagestory.API.Common;

namespace TabletopGames;

public class Core : ModSystem
{
    public override void StartPre(ICoreAPI api)
    {
        if (api.ModLoader.IsModEnabled("configlib"))
        {
            _ = new ConfigLibCompatibility(api);
        }
    }

    public override void Start(ICoreAPI api)
    {
        api.RegisterBlockClass("TabletopGames.BlockShapeTexturesFromAttributes", typeof(BlockShapeTexturesFromAttributes));
        api.RegisterBlockEntityClass("TabletopGames.BEShapeTexturesFromAttributes", typeof(BEShapeTexturesFromAttributes));

        api.RegisterBlockClass("TabletopGames.BlockBoard", typeof(BlockBoard));
        api.RegisterBlockEntityClass("TabletopGames.BEBoard", typeof(BlockEntityBoard));

        api.RegisterItemClass("TabletopGames.ItemShapeTexturesFromAttributes", typeof(ItemShapeTexturesFromAttributes));

        api.World.Logger.Event("started '{0}' mod", Mod.Info.Name);
    }
}
