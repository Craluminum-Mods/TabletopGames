using System.Text;
using Vintagestory.API.MathTools;

namespace TabletopGames;

public class BoardData
{
    public Vec2i Size { get; set; }
    public int QuantitySlots { get; set; }
    public string AttributeTransformCode { get; set; }

    public void GetDescription(StringBuilder dsc)
    {
        if (!TabletopDebug.BoardDataDebugInfo)
        {
            return;
        }

        dsc.AppendLine(string.Format("DEBUG::Board Dimensions: X: {0}, Y: {1}", Size.X, Size.Y));
        dsc.AppendLine(string.Format("DEBUG::Quantity Slots: {0}", QuantitySlots));
        dsc.AppendLine(string.Format("DEBUG::Transform Code: {0}", AttributeTransformCode));
    }
}