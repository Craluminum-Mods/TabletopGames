using Vintagestory.API.Common;

namespace TabletopGames
{
    public class BEMorrisBoard : BEGoBoard
    {
        public override string InventoryClassName => "ttgmorrisboard";
        public override bool HasWoodType => true;

        public override NewSlotDelegate OnNewSlot() => (f, f2) => new ItemSlotGoBoard(f2);

        public override string MeshesKey => "ttg_morrisBoardBlockMeshes";
    }
}