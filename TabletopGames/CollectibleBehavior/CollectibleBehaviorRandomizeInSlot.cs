using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace TabletopGames;

public class CollectibleBehaviorRandomizeInSlot : CollectibleBehavior
{
    private ICoreAPI api;
    private string targetVariant;
    private List<string> possibleVariantValues = new();

    public CollectibleBehaviorRandomizeInSlot(CollectibleObject collObj) : base(collObj) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        targetVariant = properties["targetVariant"].AsString();
        possibleVariantValues = properties["possibleVariantValues"].AsObject(new List<string>());
    }

    public override void OnLoaded(ICoreAPI api)
    {
        this.api = api;
    }

    public void RandomizeAttributes(ItemStack stack)
    {
        if (possibleVariantValues == null || !possibleVariantValues.Any() || string.IsNullOrEmpty(targetVariant))
        {
            return;
        }
        Variants variants = Variants.FromStack(stack);
        variants.Set(targetVariant, possibleVariantValues[api.World.Rand.Next(possibleVariantValues.Count)]);
        variants.ToStack(stack);
    }
}
