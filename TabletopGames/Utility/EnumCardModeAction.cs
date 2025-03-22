namespace TabletopGames;

/// <summary>
/// The action in tool mode menu of the playing card
/// </summary>
public enum EnumCardModeAction
{
    None,
    /// <summary>
    /// Take card if mouse slot is empty
    /// </summary>
    Take,
    /// <summary>
    /// Exchange card if mouse slot is not empty
    /// </summary>
    Exchange,
    /// <summary>
    /// Add new card before current card if mouse slot is not empty and Ctrl key is pressed
    /// </summary>
    AddPrev,
    /// <summary>
    /// Add new card after current card if mouse slot is not empty and Shift key is pressed
    /// </summary>
    AddNext
}
