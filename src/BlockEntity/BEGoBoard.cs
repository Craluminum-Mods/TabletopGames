using System.Text;
using Vintagestory.API.Common;
using TabletopGames.Utils;

namespace TabletopGames
{
    public class BEGoBoard : BEBoard
    {
        public override string InventoryClassName => "ttggoboard";
        public override bool HasWoodType => true;

        public override NewSlotDelegate OnNewSlot() => (f, f2) => new ItemSlotGoBoard(f2);

        public override string MeshesKey => "ttg_goBoardBlockMeshes";

        public override string MeshCacheKey
        {
            get
            {
                string size = Block?.VariantStrict?["size"];
                return size + "-" + woodType;
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