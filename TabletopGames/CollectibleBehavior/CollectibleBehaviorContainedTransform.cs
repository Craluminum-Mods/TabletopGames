using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace TabletopGames;

/// <summary>
/// Provides custom transformation for OnBeforeRender and collectibles stored inside a BlockEntityDisplay, 
/// specifically for collectibles with Variants.
/// </summary>
public class CollectibleBehaviorContainedTransform : CollectibleBehavior, IContainedTransform
{
    protected Transforms transforms;
    protected Dictionary<string, Dictionary<string, ModelTransform>> extraTransforms = new();

    public CollectibleBehaviorContainedTransform(CollectibleObject collObj) : base(collObj) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);

        transforms = properties["transforms"].AsObject(defaultValue: new Transforms());

        extraTransforms = properties["extraTransforms"]
            .AsObject(defaultValue: new Dictionary<string, Dictionary<string, ModelTransform>>())
            .ToDictionary(x => x.Key.ToLowerInvariant(), x => x.Value);
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        ApplyOnBeforeRenderTransform(target, variants: Variants.FromStack(itemstack), ref renderinfo.Transform);
    }

    ModelTransform? IContainedTransform.GetTransform(BlockEntityDisplay be, string attributeTransformCode, ItemStack stack)
    {
        attributeTransformCode = attributeTransformCode.ToLowerInvariant();

        if (extraTransforms.TryGetValue(attributeTransformCode, out Dictionary<string, ModelTransform>? transformByType)
            && Variants.FromStack(stack).FindByVariant(transformByType, out ModelTransform transform))
        {
            transform = transform.EnsureDefaultValues();
            return transform;
        }
        return null;
    }

    public void ApplyOnBeforeRenderTransform(EnumItemRenderTarget target, Variants variants, ref ModelTransform transform)
    {
        Dictionary<string, ModelTransform> transformByType = target switch
        {
            EnumItemRenderTarget.Gui => transforms.GuiTransform,
            EnumItemRenderTarget.HandTp => transforms.TpHandTransform,
            EnumItemRenderTarget.HandTpOff => transforms.TpOffHandTransform,
            EnumItemRenderTarget.Ground => transforms.GroundTransform,
            _ => new(),
        };

        if (variants.FindByVariant(inDictionary: transformByType, out ModelTransform newTransform))
        {
            newTransform = newTransform.EnsureDefaultValues();
            transform = newTransform;
        }
    }
}