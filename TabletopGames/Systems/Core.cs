using HarmonyLib;
using TabletopGames.Configuration;
using Vintagestory.API.Common;

namespace TabletopGames;

public class Core : ModSystem
{
    public ConfigClient ConfigClient { get; set; }

    private ICoreAPI api;
    private Harmony HarmonyInstance => new Harmony(Mod.Info.ModID);

    public static Core GetInstance(ICoreAPI api) => api.ModLoader.GetModSystem<Core>();

    public override void StartPre(ICoreAPI api)
    {
        if (api.Side.IsClient())
        {
            ConfigClient = ModConfig.ReadConfig<ConfigClient>(api, ConfigClient.ConfigName);
        }

        HarmonyInstance.PatchAll();

        if (api.ModLoader.IsModEnabled("configlib"))
        {
            new ConfigLibCompatibility(api);
        }
    }

    public override void Start(ICoreAPI api)
    {
        this.api = api;
        RegisterItems();
        RegisterBlocks();
        RegisterBlockEntities();
        RegisterBehaviors();
        InitializeWorldConfigs();
        Mod.Logger.Event("started '{0}' mod", Mod.Info.Name);
    }

    public override void Dispose()
    {
        HarmonyInstance.UnpatchAll(HarmonyInstance.Id);
    }

    private void RegisterItems()
    {
        api.RegisterItemClass("TabletopGames.ItemBoardPiece", typeof(ItemBoardPiece));
        api.RegisterItemClass("TabletopGames.ItemChiseledPiece", typeof(ItemChiseledPiece));
        api.RegisterItemClass("TabletopGames.ItemContainer", typeof(ItemContainer));
        api.RegisterItemClass("TabletopGames.ItemContainerWithDetachableLid", typeof(ItemContainerWithDetachableLid));
        api.RegisterItemClass("TabletopGames.ItemDice", typeof(ItemDice));
        api.RegisterItemClass("TabletopGames.ItemIntermediate", typeof(ItemIntermediate));
        api.RegisterItemClass("TabletopGames.ItemShapeTexturesFromAttributes", typeof(ItemShapeTexturesFromAttributes));
    }

    private void RegisterBlocks()
    {
        api.RegisterBlockClass("TabletopGames.BlockBoard", typeof(BlockBoard));
        api.RegisterBlockClass("TabletopGames.BlockChiseledBoard", typeof(BlockChiseledBoard));
    }

    private void RegisterBlockEntities()
    {
        api.RegisterBlockEntityClass("TabletopGames.Board", typeof(BlockEntityBoard));
        api.RegisterBlockEntityClass("TabletopGames.ChiseledBoard", typeof(BlockEntityChiseledBoard));
    }

    private void RegisterBehaviors()
    {
        api.RegisterCollectibleBehaviorClass("TabletopGames.AdvancedToolModes", typeof(CollectibleBehaviorAdvancedToolModes));
        api.RegisterCollectibleBehaviorClass("TabletopGames.Containable", typeof(CollectibleBehaviorContainable));
        api.RegisterCollectibleBehaviorClass("TabletopGames.ContainableTyped", typeof(CollectibleBehaviorContainableTyped));
        api.RegisterCollectibleBehaviorClass("TabletopGames.ContainedTransform", typeof(CollectibleBehaviorContainedTransform));
        api.RegisterCollectibleBehaviorClass("TabletopGames.DetachableLid", typeof(CollectibleBehaviorDetachableLid));
        api.RegisterCollectibleBehaviorClass("TabletopGames.InteractionHelpConstructor", typeof(CollectibleBehaviorInteractionHelpConstructor));
        api.RegisterCollectibleBehaviorClass("TabletopGames.RandomizeInSlot", typeof(CollectibleBehaviorRandomizeInSlot));
        api.RegisterCollectibleBehaviorClass("TabletopGames.PieceTags", typeof(CollectibleBehaviorPieceTags));
        api.RegisterCollectibleBehaviorClass("TabletopGames.ChiseledPieceToolModes", typeof(CollectibleBehaviorChiseledPieceToolModes));

        api.RegisterBlockBehaviorClass("TabletopGames.ExtraBlockInteractionHelp", typeof(BlockBehaviorExtraBlockInteractionHelp));
        api.RegisterBlockBehaviorClass("TabletopGames.BoardTags", typeof(BlockBehaviorBoardTags));
        api.RegisterBlockBehaviorClass("TabletopGames.BoardData", typeof(BlockBehaviorBoardData));
        api.RegisterBlockBehaviorClass("TabletopGames.ChiseledBoardTags", typeof(BlockBehaviorChiseledBoardTags));

        api.RegisterBlockEntityBehaviorClass("TabletopGames.BoardPreviewRenderer", typeof(BEBehaviorBoardPreviewRenderer));
        api.RegisterBlockEntityBehaviorClass("TabletopGames.BoardSelection", typeof(BEBehaviorBoardSelection));
        api.RegisterBlockEntityBehaviorClass("TabletopGames.ChiseledBoardSelection", typeof(BEBehaviorChiseledBoardSelection));
    }

    private void InitializeWorldConfigs()
    {
        if (!api.World.Config.HasAttribute("tabletopgames_chiseledPieceMaxUp"))
        {
            api.World.Config.SetInt("tabletopgames_chiseledPieceMaxUp", 6);
        }
        if (!api.World.Config.HasAttribute("tabletopgames_chiseledPieceMaxDown"))
        {
            api.World.Config.SetInt("tabletopgames_chiseledPieceMaxDown", 2);
        }
    }
}
