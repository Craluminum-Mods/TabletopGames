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
        GuiDialogHandbook dialog = null;
        try
        {
            if (Core.apiForHarmony == null)
            {
                return;
            }
            dialog = Core.apiForHarmony.ModLoader.GetModSystem<ModSystemSurvivalHandbook>().GetField<GuiDialogHandbook>("dialog");
        }
        catch
        {
            return;
        }

        if (stack?.Collectible?.GetCollectibleInterface<IHandbookTweaks>() is IHandbookTweaks handbookTweaks && handbookTweaks.CanRedirect(stack, out ItemStack newStack))
        {
            __result = Base(newStack);
        }
    }
}