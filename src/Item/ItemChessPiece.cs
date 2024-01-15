using Vintagestory.API.Common;
using Vintagestory.API.Config;
using TabletopGames.Utils;

namespace TabletopGames
{
    class ItemChessPiece : ItemWithAttributes
    {
        public override string GetHeldItemName(ItemStack stack)
        {
            var type = stack.Attributes.GetString("type");
            var color = stack.Attributes.GetString("color");

            var typeKey = Lang.Get($"tabletopgames:chesspiece-{type}");
            var colorKey = Lang.Get($"color-{color}");

            return Lang.GetMatching("tabletopgames:item-chesspiece", typeKey, colorKey);
        }

        public override Shape GetShape(ItemStack stack)
        {
            var chessType = stack.Attributes.GetString("type");
            var shape = api.GetShapeFromAttributesByKey(stack, key: chessType);
            return shape ?? base.GetShape(stack);
        }

        public override string GetMeshCacheKey(ItemStack stack)
        {
            string color = stack.Attributes.GetString("color");
            string type = stack.Attributes.GetString("type");

            return Code.ToShortString() + "-" + color + "-" + type;
        }
    }
}