using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;

namespace TabletopGames;

/// <summary>
/// A pack of items. Used for giving huge amount of different items
/// </summary>
public class ItemPack
{
    public bool Empty => !Items.Any();

    [JsonProperty]
    public List<JsonItemStack> Items { get; set; } = new();

    public ItemPack Resolve(IWorldAccessor world, Variants variants)
    {
        for (int i = 0; i < Items.Count; i++)
        {
            JsonItemStack jstack = Items[i];

            jstack = variants.ReplacePlaceholders(jstack);

            if (jstack == null || !jstack.Resolve(world, "TabletopGames ItemPack"))
            {
                Core.GetInstance(world.Api).Mod.Logger.Fatal("Invalid collectible at index '{0}' inside a pack with variants: {1}", i, variants.ToString());
                return new ItemPack();
            }
        }
        return this;
    }

    public ItemPack Clone()
    {
        return new ItemPack()
        {
            Items = Items.Select(jstack => jstack?.Clone() ?? new JsonItemStack()).ToList()
        };
    }
}