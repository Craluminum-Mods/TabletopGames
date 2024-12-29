using System.Text;
using Vintagestory.API.MathTools;

namespace TabletopGames;

public class BoardData
{
    public Vec2i Size { get; set; }
    public Vec2f SlotYRange { get; set; } = new Vec2f(0, 1);
    public Vec2f Padding { get; set; } = Vec2f.Zero;
    public int QuantitySlots { get; set; }
    public string AttributeTransformCode { get; set; }
    public Cuboidf[] SlotsHitboxes { get; set; } = System.Array.Empty<Cuboidf>();

    public float SlotMinY => SlotYRange.A / 16f;
    public float SlotMaxY => SlotYRange.B / 16f;

    public void GetDescription(StringBuilder dsc)
    {
        if (!TabletopDebug.BoardDataDebugInfo)
        {
            return;
        }

        if (Size != null)
        {
            dsc.AppendLine(string.Format("DEBUG::Board Dimensions: X: {0}, Y: {1}", Size.X, Size.Y));
        }
        dsc.AppendLine(string.Format("DEBUG::Quantity Slots: {0}", QuantitySlots));
        dsc.AppendLine(string.Format("DEBUG::Slot MinY: {0}", SlotMinY));
        dsc.AppendLine(string.Format("DEBUG::Slot MaxY: {0}", SlotMaxY));
        dsc.AppendLine(string.Format("DEBUG::Transform Code: {0}", AttributeTransformCode));
    }

    public override string ToString()
    {
        StringBuilder result = new StringBuilder();
        if (Size != null)
        {
            result.Append(Size);
            result.Append('-');
        }
        result.Append(QuantitySlots);
        result.Append('-');
        result.Append(AttributeTransformCode);
        return result.ToString();
    }
}