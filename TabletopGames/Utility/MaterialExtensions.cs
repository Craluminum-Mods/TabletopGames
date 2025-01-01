using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace TabletopGames;

public static class MaterialExtensions
{
    public static bool FindByMaterial<T>(this Materials materials, Dictionary<string, T> inDictionary, out T result)
    {
        result = default;

        if (inDictionary == null || !inDictionary.Any())
        {
            return false;
        }

        IOrderedEnumerable<string> _materials = materials.GetOrderedStringArray();
        foreach ((string key, T value) in inDictionary)
        {
            string[] keys = key.Contains("::") ? key.Split("::") : new[] { key };
            if (keys.All(k => _materials.Any(m => WildcardUtil.Match(k, m))))
            {
                result = value;
                return true;
            }
        }

        return false;
    }

    public static bool FindByMaterial<T>(this Materials materials, string attribute, CollectibleObject collObj, out T result)
    {
        Dictionary<string, T> dict = collObj?.Attributes?[attribute]?.AsObject(new Dictionary<string, T>());
        return materials.FindByMaterial(dict, out result);
    }

    public static bool FindByMaterial<T>(this Materials materials, string attribute, ItemStack stack, out T result)
    {
        return FindByMaterial(materials, attribute, stack.Collectible, out result);
    }

    /// <summary>
    /// Updates the materials of the input <see cref="ItemStack"/> based on the specified parameters.
    /// If the <paramref name="materials"/> argument is null, the materials from the <paramref name="oldStack"/> are cloned and used.
    /// </summary>
    /// <param name="oldStack">
    /// The original <see cref="ItemStack"/> whose materials are used as the base if <paramref name="materials"/> is null.
    /// </param>
    /// <param name="newStack">
    /// An output parameter that returns the modified <see cref="ItemStack"/> with updated materials.
    /// </param>
    /// <param name="setAttributes">
    /// A dictionary of attribute key-value pairs to add or update in the materials.
    /// If null, no attributes are added.
    /// </param>
    /// <param name="removeAttributes">
    /// A list of attribute keys to remove from the materials.
    /// If null, no attributes are removed.
    /// </param>
    /// <param name="materials">
    /// (Optional) A <see cref="Materials"/> object to use for the new stack. 
    /// If null, the materials from <paramref name="oldStack"/> are cloned and used.
    /// </param>
    /// <remarks>
    /// The method ensures that the original materials and stack remain unmodified by cloning them before applying changes.
    /// </remarks>
    public static void SetStackMaterials(this ItemStack oldStack, out ItemStack newStack, Dictionary<string, string> setAttributes = null, List<string> removeAttributes = null, Materials materials = null)
    {
        Materials newMaterials = materials?.Clone() ?? Materials.FromStack(oldStack.Clone())?.Clone();

        setAttributes ??= new();
        removeAttributes ??= new();

        foreach ((string key, string value) in setAttributes)
        {
            newMaterials.SetValue(key, value);
        }

        foreach (string key in removeAttributes)
        {
            newMaterials.RemoveKey(key);
        }

        newStack = oldStack.Clone();
        newMaterials.ToStack(newStack);
    }
}