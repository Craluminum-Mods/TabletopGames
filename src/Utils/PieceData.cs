using System.Collections.Generic;
using Newtonsoft.Json;

namespace TabletopGames
{
    [JsonObject]
    public class ChessData
    {
        public List<string> Pieces { get; set; }
        public Dictionary<string, string> Colors { get; set; }
    }

    [JsonObject]
    public class CheckerData
    {
        public Dictionary<string, string> Colors { get; set; }
    }
}