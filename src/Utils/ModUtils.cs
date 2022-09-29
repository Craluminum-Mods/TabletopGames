using System;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;

namespace TabletopGames.ModUtils
{
    public static class ModUtils
    {
        public static RenderSkillItemDelegate RenderItemWithAttributes(this CollectibleObject collobj, ICoreAPI api, string json)
        {
            return (AssetLocation code, float dt, double posX, double posY) =>
            {
                var size = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
                var scsize = GuiElement.scaled(size - 5);

                (api as ICoreClientAPI)?.Render.RenderItemstackToGui(
                    new DummySlot(GenItemstack(collobj, api, json)),
                    posX + (scsize / 2),
                    posY + (scsize / 2),
                    100,
                    (float)GuiElement.scaled(GuiElementPassiveItemSlot.unscaledItemSize),
                    ColorUtil.WhiteArgb);
            };
        }

        public static ItemStack GenItemstack(this CollectibleObject collobj, ICoreAPI api, string json)
        {
            var jstack = new JsonItemStack();

            switch (json)
            {
                case null or "":
                    jstack = new JsonItemStack()
                    {
                        Code = collobj.Code,
                        Type = EnumItemClass.Item
                    };
                    break;

                case not null:
                    jstack = new JsonItemStack()
                    {
                        Code = collobj.Code,
                        Type = EnumItemClass.Item,
                        Attributes = new JsonObject(JToken.Parse(json))
                    };
                    break;
            }

            jstack.Resolve(api.World, "some type");

            return jstack.ResolvedItemstack;
        }

        public static Vec3f GetPositionOnBoard(this int index, int width, int height, float distanceBetweenSlots, float fromBorderX, float fromBorderZ)
        {
            var dX = Math.Floor((double)(index / width));
            var dZ = index % height;
            return new Vec3f((float)(fromBorderX + (distanceBetweenSlots * dX)), 0f, fromBorderZ - (distanceBetweenSlots * dZ));
        }
    }
}