using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace TabletopGames;

public interface IBoardPreviewRendererHelper
{
    MeshData GetOrCreateMesh(ItemStack stack, int selectionIndex);
    bool TryGetSlot(int selectionIndex, out ItemSlot boardSlot);
    void ApplyPieceMeshRotation(ItemStack stack, ref MeshData pieceMesh);
    void SetPieceRotation(ItemStack stack, IPlayer player);
    float[][] GenTransformationMatrices();
}
