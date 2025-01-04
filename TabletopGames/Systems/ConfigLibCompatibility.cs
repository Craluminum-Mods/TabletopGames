using ConfigLib;
using ImGuiNET;
using System.Numerics;
using System.Text;
using Vintagestory.API.Client;
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
        if (ImGui.CollapsingHeader("DEBUG##DEBUG-{id}"))
        {
            ImGui.Indent();
            if (api is ICoreClientAPI capi)
            {
                if (ImGui.Button("Steal selected block selection" + $"##DEBUG-StealSelectedBlockSelection-{id}"))
                {
                    StealSelBox(capi);

                    var aa = capi.World.Player.CurrentBlockSelection?.SelectionBoxIndex;
                }
                if (ImGui.Button("Steal whole selected block selection" + $"##DEBUG-StealWholeSelectedBlockSelection-{id}"))
                {
                    var aa = capi.World.Player.CurrentBlockSelection?.SelectionBoxIndex;
                    StealSelBox(capi, whole: true);
                }
            }

            ImGui.Checkbox("Toggle variants debug info" + $"##DEBUG-VariantsDebugInfo-{id}", ref TabletopDebug.VariantsDebugInfo);
            ImGui.Checkbox("Toggle board data debug info" + $"##DEBUG-BoardDataDebugInfo-{id}", ref TabletopDebug.BoardDataDebugInfo);
            ImGui.Checkbox("Toggle tags debug info" + $"##DEBUG-TagsDebugInfo-{id}", ref TabletopDebug.TagsDebugInfo);
            ImGui.Checkbox("Toggle board particle selection" + $"##DEBUG-BoardParticleSelection-{id}", ref TabletopDebug.BoardParticleSelection);
            ColorPicker4VS("Board selection color" + $"##DEBUG-BoardSelectionColor-{id}", ref TabletopDebug.BoardSelectionColor);
            ImGui.Unindent();
        }
    }

    private static void StealSelBox(ICoreClientAPI capi, bool whole = false)
    {
        BlockSelection selection = capi?.World?.Player?.CurrentBlockSelection;
        if (selection == null || capi.World.BlockAccessor.GetBlockEntity(selection.Position) is not BlockEntityBoard blockEntity)
        {
            return;
        }

        StringBuilder sb = new();

        int selectionBoxIndex = selection.SelectionBoxIndex;

        Cuboidf[] cuboids = blockEntity.GetOrCreateSelectionBoxes();
        if (whole)
        {
            for (int i = 0; i < cuboids.Length; i++)
            {
                SaveSelBoxAsText(sb, cuboids[i], i);
            }
        }
        else
        {
            Cuboidf box = blockEntity.GetOrCreateSelectionBoxes()[selectionBoxIndex];
            SaveSelBoxAsText(sb, box, selectionBoxIndex);
        }

        capi.Input.ClipboardText = sb.ToString();
    }

    private static void SaveSelBoxAsText(StringBuilder sb, Cuboidf box, int i)
    {
        sb.Append("{ \"___cmt\": \"" + i + "\", ");
        sb.Append("\"" + nameof(box.X1).ToLower() + "\": " + box.X1.ToString() + ", ");
        sb.Append("\"" + nameof(box.Y1).ToLower() + "\": " + box.Y1.ToString() + ", ");
        sb.Append("\"" + nameof(box.Z1).ToLower() + "\": " + box.Z1.ToString() + ", ");
        sb.Append("\"" + nameof(box.X2).ToLower() + "\": " + box.X2.ToString() + ", ");
        sb.Append("\"" + nameof(box.Y2).ToLower() + "\": " + box.Y2.ToString() + ", ");
        sb.Append("\"" + nameof(box.Z2).ToLower() + "\": " + box.Z2.ToString());
        sb.AppendLine(" },");
    }

    private void ColorPicker4VS(string label, ref Vec4f vec)
    {
        Vector4 vector4 = new Vector4(vec.X, vec.Y, vec.Z, vec.W);
        ImGui.SetNextItemWidth(200);
        ImGui.ColorPicker4(label, ref vector4, ImGuiColorEditFlags.NoAlpha);
        vec = new Vec4f(vector4.X, vector4.Y, vector4.Z, vector4.W);
    }
}
