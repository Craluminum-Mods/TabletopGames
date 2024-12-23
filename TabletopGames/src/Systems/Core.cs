using Vintagestory.API.Common;

namespace TabletopGames;

public class Core : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        api.World.Logger.Event("started '{0}' mod", Mod.Info.Name);
    }
}
