using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Config;

namespace TabletopGames;

public class AdvancedToolMode
{
    public Dictionary<string, string> SetVariants { get; set; } = new();
    public List<string> RemoveVariants { get; set; } = new();

    public JsonItemStack ConvertTo { get; set; }
    public bool CopyAttributes { get; set; }

    public bool IsSinkSlot { get; set; }
    public List<CraftingStep> SlotParams { get; set; } = new();

    public string IconTexture { get; set; } = "";
    public JsonItemStack IconStack { get; set; }

    public string Name { get; set; }
    public bool Linebreak { get; set; }

    public string NameTranslated => Lang.Get(Name);
    public bool NameExists => !string.IsNullOrEmpty(Name);
}
