using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace TabletopGames;

/// <summary>
/// Provides interaction help for items stored inside a block with BlockBehaviorExtraBlockInteractionHelp.
/// </summary>
public class CollectibleBehaviorInteractionHelpConstructor : CollectibleBehavior
{
    private WorldInteraction[] interactions = Array.Empty<WorldInteraction>();
    private Dictionary<string, WorldInteraction[]> typedInteractions = new();

    public CollectibleBehaviorInteractionHelpConstructor(CollectibleObject obj) : base(obj) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        interactions = properties["interactions"].AsObject(defaultValue: Array.Empty<WorldInteraction>());
        typedInteractions = properties["typedInteractions"].AsObject(defaultValue: new Dictionary<string, WorldInteraction[]>());
    }

    public override void OnLoaded(ICoreAPI api)
    {
        for (int i = 0; i < interactions.Length; i++)
        {
            WorldInteraction interaction = interactions[i];
            if (interaction.JsonItemStacks != null)
            {
                for (int j = 0; j < interaction.JsonItemStacks.Length; j++)
                {
                    JsonItemStack jstack = interaction.JsonItemStacks[j];
                    if (jstack.Resolve(api.World, ""))
                    {
                        interaction.Itemstacks ??= Array.Empty<ItemStack>();
                        interaction.Itemstacks = interaction.Itemstacks.Append(jstack.ResolvedItemstack);
                    }
                }
            }
        }
    }

    public WorldInteraction[] GetInteractionHelp(ItemStack itemstack)
    {
        return Variants.FromStack(itemstack).FindByVariant(typedInteractions, out WorldInteraction[] _interactions) && _interactions != null && _interactions.Any()
            ? _interactions
            : interactions;
    }
}