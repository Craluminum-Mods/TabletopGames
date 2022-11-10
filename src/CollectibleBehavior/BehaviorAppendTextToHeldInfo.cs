using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

namespace TabletopGames
{
    class CollectibleBehaviorAppendTextToHeldInfo : CollectibleBehavior
    {
        string[] keys;

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            keys = properties["keys"].AsArray<string>();
        }

        public CollectibleBehaviorAppendTextToHeldInfo(CollectibleObject collObj) : base(collObj) { }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            for (int i = 0; i < keys.Length; i++)
            {
                dsc.AppendLine(Lang.Get(keys[i]));
            }
        }
    }
}