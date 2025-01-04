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
        if (ImGui.CollapsingHeader($"DEBUG##DEBUG-{id}"))
        {
            ImGui.Indent();
            ImGui.Checkbox("Toggle variants debug info" + $"##DEBUG-VariantsDebugInfo-{id}", ref TabletopDebug.VariantsDebugInfo);
            ImGui.Checkbox("Toggle board data debug info" + $"##DEBUG-BoardDataDebugInfo-{id}", ref TabletopDebug.BoardDataDebugInfo);
            ImGui.Checkbox("Toggle tags debug info" + $"##DEBUG-TagsDebugInfo-{id}", ref TabletopDebug.TagsDebugInfo);
            ImGui.Checkbox("Toggle board particle selection" + $"##DEBUG-BoardParticleSelection-{id}", ref TabletopDebug.BoardParticleSelection);
            if (ImGui.CollapsingHeader($"Selection colors##DEBUG-{id}"))
            {
                ImGui.Indent();
                ColorPicker4VS("Board selection color" + $"##DEBUG-BoardSelectionColor-{id}", ref TabletopDebug.BoardSelectionColor);
                ImGui.Unindent();
            }
            if (api is ICoreClientAPI capi)
            {
                BlockSelection selection = capi?.World?.Player?.CurrentBlockSelection;
                if (selection != null && capi.World.BlockAccessor.GetBlockEntity(selection.Position) is BlockEntityBoard blockEntity)
                {
                    StealSelBox(id, blockEntity);
                    ImGui.NewLine();
                    EditBoardData(id, blockEntity);
                }
            }
            ImGui.Unindent();
        }
    }

    private static void EditBoardData(string id, BlockEntityBoard blockEntity)
    {
        if (blockEntity.BoardData != null)
        {
            EditPadding(id, blockEntity);
        }
    }

    private static void EditPadding(string id, BlockEntityBoard blockEntity)
    {
        if (blockEntity.BoardData.Padding == null)
        {
            return;
        }

        ICoreClientAPI capi = blockEntity.Api as ICoreClientAPI;

        Vector2 padding = new Vector2(blockEntity.BoardData.Padding.X, blockEntity.BoardData.Padding.Y);
        if (ImGui.InputFloat2("edit padding" + $"##DEBUG-EditPadding-{id}", ref padding))
        {
            Vec2f newPadding = new Vec2f(padding.X, padding.Y);
            blockEntity.BoardData.Padding = newPadding;
            blockEntity.GetOrCreateSelectionBoxes(forceNew: true);
        }
        if (ImGui.Button("Copy padding" + $"##DEBUG-CopyPadding-{id}"))
        {
            StringBuilder dsc = new();
            dsc.Append("\"padding\": { \"x\": " + blockEntity.BoardData.Padding.X.ToString() + ", \"y\": " + blockEntity.BoardData.Padding.Y.ToString() + " }");

            if (capi != null)
            {
                capi.Input.ClipboardText = dsc.ToString();
            }
        }
    }

    private static void StealSelBox(string id, BlockEntityBoard blockEntity)
    {
        ICoreClientAPI capi = blockEntity.Api as ICoreClientAPI;

        bool stealAll = ImGui.Button("Steal whole selected block selection" + $"##DEBUG-StealWholeSelectedBlockSelection-{id}");
        bool stealOne = ImGui.Button("Steal selected block selection" + $"##DEBUG-StealSelectedBlockSelection-{id}");

        if (!stealAll && !stealOne)
        {
            return;
        }

        StringBuilder sb = new();

        int selectionBoxIndex = capi.World.Player.CurrentBlockSelection.SelectionBoxIndex;

        Cuboidf[] cuboids = blockEntity.GetOrCreateSelectionBoxes();
        if (stealAll)
        {
            for (int i = 0; i < cuboids.Length; i++)
            {
                SaveSelBoxAsText(sb, cuboids[i], i);
            }
        }
        else if (stealOne)
        {
            Cuboidf box = cuboids[selectionBoxIndex];
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
