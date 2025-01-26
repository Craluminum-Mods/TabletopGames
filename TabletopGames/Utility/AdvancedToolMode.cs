using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public List<object> NameArray { get; set; } = new();
    public bool Linebreak { get; set; }

    public bool NameExists => !string.IsNullOrEmpty(Name) || NameArray.Any();

    public string GetName()
    {
        if (!NameArray.Any())
        {
            return Lang.Get(Name);
        }

        StringBuilder sb = new StringBuilder();
        foreach (object entry in NameArray)
        {
            if (entry is string)
            {
                sb.Append(Lang.GetMatching(entry.ToString()));
            }
            else if (entry is JArray array && array.Any())
            {
                object[] args = array.Skip(1).Select(arg =>
                {
                    if (arg.Type == JTokenType.String)
                    {
                        return (object)Lang.GetMatching(arg.ToString());
                    }
                    return (object)arg;
                }).ToArray();

                string key = array[0].ToString();
                sb.Append(Lang.GetMatching(key, args));
                continue;
            }
        }
        return sb.ToString();
    }
}