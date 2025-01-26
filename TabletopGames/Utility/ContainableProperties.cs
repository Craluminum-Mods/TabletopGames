using HarmonyLib;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace TabletopGames;

public class ContainableProperties
{
    public Dictionary<string, bool> Enabled { get; set; } = new()
    {
        ["*"] = true
    };

    public Dictionary<string, CompositeShape> Shape { get; set; } = new();
    public Dictionary<string, Dictionary<string, CompositeTexture>> Textures { get; set; } = new();

    public bool IsEnabled(Variants variants = null)
    {
        if (variants == null)
        {
            return Enabled.GetValueSafe("*");
        }

        bool enabled;
        variants.FindByVariant(Enabled, out enabled);
        return enabled;
    }

    public CompositeShape GetShape(Variants variants = null)
    {
        if (!IsEnabled(variants))
        {
            return new();
        }

        if (variants == null)
        {
            return Shape.GetValueSafe("*");
        }

        CompositeShape shape;
        variants.FindByVariant(Shape, out shape);
        return shape;
    }

    public Dictionary<string, CompositeTexture> GetTextures(Variants variants = null)
    {
        if (!IsEnabled(variants))
        {
            return new();
        }

        if (variants == null)
        {
            return Textures.GetValueSafe("*");
        }

        Dictionary<string, CompositeTexture> textures;
        variants.FindByVariant(Textures, out textures);
        return textures;
    }
}