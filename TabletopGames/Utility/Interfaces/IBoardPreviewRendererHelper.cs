using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace TabletopGames;

public interface IBoardPreviewRendererHelper
{
    MeshData GetOrCreateMesh(ItemStack stack, int selectionIndex);
    float[][] GenTransformationMatrices();
}
