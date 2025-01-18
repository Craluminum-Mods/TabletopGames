using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace TabletopGames;

public class BEBehaviorBoardPreviewRenderer : BlockEntityBehavior
{
    public BEBehaviorBoardPreviewRenderer(BlockEntity blockentity) : base(blockentity) { }

    public override void Initialize(ICoreAPI api, JsonObject properties)
    {
        base.Initialize(api, properties);

        if (api is ICoreClientAPI capi)
        {
            capi.Event.RegisterRenderer(new BoardPreviewRenderer(Pos, capi), EnumRenderStage.OIT, "tabletopgames.boardpreview");
        }
    }
}
