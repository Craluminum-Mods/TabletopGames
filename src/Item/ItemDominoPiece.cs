using Vintagestory.API.Common;
using Vintagestory.API.Config;
using TabletopGames.Utils;

namespace TabletopGames
{
    class ItemDominoPiece : ItemWithAttributes
    {
        public string ModelPrefix => Attributes["modelPrefix"].AsString();

        public override string GetHeldItemName(ItemStack stack)
        {
            string type = stack.Attributes.GetString("type");
            int rotation = stack.Attributes.GetInt("rotation");

            return Lang.GetMatching("tabletopgames:item-dominopiece", type, rotation);
        }

        public override Shape GetShape(ItemStack stack)
        {
            var shape = Vintagestory.API.Common.Shape.TryGet(api, ModelPrefix + stack.Attributes.GetString("type") + ".json")
                ?? Vintagestory.API.Common.Shape.TryGet(api, this.GetShapePath());
            return shape ?? base.GetShape(stack);
        }

        public override string GetMeshCacheKey(ItemStack stack)
        {
            string type = stack.Attributes.GetString("type");
            string color1 = stack.Attributes.GetString("color1");
            string color2 = stack.Attributes.GetString("color2");
            int rotation = stack.Attributes.GetInt("rotation");

            return Code.ToShortString() + "-" + type + "-" + color1 + "-" + color2 + "-" + rotation;
        }
    }
}