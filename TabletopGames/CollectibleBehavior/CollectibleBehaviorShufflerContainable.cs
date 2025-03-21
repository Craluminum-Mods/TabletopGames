using Vintagestory.API.Common;

namespace TabletopGames;

/// <summary>
/// Allows to store collectible in ItemShuffler
/// </summary>
public class CollectibleBehaviorShufflerContainable : CollectibleBehavior, IShufflerContainable
{
    public CollectibleBehaviorShufflerContainable(CollectibleObject collObj) : base(collObj) { }
}
