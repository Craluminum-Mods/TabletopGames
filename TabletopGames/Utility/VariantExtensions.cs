using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

namespace TabletopGames;

public static class VariantExtensions
{
    /// <summary>
    /// Similar to ByType, tries to match key (or multiple keys, if there is '::' separator used as AND operator) and give value behind it
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="variants"></param>
    /// <param name="inDictionary">List of keys, including keys with '::' separator used as AND operator </param>
    /// <param name="result"></param>
    /// <returns>True, if value by key is found, otherwise false</returns>
    public static bool FindByVariant<T>(this Variants variants, Dictionary<string, T> inDictionary, out T result)
    {
        result = default;

        if (variants == null || inDictionary == null || !inDictionary.Any())
        {
            return false;
        }

        IOrderedEnumerable<string> sortedVariants = variants.GetOrderedStringArray();
        foreach ((string key, T value) in inDictionary)
        {
            string[] keys = key.Contains("::") ? key.Split("::") : new[] { key };
            if (keys.All(k => sortedVariants.Any(v => WildcardUtil.Match(k, v))))
            {
                result = value;
                return true;
            }
        }

        return false;
    }

    public static bool IsTrue(this Variants variants, Dictionary<string, bool> inDictionary)
    {
        return variants != null && variants.FindByVariant(inDictionary, out bool result) && result;
    }

    /// <summary>
    /// Overwrites the variants of the input <see cref="ItemStack"/> based on the specified parameters.
    /// If the <paramref name="variants"/> argument is null, the variants from the <paramref name="oldStack"/> are cloned and used.
    /// </summary>
    /// <param name="oldStack">
    /// The original <see cref="ItemStack"/> whose variants are used as the base if <paramref name="variants"/> is null.
    /// </param>
    /// <param name="newStack">
    /// An output parameter that returns the modified <see cref="ItemStack"/> with updated variants.
    /// </param>
    /// <param name="setVariants">
    /// A dictionary of attribute key-value pairs to add or update in the variants.
    /// If null, no attributes are added.
    /// </param>
    /// <param name="removeVariants">
    /// A list of attribute keys to remove from the variants.
    /// If null, no attributes are removed.
    /// </param>
    /// <param name="variants">
    /// (Optional) A <see cref="Variants"/> object to use for the new stack. 
    /// If null, the variants from <paramref name="oldStack"/> are cloned and used.
    /// </param>
    /// <remarks>
    /// The method ensures that the original variants and stack remain unmodified by cloning them before applying changes.
    /// </remarks>
    public static void OverwriteVariants(this ItemStack oldStack, out ItemStack newStack, Dictionary<string, string> setVariants = null, List<string> removeVariants = null, Variants variants = null)
    {
        Variants newVariants = variants?.Clone() ?? Variants.FromStack(oldStack.Clone())?.Clone();

        setVariants ??= new();
        removeVariants ??= new();

        foreach ((string key, string value) in setVariants)
        {
            newVariants.Set(key, value);
        }

        foreach (string key in removeVariants)
        {
            newVariants.RemoveKey(key);
        }

        newStack = oldStack.Clone();
        newVariants.ToStack(newStack);
    }
}