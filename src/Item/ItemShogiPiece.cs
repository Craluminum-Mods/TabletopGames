using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using TabletopGames.Utils;
using Vintagestory.API.MathTools;
using System.Text;

namespace TabletopGames
{
    class ItemShogiPiece : ItemChessPiece
    {
        public override string GetHeldItemName(ItemStack stack)
        {
            var type = stack.Attributes.GetString("type");
            var typeKey = Lang.Get($"tabletopgames:shogipiece-{type}");

            return Lang.GetMatching("tabletopgames:item-shogipiece", typeKey);
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            if (inSlot.Itemstack.Attributes.HasAttribute("material")
                && inSlot.Itemstack.Attributes.GetString("material").Contains("wood-"))
            {
                dsc.AppendWoodText(wood: inSlot.Itemstack.Attributes.GetString("material").Replace("wood-", ""));
            }
            else if (inSlot.Itemstack.Attributes.GetString("material") == "bone")
            {
                var textPart = Lang.Get("Material: {0}", Lang.Get("item-bone"));
                var textFormat = Lang.Get("tabletopgames:format-pastelbrown", textPart);
                dsc.AppendLine(textFormat);
            }
        }

        public override string GetMeshCacheKey(ItemStack stack)
        {
            string _base = base.GetMeshCacheKey(stack);
            var material = stack.Attributes.GetString("material");
            var type = stack.Attributes.GetString("type");
            var rotation = stack.Attributes.GetInt("rotation");

            return $"{_base}-{material}-{type}-{rotation}";
        }
    }
}