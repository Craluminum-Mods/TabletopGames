using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;

namespace TabletopGames;

public class BoardData
{
    public Vec2i Size { get; set; }
    public Vec2f SlotYRange { get; set; } = new Vec2f(0, 1);
    public Vec2f Padding { get; set; }
    public int QuantitySlots { get; set; }
    public string AttributeTransformCode { get; set; }
    public Cuboidf[] SlotsHitboxes { get; set; } = System.Array.Empty<Cuboidf>();
    public float SlotMinY => SlotYRange.A / 16f;
    public float SlotMaxY => SlotYRange.B / 16f;

    public Dictionary<string, EnumSlotType> SlotTypes { get; set; } = new();

    public void GetDescription(StringBuilder dsc, int slotIndex = -1)
    {
        if (!TabletopDebug.BoardDataDebugInfo)
        {
            return;
        }

        if (slotIndex >= 0 && SlotTypes.Any())
        {
            string id = slotIndex.ToString();
            foreach ((string wildcard, EnumSlotType slotType) in SlotTypes)
            {
                if (WildcardUtil.Match(wildcard, id))
                {
                    dsc.AppendLine("Slot Type: " + slotType.ToString());
                    break;
                }
            }
        }
        else
        {
            dsc.AppendLine("Slot Type: " + EnumSlotType.Normal.ToString());
        }

        if (Size != null)
        {
            dsc.AppendLine(string.Format("DEBUG::Board Dimensions: X: {0}, Y: {1}", Size.X, Size.Y));
        }
        if (Padding != null)
        {
            dsc.AppendLine(string.Format("DEBUG::Padding: X: {0}, Y: {1}", Padding.X, Padding.Y));
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