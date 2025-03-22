using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace TabletopGames;

public interface IContainedTransform
{
    public ModelTransform? GetTransform(BlockEntityDisplay be, string attributeTransformCode, ItemStack itemStack);
}