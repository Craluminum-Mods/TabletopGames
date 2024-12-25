using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace TabletopGames;

public class Materials
{
    public const string AttributeName = "types";

    protected Dictionary<string, string> Elements { get; set; } = new();

    public int Count => Elements.Count;
    public bool Any => Elements.Any();

    public IOrderedEnumerable<Material> GetOrdered(string textureCode = null)
    {
        return Elements
            .Select(x => new Material(x.Key, x.Value))
            .OrderBy(x => x.Key);
    }

    public void GetDescription(StringBuilder dsc, List<string> langKeys, bool withDebugInfo = false)
    {
        if (!Elements.Any())
        {
            return;
        }

        if (langKeys != null)
        {
            foreach (string langKey in langKeys)
            {
                string newLangKey = ReplacePlaceholders(langKey);
                dsc.Append(Lang.GetMatching(newLangKey));
            }
            dsc.AppendLine();
        }

        if (!withDebugInfo)
        {
            return;
        }

        dsc.AppendLine();
        foreach (KeyValuePair<string, string> material in Elements)
        {
            if (withDebugInfo)
            {
                dsc.AppendLine($"{material.Key}-{material.Value}");
            }
        }
    }

    public static Materials FromTreeAttribute(ITreeAttribute rootTree)
    {
        Materials materials = new Materials();
        if (!rootTree.HasAttribute(AttributeName) || !rootTree.GetTreeAttribute(AttributeName).Any())
        {
            return materials;
        }

        ITreeAttribute typesTree = rootTree.GetTreeAttribute(AttributeName);

        foreach (string key in typesTree.Select(x => x.Key).Where(key => !materials.Elements.ContainsKey(key)))
        {
            materials.Elements.Add(key, typesTree.GetString(key));
        }

        return materials;
    }

    public void ToTreeAttribute(ITreeAttribute rootTree)
    {
        foreach ((string key, string val) in Elements)
        {
            rootTree.GetOrAddTreeAttribute(AttributeName).SetString(key, val);
        }
    }

    public static Materials FromStack(ItemStack stack)
    {
        return FromTreeAttribute(stack.Attributes);
    }

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
}

public class Material
{
    public string Key { get; protected set; }
    public string Value { get; protected set; }

    public Material(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public static Material FromString(string keyVal)
    {
        string[] list = keyVal.Split('-');
        if (list.Length != 2)
        {
            // TODO throw errow
            return null;
        }
        return new Material(list[0], list[1]);
    }

    public override string ToString()
    {
        return $"{Key}-{Value}";
    }
}