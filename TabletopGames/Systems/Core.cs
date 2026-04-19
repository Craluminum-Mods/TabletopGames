global using AttributeRenderingLibrary;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using TabletopGames.Configuration;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TabletopGames;

public class Core : ModSystem
{
    public ConfigClient ConfigClient { get; set; }

    private ICoreAPI api;
    private Harmony HarmonyInstance => new Harmony(Mod.Info.ModID);

    public static Core GetInstance(ICoreAPI api) => api.ModLoader.GetModSystem<Core>();

    public override void StartPre(ICoreAPI api)
    {
        GlobalConstants.IgnoredStackAttributes = GlobalConstants.IgnoredStackAttributes.Append("rotateYaw");
        GlobalConstants.IgnoredStackAttributes = GlobalConstants.IgnoredStackAttributes.Append("rotateY");
        GlobalConstants.IgnoredStackAttributes = GlobalConstants.IgnoredStackAttributes.Append("scale");

        HarmonyInstance.PatchAllUncategorized();

        if (api.Side.IsClient())
        {
            ConfigClient = ModConfig.ReadConfig<ConfigClient>(api, ConfigClient.ConfigName);
        }

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

    public override void AssetsFinalize(ICoreAPI api)
    {
        foreach (var item in api.World.Items)
        {
            if (item is ItemStone)
            {
                item.Attributes ??= new JsonObject(new JObject());
                item.Attributes.Token?["knappable"] = JToken.FromObject(true);
            }
        }
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        HarmonyInstance.PatchCategory("Client");
    }

    public override void Dispose()
    {
        HarmonyInstance.UnpatchAll(HarmonyInstance.Id);
    }

    private void RegisterItems()
    {
        api.RegisterItemClass("TabletopGames.ItemChiseledPiece", typeof(ItemChiseledPiece));
        api.RegisterItemClass("TabletopGames.ItemContainer", typeof(ItemContainer));
        api.RegisterItemClass("TabletopGames.ItemContainerWithDetachableLid", typeof(ItemContainerWithDetachableLid));
        api.RegisterItemClass("TabletopGames.ItemDice", typeof(ItemDice));
        api.RegisterItemClass("TabletopGames.ItemPlayingCard", typeof(ItemPlayingCard));
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
        api.RegisterCollectibleBehaviorClass("TabletopGames.ShapeTexturesFromAttributes.RotateFromOrigin", typeof(TabletopGames.CollectibleBehaviorSTFARotateFromOrigin));
        api.RegisterCollectibleBehaviorClass("TabletopGames.Rollable", typeof(TabletopGames.CollectibleBehaviorRollable));
        api.RegisterCollectibleBehaviorClass("TabletopGames.AdvancedToolModes", typeof(CollectibleBehaviorAdvancedToolModes));
        api.RegisterCollectibleBehaviorClass("TabletopGames.Containable", typeof(CollectibleBehaviorContainable));
        api.RegisterCollectibleBehaviorClass("TabletopGames.ContainableTyped", typeof(CollectibleBehaviorContainableTyped));
        api.RegisterCollectibleBehaviorClass("TabletopGames.DetachableLid", typeof(CollectibleBehaviorDetachableLid));
        api.RegisterCollectibleBehaviorClass("TabletopGames.InteractionHelpConstructor", typeof(CollectibleBehaviorInteractionHelpConstructor));
        api.RegisterCollectibleBehaviorClass("TabletopGames.RandomizeInSlot", typeof(CollectibleBehaviorRandomizeInSlot));
        api.RegisterCollectibleBehaviorClass("TabletopGames.PieceTags", typeof(CollectibleBehaviorPieceTags));
        api.RegisterCollectibleBehaviorClass("TabletopGames.ChiseledPieceToolModes", typeof(CollectibleBehaviorChiseledPieceToolModes));
        api.RegisterCollectibleBehaviorClass("TabletopGames.ChiseledBoardToolModes", typeof(CollectibleBehaviorChiseledBoardToolModes));
        api.RegisterCollectibleBehaviorClass("TabletopGames.PlayingCardInteractions", typeof(CollectibleBehaviorPlayingCardInteractions));
        api.RegisterCollectibleBehaviorClass("TabletopGames.PlayingCardToolModes", typeof(CollectibleBehaviorPlayingCardToolModes));
        api.RegisterCollectibleBehaviorClass("TabletopGames.PackTyped", typeof(CollectibleBehaviorPackTyped));
        api.RegisterCollectibleBehaviorClass("TabletopGames.Shuffler", typeof(CollectibleBehaviorShuffler));
        api.RegisterCollectibleBehaviorClass("TabletopGames.ShufflerContainable", typeof(CollectibleBehaviorShufflerContainable));
        api.RegisterCollectibleBehaviorClass("TabletopGames.Intermediate", typeof(CollectibleBehaviorIntermediate));
        api.RegisterCollectibleBehaviorClass("TabletopGames.RotatableDisplayableProps", typeof(CollectibleBehaviorRotatableDisplayableProps));

        api.RegisterBlockBehaviorClass("TabletopGames.BoardTags", typeof(BlockBehaviorBoardTags));
        api.RegisterBlockBehaviorClass("TabletopGames.BoardData", typeof(BlockBehaviorBoardData));
        api.RegisterBlockBehaviorClass("TabletopGames.ChiseledBoardTags", typeof(BlockBehaviorChiseledBoardTags));

        api.RegisterBlockEntityBehaviorClass("TabletopGames.BoardPreviewRenderer", typeof(BEBehaviorBoardPreviewRenderer));
        api.RegisterBlockEntityBehaviorClass("TabletopGames.BoardSelection", typeof(BEBehaviorBoardSelection));
        api.RegisterBlockEntityBehaviorClass("TabletopGames.BoardInteractions", typeof(BEBehaviorBoardInteractions));
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
