using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using System.Linq;

namespace TabletopGames.Utils
{
    public static class ModUtils
    {
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
            var wood = stack.Attributes.GetString("wood");
            if (wood == null) return;

            var textPart = string.Format("{0}: {1}", Lang.Get("Wood"), Lang.Get($"material-{wood}"));
            var woodFormat = Lang.Get("tabletopgames:format-wood", textPart);
            dsc.AppendLine(woodFormat);
        }

        public static void AppendWoodText(this StringBuilder dsc, string wood)
        {
            var textPart = string.Format("{0}: {1}", Lang.Get("Wood"), Lang.Get($"material-{wood}"));
            var woodFormat = Lang.Get("tabletopgames:format-wood", textPart);
            dsc.AppendLine(woodFormat);
        }

        public static void AppendInventorySlotsText(this StringBuilder dsc, ItemStack stack)
        {
            var quantitySlots = stack.Attributes.GetAsInt("quantitySlots");
            if (quantitySlots == 0) return;

            var textPart = Lang.Get("Quantity Slots: {0}", quantitySlots);
            var quantitySlotsFormat = Lang.Get("tabletopgames:format-quantitySlots", textPart);
            dsc.AppendLine(quantitySlotsFormat);
        }

        public static void AppendInventorySlotsText(this StringBuilder dsc, int quantitySlots)
        {
            if (quantitySlots == 0) return;

            var textPart = Lang.Get("Quantity Slots: {0}", quantitySlots);
            var quantitySlotsFormat = Lang.Get("tabletopgames:format-quantitySlots", textPart);
            dsc.AppendLine(quantitySlotsFormat);
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

        public static AssetLocation GetTexturePath(this ItemStack stack, KeyValuePair<string, CompositeTexture> key)
        {
            var textures = stack?.Item?.Textures ?? stack?.Block?.Textures;

            if (stack?.Attributes?.HasAttribute(key.Key) == true)
            {
                var keyOnly = key.Key;
                var valueOnly = stack?.Attributes?.GetString(keyOnly);
                var keyValue = $"{keyOnly}-{valueOnly}";

                if (textures.ContainsKey(keyValue)) return textures?[keyValue]?.Base;
                if (valueOnly != null && textures.ContainsKey(valueOnly)) return textures?[valueOnly]?.Base;
                if (textures.ContainsKey(keyOnly)) return textures[keyOnly].Base;
            }
            return textures[key.Key].Base;
        }

        public static AssetLocation GetShapePath(this CollectibleObject collobj)
        {
            var shapeBase = (collobj as Item)?.Shape.Base ?? (collobj as Block)?.Shape.Base;
            return new(shapeBase.Domain, "shapes/" + shapeBase.Path + ".json");
        }

        public static Shape GetShapeFromAttributes(this ICoreAPI api, ItemStack stack)
        {
            return Shape.TryGet(api, stack.Collectible.Attributes["shape"].AsString());
        }

        public static Shape GetShapeFromAttributesByKey(this ICoreAPI api, ItemStack stack, string key)
        {
            if (key == null) return Shape.TryGet(api, stack.Collectible.GetShapePath());
            return Shape.TryGet(api, stack.Collectible.Attributes["shapes"][key].AsString());
        }

        public static bool TryPickup(this Block block, BlockEntityContainer blockEntity, IWorldAccessor world, IPlayer byPlayer, bool saveInventory = true)
        {
            if (blockEntity.Inventory == null) return false;
            if (!byPlayer.Entity.Controls.ShiftKey) return false;
            if (!byPlayer.Entity.Controls.CtrlKey) return false;

            var blockStack = block.OnPickBlock(world, blockEntity.Pos);

            if (saveInventory) blockStack.TransferInventory(blockEntity.Inventory);

            if (!byPlayer.InventoryManager.TryGiveItemstack(blockStack, true))
            {
                world.SpawnItemEntity(blockStack, blockEntity.Pos.ToVec3d().Add(0.5, 0.5, 0.5));
            }

            world.BlockAccessor.SetBlock(0, blockEntity.Pos);
            return true;
        }

        public static void ApplyStackRotation(this ITreeAttribute stackAttributes, IPlayer byPlayer, Block block)
        {
            var facing = BlockFacing.HorizontalFromAngle(GameMath.Mod(byPlayer.Entity.Pos.Yaw, (float)Math.PI * 2f));
            var side = block?.Variant?["side"];

            stackAttributes.RemoveAttribute("rotation");
            stackAttributes.SetInt("rotation", GetRotationBySideAndFacing(side, facing));
        }

        public static int GetRotationBySideAndFacing(string side, BlockFacing facing)
        {
            if (side == null && facing == BlockFacing.EAST) return 270;
            if (side == null && facing == BlockFacing.NORTH) return 0;
            if (side == null && facing == BlockFacing.WEST) return 90;
            if (side == null && facing == BlockFacing.SOUTH) return 180;

            if (side == facing.ToString().ToLower()) return 180;
            if (side == facing.Opposite.ToString().ToLower()) return 0;

            if (side == "east" && facing == BlockFacing.SOUTH) return 90;
            if (side == "north" && facing == BlockFacing.EAST) return 90;
            if (side == "south" && facing == BlockFacing.WEST) return 90;
            if (side == "west" && facing == BlockFacing.NORTH) return 90;

            if (side == "east" && facing == BlockFacing.NORTH) return 270;
            if (side == "north" && facing == BlockFacing.WEST) return 270;
            if (side == "south" && facing == BlockFacing.EAST) return 270;
            if (side == "west" && facing == BlockFacing.SOUTH) return 270;

            return 0;
        }

        public static void TryChangeVariant(this ItemStack stack, ICoreAPI api, string variantName, string variantValue, bool saveAttributes = true)
        {
            if (stack.Collectible.Variant?[variantName] == null) return;

            var clonedAttributes = stack.Attributes.Clone();

            var newStack = new ItemStack();

            switch (stack.Collectible.ItemClass)
            {
                case EnumItemClass.Block:
                    newStack = new ItemStack(api.World.GetBlock(stack.Collectible.CodeWithVariant(variantName, variantValue)));
                    break;

                case EnumItemClass.Item:
                    newStack = new ItemStack(api.World.GetItem(stack.Collectible.CodeWithVariant(variantName, variantValue)));
                    break;
            }

            if (saveAttributes) newStack.Attributes = clonedAttributes;

            stack.SetFrom(newStack);
        }
    }
}