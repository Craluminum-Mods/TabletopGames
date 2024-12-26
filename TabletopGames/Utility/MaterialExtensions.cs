using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.ServerMods;

namespace TabletopGames;

public static class MaterialExtensions
{
    public static Dictionary<string, List<string>> GatherMaterials(this ICoreAPI api, RegistryObjectVariantGroup[] variantGroups)
    {
        Dictionary<string, List<string>> resolvedTypes = new();
        foreach (RegistryObjectVariantGroup variantGroup in variantGroups)
        {
            List<string> types = new();

            if (variantGroup?.States != null && variantGroup.States.Any())
            {
                types = types.Concat(variantGroup.States).ToList();
            }

            if (variantGroup?.LoadFromProperties != null)
            {
                IAsset asset = api.Assets.TryGet(variantGroup.LoadFromProperties.WithPathPrefixOnce("worldproperties/").WithPathAppendixOnce(".json"));
                if (asset != null)
                {
                    IEnumerable<string> _types = (asset?.ToObject<StandardWorldProperty>()).Variants.Select((p) => p.Code.Path);
                    types = types.Concat(_types).ToList();
                }
            }

            if (!resolvedTypes.TryGetValue(variantGroup.Code, out List<string> _resolvedTypes))
            {
                resolvedTypes.Add(variantGroup.Code, types);
            }
            else
            {
                resolvedTypes[variantGroup.Code] = _resolvedTypes.Concat(types).ToList();
            }
        }
        return resolvedTypes;
    }

    public static void FillCreativeInventory(this CollectibleObject obj, ICoreAPI api, Dictionary<string, List<string>> materials, params string[] tabs)
    {
        List<JsonItemStack> _stacks = new List<JsonItemStack>();

        foreach (Dictionary<string, string> pairs in materials.GetCombinationsContainingAllKeys())
        {
            JsonObject _attributes = new JsonObject(new JObject());
            _attributes.Token[Materials.AttributeName] = JToken.FromObject(new object());

            foreach (KeyValuePair<string, string> pair in pairs)
            {
                _attributes.Token[Materials.AttributeName][pair.Key] = JToken.FromObject(pair.Value);
            }

            JsonItemStack _jstack = new JsonItemStack()
            {
                Code = obj.Code,
                Type = obj.ItemClass,
                Attributes = _attributes
            };
            _jstack.Resolve(api.World, obj.Code + " type");
            _stacks.Add(_jstack);
        }

        obj.CreativeInventoryStacks = new CreativeTabAndStackList[]
        {
            new CreativeTabAndStackList() { Stacks = _stacks.ToArray(), Tabs = tabs }
        };
    }

    public static List<Dictionary<string, string>> GetCombinationsContainingAllKeys(this Dictionary<string, List<string>> materials)
    {
        List<List<string>> combinations = materials.GenerateCombinations();
        List<Dictionary<string, string>> finalResult = new List<Dictionary<string, string>>();

        foreach (List<string> result in combinations)
        {
            finalResult.Add(new());

            int materialIndex = 0;

            foreach ((string material, _) in materials)
            {
                finalResult.Last().Add(material, result[materialIndex]);
                materialIndex++;
            }
        }

        return finalResult;
    }

    public static List<List<string>> GenerateCombinations(this Dictionary<string, List<string>> materials)
    {
        List<List<string>> results = new List<List<string>>();
        CombineMaterials(materials, new List<string>(), new List<string>(materials.Keys), 0, results);
        return results;
    }

    public static void CombineMaterials(Dictionary<string, List<string>> materials, List<string> current, List<string> keys, int index, List<List<string>> results)
    {
        if (index == keys.Count)
        {
            results.Add(new List<string>(current));
            return;
        }

        string key = keys[index];
        foreach (string item in materials[key])
        {
            current.Add(item);
            CombineMaterials(materials, current, keys, index + 1, results);
            current.RemoveAt(current.Count - 1);
        }
    }
}