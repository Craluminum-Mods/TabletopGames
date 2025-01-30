using System.Collections.Generic;
using Vintagestory.API.Common;

namespace TabletopGames;

public class Transforms
{
    public Dictionary<string, ModelTransform> GuiTransform { get; set; } = new();
    public Dictionary<string, ModelTransform> TpHandTransform { get; set; } = new();
    public Dictionary<string, ModelTransform> TpOffHandTransform { get; set; } = new();
    public Dictionary<string, ModelTransform> GroundTransform { get; set; } = new();
}
