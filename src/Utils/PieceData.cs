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

    [JsonObject]
    public class DominoData
    {
        public Dictionary<string, string> Colors1 { get; set; }
        public Dictionary<string, string> Colors2 { get; set; }
    }

    // [JsonObject]
    // public class PlayingCardData
    // {
    //     public string[] Backs { get; set; }
    //     public string[] Faces { get; set; }
    //     public string[] Ranks { get; set; }
    //     public string[] Suits { get; set; }
    // }
}