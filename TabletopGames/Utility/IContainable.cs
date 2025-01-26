using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace TabletopGames;

/// <summary>
/// Defines behavior for items that can be stored inside an ItemContainer.
/// Provides a containable key for compatibility checks and a method for rendering the item inside the container.
/// </summary>
public interface IContainable
{
    public virtual bool IsDetachableLid => false;

    public bool IsSuitableForContainer(string containerKey);

    public ContainableProperties GetContainableProperties(string containerKey);

    /// <summary>
    /// Generates mesh for rendering the item inside an ItemContainer.
    /// </summary>
    /// <param name="stack">The item stack to generate the mesh for.</param>
    /// <param name="targetAtlas">The texture atlas to use for the mesh.</param>
    /// <returns>The generated content mesh for rendering.</returns>
    public MeshData GenContentMesh(string containerKey, ItemStack stack, ITextureAtlasAPI targetAtlas);
}