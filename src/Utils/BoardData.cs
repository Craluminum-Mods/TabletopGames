using Newtonsoft.Json;

namespace TabletopGames
{
    [JsonObject]
    public class BoardData
    {
        public int QuantitySlots { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public float DistanceBetweenSlots { get; set; }
        public float FromBorderX { get; set; }
        public float FromBorderZ { get; set; }
        public string AttributeTransformCode { get; set; }
    }
}