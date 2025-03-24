using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TabletopGames;

/// <summary>
/// Renders a preview of the held item as if it were placed on a board.
/// </summary>
public class BoardPreviewRenderer : IRenderer, IDisposable
{
    protected BlockPos pos;
    protected ICoreClientAPI api;

    protected MultiTextureMeshRef? heldItemMeshRef;

    double IRenderer.RenderOrder => 0.5;
    int IRenderer.RenderRange => 8;

    public BoardPreviewRenderer(BlockPos pos, ICoreClientAPI api)
    {
        this.api = api;
        this.pos = pos;
    }

    void IRenderer.OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        if (api == null) return;
        if (!api.PlayerReadyFired) return;

        UpdatePreviewMesh();
        if (heldItemMeshRef == null || api.World.BlockAccessor.GetBlock(pos)?.GetInterface<IBoardPreviewRendererHelper>(api.World, pos) is not IBoardPreviewRendererHelper previewHelper)
        {
            return;
        }

        int selectionIndex = api.World.Player.CurrentBlockSelection?.SelectionBoxIndex ?? -1;
        if (selectionIndex < 0) return;

        float[][] tfMatrices = previewHelper.GenTransformationMatrices();

        float[] preMat = tfMatrices[selectionIndex];
        if (preMat == null) return;

        Matrixf mat = new Matrixf(preMat);

        IRenderAPI render = api.Render;
        Vec3d cameraPos = api.World.Player.Entity.CameraPos;
        render.GlDisableCullFace();
        render.GlToggleBlend(blend: true, EnumBlendMode.PremultipliedAlpha);
        IStandardShaderProgram standardShaderProgram = render.PreparedStandardShader(pos.X, pos.Y, pos.Z);
        standardShaderProgram.ModelMatrix = mat.Translate(pos.X - cameraPos.X, pos.Y - cameraPos.Y, pos.Z - cameraPos.Z).Values;
        standardShaderProgram.ViewMatrix = render.CameraMatrixOriginf;
        standardShaderProgram.ProjectionMatrix = render.CurrentProjectionMatrix;
        standardShaderProgram.NormalShaded = 0;
        standardShaderProgram.ExtraGodray = 0f;
        standardShaderProgram.SsaoAttn = 0f;
        standardShaderProgram.AlphaTest = 0.05f;
        standardShaderProgram.OverlayOpacity = 0f;
        standardShaderProgram.RgbaLightIn = new Vec4f(1, 1, 1, 0.1f);
        render.RenderMultiTextureMesh(heldItemMeshRef, "tex");
        standardShaderProgram.Stop();
        render.GlToggleBlend(blend: true);
    }

    internal void UpdatePreviewMesh()
    {
        ItemSlot hotbarSlot = api.World.Player.Entity.RightHandItemSlot;
        ItemStack hotbarStack = hotbarSlot.Itemstack;

        BlockSelection blockSel = api.World.Player.CurrentBlockSelection;
        int selectionIndex = blockSel?.SelectionBoxIndex ?? 0;

        if (hotbarStack == null || blockSel == null || blockSel.Position != pos)
        {
            heldItemMeshRef?.Dispose();
            heldItemMeshRef = null;
            return;
        }

        IBoardPreviewRendererHelper? previewHelper = api.World.BlockAccessor.GetBlock(pos)?.GetInterface<IBoardPreviewRendererHelper>(api.World, pos);
        BEBehaviorBoardInteractions? behaviorBoardInteractions =  api.World.BlockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorBoardInteractions>();

        if (previewHelper == null
            || behaviorBoardInteractions == null
            || !behaviorBoardInteractions.TryGetSlot(selectionIndex, out ItemSlot boardSlot)
            || !boardSlot.Empty
            || !boardSlot.CanHold(hotbarSlot))
        {
            heldItemMeshRef?.Dispose();
            heldItemMeshRef = null;
            return;
        }

        BEBehaviorBoardInteractions.SetPieceRotation(hotbarStack, api.World.Player);
        MeshData heldItemMesh = previewHelper.GetOrCreateMesh(hotbarStack, selectionIndex).Clone();
        BEBehaviorBoardInteractions.ApplyPieceMeshRotation(hotbarStack, ref heldItemMesh);

        heldItemMeshRef?.Dispose();
        heldItemMeshRef = null;

        if (heldItemMesh != null)
        {
            heldItemMeshRef = api.Render.UploadMultiTextureMesh(heldItemMesh);
        }
    }

    void IDisposable.Dispose()
    {
        api.Event.UnregisterRenderer(this, EnumRenderStage.OIT);
        api.Event.UnregisterRenderer(this, EnumRenderStage.AfterOIT);
        heldItemMeshRef?.Dispose();
    }
}