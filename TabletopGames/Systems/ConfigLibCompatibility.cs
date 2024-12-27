using ConfigLib;
using ImGuiNET;
using System.Numerics;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TabletopGames;

public class ConfigLibCompatibility
{
    public ConfigLibCompatibility(ICoreAPI api)
    {
        api.ModLoader.GetModSystem<ConfigLibModSystem>().RegisterCustomConfig(TabletopConstants.ModID, (id, buttons) =>
        {
            buttons.Save = false;
            buttons.Restore = false;
            buttons.Defaults = false;
            Edit(api, id);
        });
    }

    private void Edit(ICoreAPI api, string id)
    {
        ImGui.TextWrapped("DEBUG");
        ImGui.Checkbox("Toggle board particle selection" + $"##DEBUG-BoardParticleSelection-{id}", ref TabletopDebug.BoardParticleSelection);
        ColorPicker4VS("Board selection color" + $"##DEBUG-BoardSelectionColor-{id}", ref TabletopDebug.BoardSelectionColor);
    }

    public void ColorPicker4VS(string label, ref Vec4f vec)
    {
        Vector4 vector4 = new Vector4(vec.X, vec.Y, vec.Z, vec.W);
        ImGui.SetNextItemWidth(200);
        ImGui.ColorPicker4(label, ref vector4, ImGuiColorEditFlags.NoAlpha);
        vec = new Vec4f(vector4.X, vector4.Y, vector4.Z, vector4.W);
    }
}
