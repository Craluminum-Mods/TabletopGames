using Cairo;
using Vintagestory.API.Client;

namespace TabletopGames.Utils
{
    public static class SkillItemUtils
    {
        public static SkillItem WithSmallLetterIcon(this SkillItem skillitem, ICoreClientAPI capi, string letter)
        {
            if (capi == null)
            {
                return skillitem;
            }
            int isize = (int)GuiElement.scaled(48.0);
            skillitem.Texture = capi.Gui.Icons.GenTexture(isize, isize, (Context ctx, ImageSurface surface) =>
            {
                CairoFont cairoFont = CairoFont.WhiteSmallText().WithColor(new double[4] { 1.0, 1.0, 1.0, 1.0 });
                cairoFont.SetupContext(ctx);
                TextExtents textExtents = cairoFont.GetTextExtents(letter);
                double ascent = cairoFont.GetFontExtents().Ascent;
                double num = GuiElement.scaled(2.0);
                capi.Gui.Text.DrawTextLine(ctx, cairoFont, letter, (isize - textExtents.Width) / 2.0, (isize - (ascent + num)) / 2.0);
            });
            return skillitem;
        }
    }
}