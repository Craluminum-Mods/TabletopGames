
using Cairo;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TabletopGames;

[HarmonyPatchCategory("Client")]
[HarmonyPatch(typeof(CollectibleBehaviorHandbookTextAndExtraInfo), nameof(CollectibleBehaviorHandbookTextAndExtraInfo.GetHandbookInfo))]
public static class GetHandbookInfoPatch
{
    [HarmonyPostfix]
    public static void Postfix(ref RichTextComponentBase[] __result, ItemSlot inSlot, ICoreClientAPI capi, ActionConsumable<string> openDetailPageFor)
    {
        List<RichTextComponentBase> list = __result?.ToList() ?? [];

        if (inSlot.Itemstack!.Collectible.HasBehavior<CollectibleBehaviorShuffler>())
        {
            list.AddRange(GetShuffleContaintableInfo(capi, openDetailPageFor));
        }

        __result = [.. list];
    }

    public static List<RichTextComponentBase> GetShuffleContaintableInfo(ICoreClientAPI api, ActionConsumable<string> openDetailPageFor)
    {
        return ObjectCacheUtil.GetOrCreate(api, "tabletopgames:shufflerContainableHandbook", delegate
        {
            List<RichTextComponentBase> richText = [];
            List<string> names = [];
            List<List<ItemStack>> listsOfStacks = [];

            foreach (CollectibleObject obj in api.World.Collectibles)
            {
                if (obj == null || obj.Code == null || obj.Id == 0) continue;
                if (obj.GetCollectibleInterface<IShufflerContainable>() == null) continue;

                List<ItemStack> stacks = obj.GetHandBookStacks(api);
                if (stacks != null && stacks.Any())
                {
                    listsOfStacks.Add(stacks);
                }
                else
                {
                    names.Add(new ItemStack(obj).GetName());
                }
            }

            if (names.Any() || listsOfStacks.Any())
            {
                richText.Add(new ClearFloatTextComponent(api, 7));
                richText.Add(new RichTextComponent(api, Lang.Get("tabletopgames:handbooktitle-shuffler-containable") + "\n", CairoFont.WhiteSmallText().WithWeight(FontWeight.Bold)));

                foreach (string name in names)
                {
                    richText.Add(new RichTextComponent(api, $"\u2022 {name}\n", CairoFont.WhiteSmallText()));
                }

                foreach (List<ItemStack> stacks in listsOfStacks)
                {

                    richText.Add(new SlideshowItemstackTextComponent(api, [.. stacks], 40, EnumFloat.Inline, (cs) => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)))
                    {
                        ShowStackSize = false
                    });
                }
            }
            return richText;
        });
    }
}