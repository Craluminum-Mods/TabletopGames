using System.Text;
using Vintagestory.API.Common;
using TabletopGames.ModUtils;

namespace TabletopGames
{
    public class BEChessBoard : BEBoard
    {
        public override string InventoryClassName => "ttgchessboard";
        public override bool HasWoodType => true;
        public override bool HasCheckerboardTypes => true;

        public override NewSlotDelegate OnNewSlot() => (f, f2) => new ItemSlotChessBoard(f2);

        public override string MeshesKey => "ttg_chessBoardBlockMeshes";

        public override string MeshCacheKey
        {
            get
            {
                string size = Block?.VariantStrict?["size"];
                string side = Block?.VariantStrict?["side"];
                return size + "-" + side + "-" + woodType + "-" + darkType + "-" + lightType;
            }
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            dsc.AppendWoodText(wood: woodType);
            dsc.AppendLine().AppendSelectedSlotText(Block, forPlayer, inventory, DisplaySelectedSlotId, DisplaySelectedSlotStack);
        }
    }
}