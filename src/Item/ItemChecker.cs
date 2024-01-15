using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using TabletopGames.Utils;

namespace TabletopGames
{
    class ItemChecker : ItemWithAttributes
    {
        public override string GetHeldItemName(ItemStack stack)
        {
            string color = stack.Attributes.GetString("color");
            bool crown = stack.Attributes.GetBool("crown");

            if (crown)
            {
                return Lang.GetMatching("tabletopgames:item-checker-withcrown", Lang.Get($"color-{color}"));
            }
            else
            {
                return Lang.GetMatching("tabletopgames:item-checker", Lang.Get($"color-{color}"));
            }
        }

        public override Shape GetShape(ItemStack stack)
        {
            var trueOrFalse = stack.Attributes.GetBool("crown").ToString().ToLower();
            var shape = api.GetShapeFromAttributesByKey(stack, key: $"crown-{trueOrFalse}");
            return shape ?? base.GetShape(stack);
        }

        public override string GetMeshCacheKey(ItemStack stack)
        {
            string color = stack.Attributes.GetString("color");
            bool crown = stack.Attributes.GetBool("crown");

            return Code.ToShortString() + "-" + color + "-" + crown;
        }
    }
}