using Vintagestory.API.Common;

namespace TabletopGames;

/// <summary>
/// Allows to store collectible in ItemShuffler
/// </summary>
public class CollectibleBehaviorShufflerContainable(CollectibleObject collObj) : CollectibleBehavior(collObj), IShufflerContainable
{
}