using ProtoBuf;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent.Mechanics;

namespace TabletopGames.Network;

[ProtoContract]
public class RotatePieceRequest
{
    [ProtoMember(1)]
    public int direction = 1;

    public EnumRotDirection Direction => (EnumRotDirection)direction;
}

public class RotatePieceNetwork : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        api.Network
            .RegisterChannel("tabletopgames:rotatepiece")
            .RegisterMessageType(typeof(RotatePieceRequest));
    }

    #region Client
    IClientNetworkChannel clientChannel;
    ICoreClientAPI clientApi;

    public override void StartClientSide(ICoreClientAPI api)
    {
        clientApi = api;
        clientChannel = api.Network
            .GetChannel("tabletopgames:rotatepiece");

        api.Input.RegisterHotKeyFirst("tabletopgames:rotatepiece-counterclockwise", Lang.Get("tabletopgames:hotkey-rotatepiece-counterclockwise"), GlKeys.Minus);
        api.Input.SetHotKeyHandler("tabletopgames:rotatepiece-counterclockwise", (_) => HandleHotkey(EnumRotDirection.Counterclockwise));

        api.Input.RegisterHotKeyFirst("tabletopgames:rotatepiece-clockwise", Lang.Get("tabletopgames:hotkey-rotatepiece-clockwise"), GlKeys.Plus);
        api.Input.SetHotKeyHandler("tabletopgames:rotatepiece-clockwise", (_) => HandleHotkey(EnumRotDirection.Clockwise));
    }

    private bool HandleHotkey(EnumRotDirection dir)
    {
        ItemSlot activeSlot = clientApi.World.Player.Entity.ActiveHandItemSlot;
        if (activeSlot.Empty) return false;
        if (!activeSlot.Itemstack.ItemAttributes.IsTrue("rotateWithHotkey")) return false;

        clientChannel.SendPacket(new RotatePieceRequest() { direction = (int)dir });
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
            .GetChannel("tabletopgames:rotatepiece")
            .SetMessageHandler<RotatePieceRequest>(OnClientRequest);
    }

    private void OnClientRequest(IPlayer fromPlayer, RotatePieceRequest networkRequest)
    {
        ItemSlot activeSlot = fromPlayer.Entity.ActiveHandItemSlot;
        if (activeSlot.Empty) return;
        if (!activeSlot.Itemstack.ItemAttributes.IsTrue("rotateWithHotkey")) return;

        int curRotation = activeSlot.Itemstack.Attributes.GetInt("rotateY", 0);

        int currentStep = curRotation / 90 % 4;

        int nextStep = (currentStep + networkRequest.direction + 4) % 4;
        int newRotation = nextStep * 90;

        activeSlot.Itemstack.Attributes.SetInt("rotateY", newRotation);

        activeSlot.MarkDirty();
    }
    #endregion
}