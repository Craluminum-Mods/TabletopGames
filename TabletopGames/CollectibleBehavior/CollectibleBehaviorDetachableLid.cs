using Vintagestory.API.Common;

namespace TabletopGames;

/// <summary>
/// Ensures proper mesh rendering when a lid with Variants is attached to ItemContainer.  
/// </summary>
public class CollectibleBehaviorDetachableLid : CollectibleBehaviorContainableTyped, IContainable
{
    public CollectibleBehaviorDetachableLid(CollectibleObject collObj) : base(collObj) { }

    bool IContainable.IsDetachableLid => true;
}