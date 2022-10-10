using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using TabletopGames.BoxUtils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using System.Linq;

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

        public static void AppendWoodText(this StringBuilder dsc, ItemStack stack)
        {
            var woodType = stack.Attributes.GetString("wood");

            if (woodType != null) dsc.Append(Lang.Get("Wood")).Append(": ").AppendLine(Lang.Get($"material-{woodType}"));
        }

        public static void AppendWoodText(this StringBuilder dsc, string wood)
        {
            dsc.Append(Lang.Get("Wood")).Append(": ").AppendLine(Lang.Get($"material-{wood}"));
        }

        public static void AppendInventorySlotsText(this StringBuilder dsc, ItemStack stack)
        {
            var slots = stack.Attributes.GetAsInt("quantitySlots");

            if (slots != 0) dsc.AppendFormat(Lang.Get("Quantity Slots: {0}", slots)).AppendLine();
        }

        public static void AppendInventorySlotsText(this StringBuilder dsc, int quantitySlots)
        {
            if (quantitySlots != 0) dsc.AppendFormat(Lang.Get("Quantity Slots: {0}", quantitySlots)).AppendLine();
        }

        public static void AppendSelectedSlotText(this StringBuilder dsc, CollectibleObject collobj, IPlayer forPlayer, InventoryBase inventory, bool withSlotId = true, bool withStackName = true)
        {
            if (inventory == null || inventory.Count == 0) return;

            var selBoxIndex = forPlayer.CurrentBlockSelection.SelectionBoxIndex;

            if (collobj.GetIgnoredSelectionBoxIndexes()?.Contains(selBoxIndex) == true) return;

            if (withSlotId) dsc.AppendFormat($"[{inventory.GetSlotId(inventory?[selBoxIndex])}] ");
            if (withStackName) dsc.Append(inventory?[selBoxIndex].GetStackName() ?? Lang.Get("Empty"));
        }

        public static int[] GetIgnoredSelectionBoxIndexes(this CollectibleObject collobj) => collobj.Attributes?["tabletopgames"]["ignoreSelectionBoxIndexes"].AsArray<int>();

        public static void TransferInventory(this ItemStack stack, InventoryBase inventory, ICoreAPI api)
        {
            var slotsTree = stack.Attributes?.GetTreeAttribute("box")?.GetTreeAttribute("slots");

            if (slotsTree == null) return;

            foreach (var slot in inventory)
            {
                var slotId = inventory.GetSlotId(slot);
                var itemstack = slotsTree.GetItemstack("slot-" + slotId);

                if (itemstack?.ResolveBlockOrItem(api.World) == false) continue;
                inventory[slotId].Itemstack = itemstack;
                inventory.MarkSlotDirty(slotId);
            }
        }

        public static void TransferInventory(this ItemStack blockStack, InventoryBase inventory)
        {
            if (inventory != null)
            {
                foreach (var slot in inventory)
                {
                    if (slot.Itemstack == null) continue;
                    var slotId = inventory.GetSlotId(slot);
                    slot.SaveSlotToBox(blockStack, slotId);
                }
            }
        }

        public static int GetNonEmptySlotsCount(this InventoryBase inventory)
        {
            var nonEmptySlotsCount = 0;

            if (inventory != null)
            {
                nonEmptySlotsCount += (from slot in inventory where !slot.Empty select slot).Count();
            }
            return nonEmptySlotsCount;
        }

        public static string TryGetColorName(this KeyValuePair<string, CompositeTexture> key, ItemStack stack)
        {
            if (key.Key == "color" && stack.Attributes.HasAttribute("color")) return stack.Attributes.GetString("color");
            if (key.Key == "color1" && stack.Attributes.HasAttribute("color1")) return stack.Attributes.GetString("color1");
            if (key.Key == "color2" && stack.Attributes.HasAttribute("color2")) return stack.Attributes.GetString("color2");
            else return key.Key;
        }

        public static string TryGetWoodTexturePath(this CollectibleObject collobj, KeyValuePair<string, CompositeTexture> key, string woodTexPrefix, ItemStack stack)
        {
            var textures = (collobj as Item)?.Textures ?? (collobj as Block)?.Textures;
            if (key.Key == "wood") return woodTexPrefix + stack.Attributes.GetString("wood", defaultValue: "oak") + ".png";
            return textures[key.Key].Base.Path;
        }

        public static AssetLocation GetShapePath(this CollectibleObject collobj)
        {
            var shapeBase = (collobj as Item)?.Shape.Base ?? (collobj as Block)?.Shape.Base;
            return new(shapeBase.Domain, "shapes/" + shapeBase.Path + ".json");
        }

        public static bool TryPickup(this Block block, BlockEntityContainer blockEntity, IWorldAccessor world, IPlayer byPlayer)
        {
            if (blockEntity.Inventory == null) return false;
            if (!byPlayer.Entity.Controls.ShiftKey) return false;
            if (!byPlayer.Entity.Controls.CtrlKey) return false;

            var blockStack = block.OnPickBlock(world, blockEntity.Pos);

            if (!byPlayer.InventoryManager.TryGiveItemstack(blockStack, true))
            {
                world.SpawnItemEntity(blockStack, blockEntity.Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }

            world.BlockAccessor.SetBlock(0, blockEntity.Pos);
            return true;
        }
    }
}