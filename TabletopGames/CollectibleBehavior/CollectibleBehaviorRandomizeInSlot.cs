using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace TabletopGames;

public class CollectibleBehaviorRandomizeInSlot : CollectibleBehavior
{
    private ICoreAPI api;
    private string targetAttribute;
    private List<string> possibleAttributeValues = new();

    public CollectibleBehaviorRandomizeInSlot(CollectibleObject collObj) : base(collObj) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        targetAttribute = properties["targetAttribute"].AsString();
        possibleAttributeValues = properties["possibleAttributeValues"].AsObject(new List<string>());
    }

    public override void OnLoaded(ICoreAPI api)
    {
        this.api = api;
    }

    public void RandomizeAttributes(ItemStack stack)
    {
        if (possibleAttributeValues == null || !possibleAttributeValues.Any() || string.IsNullOrEmpty(targetAttribute))
        {
            return;
        }
        Materials materials = Materials.FromStack(stack);
        materials.SetValue(targetAttribute, possibleAttributeValues[api.World.Rand.Next(possibleAttributeValues.Count)]);
        materials.ToStack(stack);
    }
}
