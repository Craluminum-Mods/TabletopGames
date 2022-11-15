using System.Collections.Generic;
using TabletopGames.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace TabletopGames
{
    class CollectibleBehaviorCustomToolModes : CollectibleBehavior
    {
        public List<CustomToolMode> toolModes;
        public SkillItem[] skillItems;

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            toolModes = properties["toolModes"].AsObject<List<CustomToolMode>>();
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            skillItems = api.GetCustomToolModes(collObj);
        }

        public CollectibleBehaviorCustomToolModes(CollectibleObject collObj) : base(collObj) { }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            slot.SetAttributes(toolMode, toolModes);
            slot.ChangeVariant(byPlayer.Entity.World.Api, byPlayer, toolMode, toolModes);
            slot.PushEvents(byPlayer.Entity.World.Api, byPlayer, toolMode, toolModes);
            slot.MarkDirty();
        }

        public override void OnUnloaded(ICoreAPI api)
        {
            for (int i = 0; skillItems != null && i < skillItems.Length; i++)
            {
                skillItems[i]?.Dispose();
            }
        }

        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel) => skillItems;

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
        {
            if (skillItems == null) return base.GetHeldInteractionHelp(inSlot, ref handling);

            return base.GetHeldInteractionHelp(inSlot, ref handling).Append(new WorldInteraction
            {
                ActionLangCode = "heldhelp-settoolmode",
                HotKeyCode = "toolmodeselect",
                MouseButton = EnumMouseButton.None
            });
        }
    }
}