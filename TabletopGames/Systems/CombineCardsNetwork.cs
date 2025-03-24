using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace TabletopGames;

public class CombineCardsNetwork : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        api.Network
            .RegisterChannel("tabletopgames:combinecards")
            .RegisterMessageType(typeof(CombineCardsRequest));
    }

    #region Client
    IClientNetworkChannel clientChannel;
    ICoreClientAPI clientApi;

    public override void StartClientSide(ICoreClientAPI api)
    {
        clientApi = api;
        clientChannel = api.Network
            .GetChannel("tabletopgames:combinecards");

        api.Input.RegisterHotKey("tabletopgames:combinecards", Lang.Get("tabletopgames:hotkey-combinecards"), GlKeys.C, shiftPressed: true, ctrlPressed: true);
        api.Input.SetHotKeyHandler("tabletopgames:combinecards", HandleCombineCardsRequest);
    }

    private bool HandleCombineCardsRequest(KeyCombination keyCombination)
    {
        clientChannel.SendPacket(new CombineCardsRequest());
        return true;
    }
    #endregion

    #region Server
    IServerNetworkChannel serverChannel;
    ICoreServerAPI serverApi;

    public override void StartServerSide(ICoreServerAPI api)
    {
        serverApi = api;
        serverChannel = api.Network
            .GetChannel("tabletopgames:combinecards")
            .SetMessageHandler<CombineCardsRequest>(OnClientRequest);
    }

    private void OnClientRequest(IPlayer fromPlayer, CombineCardsRequest networkRequest)
    {
        ItemPlayingCard.CombineAllInInventory(fromPlayer);
    }
    #endregion
}