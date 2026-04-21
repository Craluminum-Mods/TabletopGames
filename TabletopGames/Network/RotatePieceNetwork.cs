using ProtoBuf;
using System;
using System.Collections.Generic;
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

    private Dictionary<string, Func<bool>> dialogHotkeys;

    public override void StartClientSide(ICoreClientAPI api)
    {
        clientApi = api;
        clientChannel = api.Network
            .GetChannel("tabletopgames:rotatepiece");

        api.Input.RegisterHotKeyFirst("tabletopgames:rotatepiece-clockwise", "Rotate Clockwise", GlKeys.Plus);
        api.Input.SetHotKeyHandler("tabletopgames:rotatepiece-clockwise", (key) => HandleHotkey(EnumRotDirection.Clockwise));

        api.Input.RegisterHotKeyFirst("tabletopgames:rotatepiece-counterclockwise", "Rotate Counter-clockwise", GlKeys.Minus);
        api.Input.SetHotKeyHandler("tabletopgames:rotatepiece-counterclockwise", (key) => HandleHotkey(EnumRotDirection.Counterclockwise));
    }

    private bool HandleHotkey(EnumRotDirection dir)
    {
        ItemSlot activeSlot = clientApi.World.Player.Entity.ActiveHandItemSlot;
        if (activeSlot.Empty) return false;
        if (!activeSlot.Itemstack.ItemAttributes.IsTrue("rotateWithHotkey")) return false;

        int direction = dir switch
        {
            EnumRotDirection.Clockwise => 1,
            EnumRotDirection.Counterclockwise => -1,
            _ => 0,
        };

        clientChannel.SendPacket(new RotatePieceRequest() { direction = direction });
        return true;
    }

    public override void Dispose()
    {
        dialogHotkeys.Clear();
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