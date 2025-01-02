using HarmonyLib;
using Vintagestory.API.Common;

namespace TabletopGames;

public class Core : ModSystem
{
    public static ICoreAPI apiForHarmony;

    private ICoreAPI api;
    private Harmony HarmonyInstance => new Harmony(Mod.Info.ModID);

    public override void StartPre(ICoreAPI api)
    {
        HarmonyInstance.PatchAll();
        apiForHarmony = api;

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
        RegisterBehaviors();
        RegisterBlockEntities();
        api.World.Logger.Event("started '{0}' mod", Mod.Info.Name);
    }

    public override void Dispose()
    {
        HarmonyInstance.UnpatchAll(HarmonyInstance.Id);
    }

    private void RegisterBlocks()
    {
        api.RegisterBlockClass("TabletopGames.BlockBoard", typeof(BlockBoard));
    }

    private void RegisterItems()
    {
        api.RegisterItemClass("TabletopGames.ItemShapeTexturesFromAttributes", typeof(ItemShapeTexturesFromAttributes));
        api.RegisterItemClass("TabletopGames.ItemBoardPiece", typeof(ItemBoardPiece));
        api.RegisterItemClass("TabletopGames.ItemIntermediate", typeof(ItemIntermediate));
    }

    private void RegisterBehaviors()
    {
        api.RegisterCollectibleBehaviorClass("TabletopGames.AdvancedToolModes", typeof(CollectibleBehaviorAdvancedToolModes));
    }

    private void RegisterBlockEntities()
    {
        api.RegisterBlockEntityClass("TabletopGames.Board", typeof(BlockEntityBoard));
    }
}
