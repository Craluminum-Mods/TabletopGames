using HarmonyLib;
using System.Runtime.CompilerServices;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace TabletopGames;

[HarmonyPatch(typeof(GuiHandbookItemStackPage), nameof(GuiHandbookItemStackPage.PageCodeForStack))]
public static class PageCodeForStackPatch
{
    [HarmonyReversePatch(HarmonyReversePatchType.Original)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static string Base(ItemStack stack) => default;

    [HarmonyPostfix]
    public static void Postfix(ref string __result, ItemStack stack)
    {
        if (stack?.Collectible?.GetCollectibleInterface<IHandbookTweaks>() is IHandbookTweaks handbookTweaks && handbookTweaks.CanRedirect(stack.Clone(), out ItemStack newStack))
        {
            __result = Base(newStack.Clone());
        }
    }
}