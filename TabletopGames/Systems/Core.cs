using HarmonyLib;
using TabletopGames.Configuration;
using Vintagestory.API.Common;

namespace TabletopGames;

public class Core : ModSystem
{
    public ConfigClient ConfigClient { get; set; }

    private ICoreAPI api;
    private Harmony HarmonyInstance => new Harmony(Mod.Info.ModID);

    public static Core GetInstance(ICoreAPI api)
    {
        return api.ModLoader.GetModSystem<Core>();
    }

    public override void StartPre(ICoreAPI api)
    {
        if (api.Side.IsClient())
        {
            ConfigClient = ModConfig.ReadConfig<ConfigClient>(api, ConfigClient.ConfigName);
        }

        HarmonyInstance.PatchAll();

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
        Mod.Logger.Event("started '{0}' mod", Mod.Info.Name);
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
        api.RegisterItemClass("TabletopGames.ItemIntermediate", typeof(ItemIntermediate));
        api.RegisterItemClass("TabletopGames.ItemBoardPiece", typeof(ItemBoardPiece));
        api.RegisterItemClass("TabletopGames.ItemDice", typeof(ItemDice));
        api.RegisterItemClass("TabletopGames.ItemContainer", typeof(ItemContainer));
        api.RegisterItemClass("TabletopGames.ItemContainerWithDetachableLid", typeof(ItemContainerWithDetachableLid));
    }

    private void RegisterBehaviors()
    {
        api.RegisterCollectibleBehaviorClass("TabletopGames.AdvancedToolModes", typeof(CollectibleBehaviorAdvancedToolModes));
        api.RegisterCollectibleBehaviorClass("TabletopGames.RandomizeInSlot", typeof(CollectibleBehaviorRandomizeInSlot));
        api.RegisterCollectibleBehaviorClass("TabletopGames.ContainableTyped", typeof(CollectibleBehaviorContainableTyped));
        api.RegisterCollectibleBehaviorClass("TabletopGames.Containable", typeof(CollectibleBehaviorContainable));
        api.RegisterCollectibleBehaviorClass("TabletopGames.DetachableLid", typeof(CollectibleBehaviorDetachableLid));
        api.RegisterBlockEntityBehaviorClass("TabletopGames.BoardPreviewRenderer", typeof(BEBehaviorBoardPreviewRenderer));
        api.RegisterBlockBehaviorClass("TabletopGames.ExtraBlockInteractionHelp", typeof(BlockBehaviorExtraBlockInteractionHelp));
        api.RegisterCollectibleBehaviorClass("TabletopGames.InteractionHelpConstructor", typeof(CollectibleBehaviorInteractionHelpConstructor));
        api.RegisterCollectibleBehaviorClass("TabletopGames.ContainedTransform", typeof(CollectibleBehaviorContainedTransform));
    }

    private void RegisterBlockEntities()
    {
        api.RegisterBlockEntityClass("TabletopGames.Board", typeof(BlockEntityBoard));
    }
}
