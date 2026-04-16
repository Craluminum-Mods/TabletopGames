using ProtoBuf;

namespace TabletopGames.Network;

[ProtoContract]
public class ShuffleResponse
{
    [ProtoMember(1)]
    public string errorCode = string.Empty;
}