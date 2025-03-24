using ProtoBuf;

namespace TabletopGames;

[ProtoContract]
public class ShuffleResponse
{
    [ProtoMember(1)]
    public string errorCode = string.Empty;
}