using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TabletopGames;

public class CollectibleBehaviorRotatableDisplayableProps(CollectibleObject collObj) : AttributeRenderingLibrary.CollectibleBehaviorDisplayableProps(collObj)
{
    public override DisplayableAttributes? GetDisplayableProps(ItemSlot inSlot, string displayType)
    {
        DisplayableAttributes? originalAttr = base.GetDisplayableProps(inSlot, displayType);
        if (originalAttr == null) return originalAttr;

        DisplayableAttributes? attr = originalAttr;

        int rotateY = inSlot.Itemstack!.Attributes.GetInt("rotateY", 0);

        int steps = ((rotateY / 90 % 4) + 4) % 4;

        if (steps == 1 || steps == 3)
        {
            Size3f currentSize = attr.Size;
            attr.Size = new Size3f(currentSize.Length, currentSize.Height, currentSize.Width);
        }
        return attr;
    }
}