using System.Collections.Generic;
using Vintagestory.API.Common;

namespace TabletopGames;

public class CraftingStep
{
    public CraftingRecipeIngredient TriggerBy { get; set; }
    public Dictionary<string, string> SetVariants { get; set; } = new();
    public List<string> RemoveVariants { get; set; } = new();

    public JsonItemStack ConvertTo { get; set; }
    public bool CopyAttributes { get; set; }
    public bool ConsumeIngredient { get; set; } = true;

    public JsonItemStack GiveStack { get; set; }
}