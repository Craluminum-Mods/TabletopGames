using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace TabletopGames;

public class Transforms
{
    public Dictionary<string, ModelTransform> GuiTransform { get; set; } = new();
    public Dictionary<string, ModelTransform> TpHandTransform { get; set; } = new();
    public Dictionary<string, ModelTransform> TpOffHandTransform { get; set; } = new();
    public Dictionary<string, ModelTransform> GroundTransform { get; set; } = new();

    public ModelTransform GetTransform(EnumItemRenderTarget target, Variants variants)
    {
        Dictionary<string, ModelTransform> transformByType = target switch
        {
            EnumItemRenderTarget.Gui => GuiTransform,
            EnumItemRenderTarget.HandTp => TpHandTransform,
            EnumItemRenderTarget.HandTpOff => TpOffHandTransform,
            EnumItemRenderTarget.Ground => GroundTransform,
            _ => new(),
        };
        if (transformByType == null || !transformByType.Any())
        {
            return null;
        }
        if (variants.FindByVariant(inDictionary: transformByType, out ModelTransform transform))
        {
            return transform;
        }
        return null;
    }

    public void TryApplyTransform(EnumItemRenderTarget target, Variants variants, ref ModelTransform transform)
    {
        if (GetTransform(target, variants) is ModelTransform newTransform && newTransform != null)
        {
            transform = newTransform;
        }
    }
}
