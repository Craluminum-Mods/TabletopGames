using Vintagestory.API.Common;

namespace TabletopGames;

public interface IHandbookTweaks
{
    bool CanRedirect(ItemStack stack, out ItemStack newStack);
}