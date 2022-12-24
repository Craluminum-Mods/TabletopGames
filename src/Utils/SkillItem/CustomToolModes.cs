using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;

namespace TabletopGames.Utils
{
    public static class CustomToolModes
    {
        public static SkillItem[] GetCustomToolModes(this ICoreAPI api, CollectibleObject collobj)
        {
            var customModes = collobj.GetBehavior<CollectibleBehaviorCustomToolModes>()?.toolModes;
            var modes = new List<SkillItem>();

            for (int i = 0; i < customModes.Count; i++)
            {
                var mode = customModes[i];

                modes.Add(
                    new SkillItem
                    {
                        Name = Lang.GetMatching(mode.Name),
                        Data = mode,
                        Linebreak = mode.Linebreak
                    }
                );

                if (api.Side.IsServer()) continue;
                var capi = (ICoreClientAPI)api;

                if (!string.IsNullOrEmpty(customModes[i].TextIcon))
                {
                    modes[i].WithSmallLetterIcon(capi, customModes[i].TextIcon);
                }
                else
                {
                    modes[i]
                    .WithIcon(capi, capi.Gui.LoadSvgWithPadding(new AssetLocation(mode.IconPath), 48, 48, 5, mode.HexColor != null ? ColorUtil.Hex2Int(mode.HexColor) : null))
                    .TexturePremultipliedAlpha = mode.NoColor;
                }
            }

            return modes.ToArray();
        }

        public static void SetAttributes(this ItemSlot slot, int index, List<CustomToolMode> customToolModes)
        {
            var setAttributes = customToolModes?[index]?.SetAttributes;
            if (setAttributes != null)
            {
                var setStrings = setAttributes?.String;
                var setInts = setAttributes?.Int;
                var setBools = setAttributes?.Bool;

                for (int i = 0; i < setStrings?.Count; i++)
                {
                    if (ConditionAllows(setStrings[i].Condition, slot))
                    {
                        slot.Itemstack.Attributes.SetString(
                            setStrings[i].Key,
                            setStrings[i].Value);
                        break;
                    }
                }

                for (int i = 0; i < setInts?.Count; i++)
                {
                    if (ConditionAllows(setInts[i].Condition, slot))
                    {
                        slot.Itemstack.Attributes.SetInt(
                            setInts[i].Key,
                            setInts[i].Value);
                        break;
                    }
                }

                for (int i = 0; i < setBools?.Count; i++)
                {
                    if (ConditionAllows(setBools[i].Condition, slot))
                    {
                        slot.Itemstack.Attributes.SetBool(
                            setBools[i].Key,
                            setBools[i].Value);
                        break;
                    }
                }
            }
        }

        private static bool ConditionAllows(Condition condition, ItemSlot slot)
        {
            if (condition?.Int != null)
            {
                return condition.Int.Value == slot.Itemstack.Attributes.GetAsInt(condition.Int.Key);
            }

            if (condition?.String != null)
            {
                return condition.String.Value == slot.Itemstack.Attributes.GetAsString(condition.String.Key);
            }

            if (condition?.Bool != null)
            {
                return condition.Bool.Value == slot.Itemstack.Attributes.GetBool(condition.Bool.Key);
            }

            return true;
        }

        public static void PushEvents(this ItemSlot slot, ICoreAPI api, IPlayer byPlayer, int index, List<CustomToolMode> customToolModes)
        {
            var events = customToolModes[index].PushEvents;

            if (events != null)
            {
                for (int i = 0; i < events?.Count; i++)
                {
                    switch (events[i])
                    {
                        case "DropAllSlots":
                            slot.Itemstack.TryDropAllSlots(byPlayer, api);
                            break;

                        case "PackToBox":
                            var boxItem = api.World.GetItem(new AssetLocation(slot.Itemstack.Collectible.Attributes["tabletopgames"]?["packTo"].AsString()));
                            if (boxItem == null) break;

                            var boxStack = boxItem?.GenItemstack(api, null);
                            if (boxStack.ResolveBlockOrItem(api.World))
                            {
                                slot.ConvertBlockToItemBox(boxStack, "containedStack");
                            }
                            break;
                        case "Unpack":
                            if (slot == null || slot.Itemstack == null || slot.Itemstack.Attributes == null) break;

                            var boardStack = slot.Itemstack.Attributes.GetItemstack("containedStack");

                            if (boardStack?.ResolveBlockOrItem(api.World) != true) break;

                            boardStack.Attributes.SetString("wood", slot.Itemstack.Attributes.GetString("wood", "oak"));

                            slot.Itemstack.SetFrom(boardStack);
                            slot.MarkDirty();
                            break;
                    }
                }
            }
        }

        public static void ChangeVariant(this ItemSlot slot, ICoreAPI api, IPlayer byPlayer, int index, List<CustomToolMode> customToolModes)
        {
            var mode = customToolModes[index];
            var changeVariant = mode.ChangeVariant;

            if (changeVariant != null)
            {
                slot.Itemstack.TryChangeVariant(api, mode.ChangeVariant.Key, mode.ChangeVariant.Value);
            }
        }
    }
}
