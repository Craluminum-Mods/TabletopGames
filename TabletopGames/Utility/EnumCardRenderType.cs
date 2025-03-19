namespace TabletopGames;

/// <summary>
/// The render type for a card item stack
/// </summary>
public enum EnumCardRenderType
{
    /// <summary>
    /// Rendered in a UI, usually the inventory
    /// </summary>
    Gui,

    /// <summary>
    /// Rendered when inside BlockEntityDisplay:  ground storages, shelves, display cases etc.
    /// </summary>
    Stack,

    /// <summary>
    /// Rendered in the players hand, third person mode
    /// </summary>
    Hand,

    /// <summary>
    /// Rendered in the players hand, third person mode. From another person's perspective
    /// </summary>
    HandSafe
}