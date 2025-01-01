using Vintagestory.API.Common;

namespace TabletopGames;

public interface IHandbookTweaks
{
    bool CanRedirect(ItemStack stack, out ItemStack newStack);
}

/// Example usage
//public Dictionary<string, JsonItemStack> RedirectToByType { get; protected set; }
//RedirectToByType = Attributes["redirectTo"].AsObject<Dictionary<string, JsonItemStack>>();
//bool IHandbookTweaks.CanRedirect(ItemStack stack, out ItemStack newStack)
//{
//    Materials materials = Materials.FromStack(stack);
//    if (!materials.FindByMaterial(RedirectToByType, out JsonItemStack jstack) || jstack == null)
//    {
//        newStack = null;
//        return false;
//    }
//    JsonItemStack _jstack = jstack.Clone();
//    if (!_jstack.Resolve(api.World, "handbook tweaks redirect"))
//    {
//        newStack = null;
//        return false;
//    }
//    newStack = _jstack.ResolvedItemstack;
//    return newStack != null;
//}