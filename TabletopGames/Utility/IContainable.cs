using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace TabletopGames;

/// <summary>
/// Defines behavior for items that can be stored inside an ItemContainer.
/// Provides a containable key for compatibility checks and a method for rendering the item inside the container.
/// </summary>
public interface IContainable
{
    /// <summary>
    /// Retrieves a unique containable key for an item.
    /// This key is used to determine whether the item can be stored in a specific ItemContainer.
    /// </summary>
    /// <param name="stack">The item stack to retrieve the key from.</param>
    /// <returns>The containable key of the item.</returns>
    public string GetContainableKey(ItemStack stack);

    /// <summary>
    /// Generates mesh for rendering the item inside an ItemContainer.
    /// </summary>
    /// <param name="stack">The item stack to generate the mesh for.</param>
    /// <param name="targetAtlas">The texture atlas to use for the mesh.</param>
    /// <returns>The generated mesh for rendering.</returns>
    public MeshData GetInsideContainerMesh(ItemStack stack, ITextureAtlasAPI targetAtlas);
}