using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TabletopGames;

public static class RenderExtensions
{
    public static RenderSkillItemDelegate RenderItemStack(this ItemStack stack, ICoreClientAPI capi, bool showStackSize = false)
    {
        return (AssetLocation code, float dt, double posX, double posY) =>
        {
            double size = GuiElementPassiveItemSlot.unscaledSlotSize + GuiElementItemSlotGridBase.unscaledSlotPadding;
            double scsize = GuiElement.scaled(size - 5);

            capi.Render.RenderItemstackToGui(
                new DummySlot(stack),
                posX + (scsize / 2),
                posY + (scsize / 2),
                100,
                (float)GuiElement.scaled(GuiElementPassiveItemSlot.unscaledItemSize),
                ColorUtil.WhiteArgb,
                showStackSize: showStackSize);
        };
    }
}
