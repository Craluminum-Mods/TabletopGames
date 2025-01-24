using HarmonyLib;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace TabletopGames;

public class Variants
{
    public const string RootAttributeName = "types";

    protected Dictionary<string, string> Elements { get; set; } = new();

    public int Count => Elements.Count;
    public bool Any => Elements.Any();

    public IOrderedEnumerable<Variant> GetOrdered()
    {
        return Elements
            .Select(x => new Variant(x.Key, x.Value))
            .OrderBy(x => x.Key);
    }

    public IOrderedEnumerable<string> GetOrderedStringArray()
    {
        return Elements
            .Select(x => $"{x.Key}-{x.Value}")
            .OrderBy(x => x);
    }

    public string Get(string key)
    {
        return Elements.GetValueSafe(key);
    }
    
    public void Set(string key, string value)
    {
        if (Elements.ContainsKey(key))
        {
            Elements[key] = value;
            return;
        }
        Elements.TryAdd(key, value);
    }
    
    public void RemoveKey(string key)
    {
        Elements.Remove(key);
    }

    public static Variants FromTreeAttribute(ITreeAttribute rootTree)
    {
        Variants variants = new Variants();
        if (!rootTree.HasAttribute(RootAttributeName))
        {
            return variants;
        }

        ITreeAttribute typesTree = rootTree.GetTreeAttribute(RootAttributeName);
        foreach (string key in typesTree.Select(x => x.Key).Where(key => !variants.Elements.ContainsKey(key)))
        {
            variants.Elements.Add(key, typesTree.GetString(key));
        }
        return variants;
    }

    /// <summary> Overwrites tree </summary>
    public void ToTreeAttribute(ITreeAttribute rootTree)
    {
        rootTree.RemoveAttribute(RootAttributeName);
        ITreeAttribute typesTree = rootTree.GetOrAddTreeAttribute(RootAttributeName);
        foreach ((string key, string val) in Elements)
        {
            typesTree.SetString(key, val);
        }
    }

    public static Variants FromStack(ItemStack stack)
    {
        return FromTreeAttribute(stack.Attributes);
    }

    /// <summary> Overwrites tree </summary>
    public void ToStack(ItemStack stack)
    {
        ToTreeAttribute(stack.Attributes);
    }

    public string ReplacePlaceholders(string input)
    {
        foreach ((string key, string value) in Elements)
        {
            input = input.Replace("{" + key + "}", value);
        }
        return input;
    }

    public override string ToString()
    {
        StringBuilder result = new StringBuilder();
        if (Elements.Any())
        {
            result.Append(string.Join('-', Elements.Select(x => $"{x.Key}-{x.Value}")));
        }
        return result.ToString();
    }

    public Variants Clone()
    {
        return new Variants()
        {
            Elements = Elements
        };
    }

    public void AppendTranslatedText(StringBuilder sb, List<object> entries)
    {
        foreach (var entry in entries)
        {
            if (entry is string)
            {
                sb.Append(Lang.GetMatching(ReplacePlaceholders(entry.ToString())));
            }
            else if (entry is JArray array && array.Any())
            {
                object[] args = array.Skip(1).Select(arg =>
                {
                    if (arg.Type == JTokenType.String)
                    {
                        return (object)ReplacePlaceholders(arg.ToString());
                    }
                    return (object)arg;
                }).ToArray();

                string key = ReplacePlaceholders(array[0].ToString());
                sb.Append(Lang.GetMatching(key, args).ToArray());
            }
        }
    }

    public string GetName(List<object> entries, string defaultName)
    {
        if (!Any || entries == null || !entries.Any())
        {
            return defaultName;
        }

        StringBuilder sb = new StringBuilder();
        AppendTranslatedText(sb, entries);
        return sb.ToString();
    }

    public void GetDescription(StringBuilder sb, List<object> entries)
    {
        if (!Any || entries == null || !entries.Any())
        {
            return;
        }

        AppendTranslatedText(sb, entries);
        sb.AppendLine();
        GetDebugDescription(sb);
    }

    public void GetDebugDescription(StringBuilder sb)
    {
        if (!Any)
        {
            return;
        }
        if (TabletopDebug.VariantsDebugInfo)
        {
            sb.AppendLine();
            foreach ((string key, string value) in Elements)
            {
                sb.AppendLine($"DEBUG::{key}-{value}");
            }
        }
    }
}