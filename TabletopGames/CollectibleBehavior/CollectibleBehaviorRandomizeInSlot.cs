using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace TabletopGames;

public class CollectibleBehaviorRandomizeInSlot : CollectibleBehavior
{
    private ICoreAPI api;

    private Dictionary<string, string> targetVariantByType;
    private Dictionary<string, List<string>> possibleVariantValuesByType = new();

    public CollectibleBehaviorRandomizeInSlot(CollectibleObject collObj) : base(collObj) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        targetVariantByType = properties["targetVariant"].AsObject<Dictionary<string, string>>();
        possibleVariantValuesByType = properties["possibleVariantValues"].AsObject(new Dictionary<string, List<string>>());
    }

    public override void OnLoaded(ICoreAPI api)
    {
        this.api = api;
    }

    public void RandomizeAttributes(ItemStack stack)
    {
        Variants variants = Variants.FromStack(stack);
        if (!variants.FindByVariant(targetVariantByType, out string targetVariant) || !variants.FindByVariant(possibleVariantValuesByType, out List<string> possibleVariantValues))
        {
            return;
        }

        if (possibleVariantValues == null || !possibleVariantValues.Any() || string.IsNullOrEmpty(targetVariant))
        {
            return;
        }

        variants.Set(targetVariant, possibleVariantValues[api.World.Rand.Next(possibleVariantValues.Count)]);
        variants.ToStack(stack);
    }
}
