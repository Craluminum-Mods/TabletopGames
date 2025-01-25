using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace TabletopGames;

public interface IContainable
{
    public string GetContainableKey(ItemStack stack);
    public MeshData GetInsideContainerMesh(ItemStack stack, ITextureAtlasAPI targetAtlas);
}