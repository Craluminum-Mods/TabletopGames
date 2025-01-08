using Vintagestory.API.Common;

namespace TabletopGames.Configuration;

public class ConfigClient : IModConfig
{
    public const string ConfigName = "TabletopGames-Client.json";

    public bool DiceAnimationsEnabled { get; set; } = true;

    public ConfigClient(ICoreAPI api, ConfigClient previousConfig = null)
    {
        if (previousConfig != null)
        {
            DiceAnimationsEnabled = previousConfig.DiceAnimationsEnabled;
        }
    }
}