using System.Text;
using Vintagestory.API.Common;
using TabletopGames.Utils;
using Vintagestory.API.Client;

namespace TabletopGames
{
    public class BEPlayingSurface : BEBoard
    {
        public override string InventoryClassName => "ttgplayingsurface";

        public override NewSlotDelegate OnNewSlot() => (f, f2) => new ItemSlotPlayingCard(f2);

        public override string MeshesKey => "ttg_playingSurfaceBlockMeshes";

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            dsc.AppendLine().AppendSelectedSlotText(Block, forPlayer, inventory, DisplaySelectedSlotId, DisplaySelectedSlotStack);
        }
    }
}