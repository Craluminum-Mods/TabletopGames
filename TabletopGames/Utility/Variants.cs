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

    public void GetDescription(StringBuilder dsc, List<string> langKeys)
    {
        langKeys ??= new List<string>();
        if (!Elements.Any())
        {
            return;
        }

        if (langKeys.Any())
        {
            foreach (string langKey in langKeys)
            {
                string newLangKey = ReplacePlaceholders(langKey);
                dsc.Append(Lang.GetMatching(newLangKey));
            }
            dsc.AppendLine();
        }

        if (TabletopDebug.VariantsDebugInfo)
        {
            dsc.AppendLine();
            foreach (KeyValuePair<string, string> variant in Elements)
            {
                dsc.AppendLine($"DEBUG::{variant.Key}-{variant.Value}");
            }
        }
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

    /// <summary>
    /// Overwrites tree
    /// </summary>
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

    /// <summary>
    /// Overwrites tree
    /// </summary>
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

    public class Variant
    {
        public string Key { get; protected set; }
        public string Value { get; protected set; }

        public Variant(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public static Variant FromString(string keyVal)
        {
            string[] list = keyVal?.Split('-');
            if (list.Length != 2)
            {
                return null;
            }
            return new Variant(list[0], list[1]);
        }

        public override string ToString()
        {
            return $"{Key}-{Value}";
        }
    }
}