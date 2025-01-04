using ConfigLib;
using ImGuiNET;
using System.Collections.Generic;
using System.Linq;
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

    private static List<Cuboidf> SelectedSelectionBoxes { get; set; } = new();

    private void Edit(ICoreAPI api, string id)
    {
        if (ImGui.CollapsingHeader($"Debug##Debug-{id}"))
        {
            ImGui.Indent();

            ImGui.Checkbox($"Show variants debug info##VariantsDebug-{id}", ref TabletopDebug.VariantsDebugInfo);
            ImGui.Checkbox($"Show board data debug info##BoardDataDebug-{id}", ref TabletopDebug.BoardDataDebugInfo);
            ImGui.Checkbox($"Show tags debug info##TagsDebug-{id}", ref TabletopDebug.TagsDebugInfo);
            ImGui.Checkbox($"Enable board particle selection##ParticleSelection-{id}", ref TabletopDebug.BoardParticleSelection);

            if (ImGui.CollapsingHeader($"Selection Colors##SelectionColors-{id}"))
            {
                ImGui.Indent();
                ColorPicker4VS($"Board Selection Color##SelectionColor-{id}", ref TabletopDebug.BoardSelectionColor);
                ImGui.Unindent();
            }

            if (api is ICoreClientAPI capi)
            {
                BlockSelection selection = capi?.World?.Player?.CurrentBlockSelection;
                if (selection != null && capi.World.BlockAccessor.GetBlockEntity(selection.Position) is BlockEntityBoard blockEntity)
                {
                    ManageSelectionBoxes(id, blockEntity);
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
            ManagePadding(id, blockEntity);
        }
    }

    private static void ManagePadding(string id, BlockEntityBoard blockEntity)
    {
        ICoreClientAPI capi = blockEntity.Api as ICoreClientAPI;

        Vec4f oldPadding = blockEntity.BoardData.Padding;
        Vector4 padding = new Vector4(oldPadding.X, oldPadding.Y, oldPadding.Z, oldPadding.W);

        if (ImGui.InputFloat4($"Edit Padding##EditPadding-{id}", ref padding))
        {
            Vec4f newPadding = new Vec4f(padding.X, padding.Y, padding.Z, padding.W);
            blockEntity.BoardData.Padding = newPadding;
            blockEntity.GetOrCreateSelectionBoxes(forceNew: true);
        }

        if (ImGui.Button($"Copy Padding##CopyPadding-{id}"))
        {
            StringBuilder dsc = new();
            dsc.Append($"\"padding\": {{ \"x\": {blockEntity.BoardData.Padding.X}, ");
            dsc.Append($"\"y\": {blockEntity.BoardData.Padding.Y}, ");
            dsc.Append($"\"z\": {blockEntity.BoardData.Padding.Z}, ");
            dsc.Append($"\"w\": {blockEntity.BoardData.Padding.W} }}");

            if (capi != null)
            {
                capi.Input.ClipboardText = dsc.ToString();
            }
        }
    }

    private static void ManageSelectionBoxes(string id, BlockEntityBoard blockEntity)
    {
        ICoreClientAPI capi = blockEntity.Api as ICoreClientAPI;
        ImGui.NewLine();

        bool addBoxToList = ImGui.Button($"Add Selection Box to List##SelectionBoxes-AddToList-{id}");
        bool copyListToClipboard = ImGui.Button($"Copy Selection Box List to Clipboard##SelectionBoxes-CopyList-{id}");
        bool clearList = ImGui.Button($"Clear Selection Box List##ClearList-{id}");
        ImGui.NewLine();
        bool copyAllBoxes = ImGui.Button($"Copy All Selection Boxes to Clipboard##SelectionBoxes-CopyAll-{id}");
        bool copySelectedBox = ImGui.Button($"Copy Selected Selection Box to Clipboard##SelectionBoxes-CopySelected-{id}");
        ImGui.NewLine();
        bool copyAndApplyRotated90 = ImGui.Button($"Rotate by 90 & Copy Selection Boxes To Clipboard##SelectionBoxes-CopyApplyRotated90-{id}");
        bool copyAndApplyRotated180 = ImGui.Button($"Rotate by 180 & Copy Selection Boxes To Clipboard##SelectionBoxes-CopyApplyRotated180-{id}");

        if (clearList)
        {
            SelectedSelectionBoxes.Clear();
            return;
        }

        if (!addBoxToList && !copyListToClipboard && !copyAllBoxes && !copySelectedBox && !copyAndApplyRotated90 && !copyAndApplyRotated180) return;

        Cuboidf[] cuboids = blockEntity.GetOrCreateSelectionBoxes();

        int selectedIndex = capi.World.Player.CurrentBlockSelection.SelectionBoxIndex;

        if (addBoxToList)
        {
            if (selectedIndex >= 0 && selectedIndex < cuboids.Length)
            {
                SelectedSelectionBoxes.Add(cuboids[selectedIndex]);
            }
            return;
        }

        StringBuilder sb = new();
        if (copyAllBoxes)
        {
            for (int i = 0; i < cuboids.Length; i++)
            {
                AppendSelectionBox(sb, cuboids[i], i);
            }
        }
        else if (copyAndApplyRotated90 || copyAndApplyRotated180)
        {
            int rotatedBy = 90;
            if (copyAndApplyRotated180)
            {
                rotatedBy = 180;
            }

            List<Cuboidf> newCuboids = cuboids.DeepClone().Select(x => x.RotatedCopy(0, rotatedBy, 0, new Vec3d(0.5, 0.5, 0.5))).ToList();
            for (int i = 0; i < newCuboids.Count; i++)
            {
                AppendSelectionBox(sb, newCuboids[i], i);
            }
            blockEntity.SetSelectionBoxes(newCuboids.ToArray());
        }
        else if (copySelectedBox)
        {
            AppendSelectionBox(sb, cuboids[selectedIndex], selectedIndex);
        }
        else if (copyListToClipboard)
        {
            for (int i = 0; i < SelectedSelectionBoxes.Count; i++)
            {
                AppendSelectionBox(sb, SelectedSelectionBoxes[i], i);
            }
        }

        capi.Input.ClipboardText = sb.ToString();
    }

    private static void AppendSelectionBox(StringBuilder sb, Cuboidf box, int index)
    {
        sb.Append($"{{ \"index\": \"{index}\", ");
        sb.Append($"  \"x1\": {box.X1}, \"y1\": {box.Y1}, \"z1\": {box.Z1}, ");
        sb.AppendLine($"  \"x2\": {box.X2}, \"y2\": {box.Y2}, \"z2\": {box.Z2} }},");
    }

    private void ColorPicker4VS(string label, ref Vec4f vec)
    {
        Vector4 vector4 = new Vector4(vec.X, vec.Y, vec.Z, vec.W);
        ImGui.SetNextItemWidth(200);
        ImGui.ColorPicker4(label, ref vector4, ImGuiColorEditFlags.NoAlpha);
        vec = new Vec4f(vector4.X, vector4.Y, vector4.Z, vector4.W);
    }
}
