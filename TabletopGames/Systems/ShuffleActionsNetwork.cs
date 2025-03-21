using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace TabletopGames;

public class ShuffleActionsNetwork : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        api.Network
            .RegisterChannel("tabletopgames:shuffle")
            .RegisterMessageType(typeof(ShuffleRequest))
            .RegisterMessageType(typeof(ShuffleResponse));
    }

    #region Client
    IClientNetworkChannel clientChannel;
    ICoreClientAPI clientApi;

    public override void StartClientSide(ICoreClientAPI api)
    {
        clientApi = api;
        clientChannel = api.Network
            .GetChannel("tabletopgames:shuffle")
            .SetMessageHandler<ShuffleResponse>(OnServerResponse);

        api.Input.RegisterHotKey("tabletopgames:shuffle", Lang.Get("tabletopgames:hotkey-shuffle"), GlKeys.S, shiftPressed: true, ctrlPressed: true);
        api.Input.SetHotKeyHandler("tabletopgames:shuffle", HandleShuffleRequest);
    }

    private void OnServerResponse(ShuffleResponse networkResponse)
    {
        if (!string.IsNullOrEmpty(networkResponse.errorCode))
        {
            clientApi.TriggerIngameError(
                sender: this,
                errorCode: networkResponse.errorCode,
                text: Lang.Get(networkResponse.errorCode));
        }
    }

    private bool HandleShuffleRequest(KeyCombination t1)
    {
        clientChannel.SendPacket(new ShuffleRequest());
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
            .GetChannel("tabletopgames:shuffle")
            .SetMessageHandler<ShuffleRequest>(OnClientRequest);
    }

    private void OnClientRequest(IPlayer fromPlayer, ShuffleRequest networkRequest)
    {
        string errorCode = string.Empty;
        ItemSlot hotbarSlot = fromPlayer.InventoryManager.ActiveHotbarSlot;
        if (ItemPlayingCard.CanShuffle(hotbarSlot))
        {
            ItemPlayingCard.Shuffle(hotbarSlot, serverApi.World);
        }
        else
        {
            ShuffleResponse response = new ShuffleResponse()
            {
                errorCode = "tabletopgames:ingameerror-shuffle-empty-slot"
            };

            serverChannel.SendPacket(response, fromPlayer as IServerPlayer);
        }
    }
    #endregion
}