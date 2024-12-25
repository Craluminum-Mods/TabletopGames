using Vintagestory.API.Common;

namespace TabletopGames;

public class Core : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        api.RegisterBlockClass("TabletopGames.BlockShapeTexturesFromAttributes", typeof(BlockShapeTexturesFromAttributes));
        api.RegisterBlockEntityClass("TabletopGames.BEShapeTexturesFromAttributes", typeof(BEShapeTexturesFromAttributes));
        api.World.Logger.Event("started '{0}' mod", Mod.Info.Name);
    }
}
