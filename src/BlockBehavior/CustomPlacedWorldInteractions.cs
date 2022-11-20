using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace TabletopGames
{
    class BlockBehaviorCustomPlacedWorldInteractions : BlockBehavior
    {
        WorldInteraction[] interactions;

        public BlockBehaviorCustomPlacedWorldInteractions(Block block) : base(block) { }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            interactions = properties["interactions"].AsObject<WorldInteraction[]>();
        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handling)
        {
            return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer, ref handling).Append(interactions);
        }
    }
}