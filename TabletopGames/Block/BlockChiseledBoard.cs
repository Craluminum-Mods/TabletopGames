using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace TabletopGames;

/// <summary>
/// Chiseled board that contains one-two chiseled blocks.
/// First chiseled block is used for Hitboxes.
/// Second chiseled block is used for Textures.
/// Works in tandem with BlockEntityChiseledBoard and CollectibleBehaviorChiseledBoardToolModes
/// </summary>
public class BlockChiseledBoard : Block, IContainedMeshSource
{
    public enum EnumStackType
    {
        HitBoxes = 0,
        Textures = 1
    }

    public const string ChiseledStackHitboxesAttributeName = "chiseledStackHitboxes";
    public const string ChiseledStackTexturesAttributeName = "chiseledStackTextures";

    public string? AttributeTransformCode { get; protected set; }

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        if (Attributes != null)
        {
            AttributeTransformCode = Attributes["attributeTransformCode"].AsString();
        }

        if (!GuiDialogTransformEditor.extraTransforms.Any(x => x.AttributeName == AttributeTransformCode))
        {
            GuiDialogTransformEditor.extraTransforms.Add(new TransformConfig()
            {
                Title = Lang.Get($"{TabletopConstants.ModID}:transform-{AttributeTransformCode}"),
                AttributeName = AttributeTransformCode
            });
        }
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        base.OnUnloaded(api);

        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "TabletopGames_BlockChiseledBoard_MeshRefs");
        meshRefs?.Foreach(meshRef => meshRef.Value?.Dispose());
        ObjectCacheUtil.Delete(api, "TabletopGames_BlockChiseledBoard_MeshRefs");

        Dictionary<string, MeshData> meshes = ObjectCacheUtil.TryGet<Dictionary<string, MeshData>>(api, "TabletopGames_BlockChiseledBoard_Meshes");
        meshRefs?.Foreach(mesh => mesh.Value?.Dispose());
        ObjectCacheUtil.Delete(api, "TabletopGames_BlockChiseledBoard_Meshes");
    }

    public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
    {
        base.OnHeldIdle(slot, byEntity);
        SelfDestroyIfEmpty(slot, api.World);
    }

    public override void OnGroundIdle(EntityItem entityItem)
    {
        base.OnGroundIdle(entityItem);
        SelfDestroyIfEmpty(entityItem?.Slot, api.World);
    }

    public override string GetHeldItemName(ItemStack itemStack)
    {
        string? name = GetChiseledStack(itemStack, api.World, EnumStackType.HitBoxes)?.Attributes.GetString("blockName");
        return !string.IsNullOrEmpty(name) ? name : base.GetHeldItemName(itemStack);
    }

    public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
    {
        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityChiseledBoard blockEntity && blockEntity.ChiseledStackHitboxes != null)
        { 
            string? name = blockEntity.ChiseledStackHitboxes?.Attributes.GetString("blockName");
            return !string.IsNullOrEmpty(name) ? name : base.GetPlacedBlockName(world, pos);
        }
        return base.GetPlacedBlockName(world, pos);
    }

    public override string GetItemDescText() => Lang.Get("tabletopgames:blockdesc-chiseledboard-held") + "\n";

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        ItemStack? chiseledStackHitboxes = GetChiseledStack(inSlot.Itemstack, api.World, EnumStackType.HitBoxes);
        ItemStack? chiseledStackTextures = GetChiseledStack(inSlot.Itemstack, api.World, EnumStackType.Textures);

        dsc.AppendLine(chiseledStackHitboxes == null ? Lang.Get("tabletopgames:missing-chiseled-block-for-hitboxes") : Lang.Get("tabletopgames:contains-chiseled-block-for-hitboxes"));
        dsc.AppendLine(chiseledStackTextures == null ? Lang.Get("tabletopgames:missing-chiseled-block-for-textures") : Lang.Get("tabletopgames:contains-chiseled-block-for-textures"));
    }

    public override Cuboidf[]? GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
    {
        return blockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorChiseledBoardSelection>() is BEBehaviorChiseledBoardSelection bebehavior
            ? bebehavior.GetOrCreateSelectionBoxes()
            : base.GetSelectionBoxes(blockAccessor, pos);
    }

    public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
    {
        return blockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorChiseledBoardSelection>() is BEBehaviorChiseledBoardSelection bebehavior
            ? bebehavior.GetOrCreateSelectionBoxes() ?? base.GetCollisionBoxes(blockAccessor, pos)
            : base.GetCollisionBoxes(blockAccessor, pos);
    }

    public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) => TabletopDebug.BoardParticleSelection;
    public override Vec4f GetSelectionColor(ICoreClientAPI capi, BlockPos pos) => TabletopDebug.BoardSelectionColor;

    public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
    {
        bool ok = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
        if (ok && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityChiseledBoard blockEntity)
        {
            BlockPos targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
            double dx = byPlayer.Entity.Pos.X - (targetPos.X + blockSel.HitPosition.X);
            double dz = (float)byPlayer.Entity.Pos.Z - (targetPos.Z + blockSel.HitPosition.Z);
            float angleHor = (float)Math.Atan2(dx, dz);

            float intervalRad = GameMath.PIHALF;
            float roundRad = (int)Math.Round(angleHor / intervalRad) * intervalRad;
            blockEntity.MeshAngleRad = roundRad;

            blockEntity.ChiseledStackHitboxes = GetChiseledStack(byItemStack, world, EnumStackType.HitBoxes);
            blockEntity.ChiseledStackTextures = GetChiseledStack(byItemStack, world, EnumStackType.Textures);

            blockEntity.OnBlockPlaced(byItemStack);
        }
        return ok;
    }

    public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
    {
        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityChiseledBoard blockEntity)
        {
            float[] mat = Matrixf.Create().Translate(0.5f, 0.5f, 0.5f).RotateY(blockEntity.MeshAngleRad).Translate(-0.5f, -0.5f, -0.5f).Values;
            MeshData? decalMesh = GetOrCreateMesh(blockEntity, overrideTexturesource: decalTexSource)?.Clone()?.MatrixTransform(mat);
            MeshData? blockMesh = GetOrCreateMesh(blockEntity)?.Clone()?.MatrixTransform(mat);
            if (decalMesh != null && blockMesh != null)
            {
                decalModelData = decalMesh;
                blockModelData = blockMesh;
                return;
            }
        }

        base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.GetOrCreate(capi, "TabletopGames_BlockChiseledBoard_MeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());

        string key = GetMeshCacheKey(itemstack);
        if (!meshRefs.TryGetValue(key, out MultiTextureMeshRef? meshref))
        {
            MeshData mesh = GenGuiMesh(itemstack);
            meshref = capi.Render.UploadMultiTextureMesh(mesh);
            meshRefs[key] = meshref;
        }

        renderinfo.ModelRef = meshref;
        renderinfo.NormalShaded = true;

        base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
    {
        return world.BlockAccessor.GetBlockEntity(pos) is BlockEntityChiseledBoard blockEntity
            ? (new ItemStack[1] { OnPickBlock(world, pos) })
            : base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    {
        ItemStack stack = base.OnPickBlock(world, pos).Clone();
        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityChiseledBoard blockEntity)
        {
            SetChiseledStack(stack, blockEntity.ChiseledStackHitboxes, EnumStackType.HitBoxes);
            SetChiseledStack(stack, blockEntity.ChiseledStackTextures, EnumStackType.Textures);
        }
        return stack;
    }

    public static void SelfDestroyIfEmpty(ItemSlot? slot, IWorldAccessor world)
    {
        if (slot == null) return;

        ItemStack? stackHitboxes = GetChiseledStack(slot.Itemstack, world, EnumStackType.HitBoxes);
        ItemStack? stackTextures = GetChiseledStack(slot.Itemstack, world, EnumStackType.Textures);
        if (stackHitboxes == null && stackTextures == null)
        {
            slot.Itemstack = null;
            slot.MarkDirty();
        }
    }

    public static void SetChiseledStack(ItemStack ownStack, ItemStack? inputStack, EnumStackType stackType)
    {
        string attribute = stackType switch
        {
            EnumStackType.HitBoxes => ChiseledStackHitboxesAttributeName,
            EnumStackType.Textures => ChiseledStackTexturesAttributeName,
            _ => ""
        };

        if (string.IsNullOrEmpty(attribute)) return;

        ownStack.Attributes.SetItemstack(attribute, inputStack);
    }

    public static ItemStack? GetChiseledStack(ItemStack ownStack, IWorldAccessor worldForResolving, EnumStackType stackType, bool removeAttribute = false)
    {
        string attribute = stackType switch
        {
            EnumStackType.HitBoxes => ChiseledStackHitboxesAttributeName,
            EnumStackType.Textures => ChiseledStackTexturesAttributeName,
            _ => ""
        };
        
        if (string.IsNullOrEmpty(attribute)) return null;

        ItemStack stack = ownStack.Attributes.GetItemstack(attribute);
        stack?.ResolveBlockOrItem(worldForResolving);
        if (removeAttribute)
        {
            ownStack.Attributes.RemoveAttribute(attribute);
        }
        return stack;
    }

    public MeshData GenGuiMesh(ItemStack stack)
    {
        MeshData mesh = RenderExtensions.GenEmptyMesh();

        ItemStack? stackHitboxes = GetChiseledStack(stack, api.World, EnumStackType.HitBoxes);
        ItemStack? stackTextures = GetChiseledStack(stack, api.World, EnumStackType.Textures);
        ItemStack? stackToRender = stackTextures ?? stackHitboxes;

        if (stackToRender != null)
        {
            MeshData containedMesh = stackToRender.CreateChiseledMesh(api);
            mesh.AddMeshData(containedMesh);
        }
        return mesh;
    }

    public MeshData GetOrCreateMesh(BlockEntityChiseledBoard blockEntity, ITexPositionSource? overrideTexturesource = null)
    {
        MeshData mesh = RenderExtensions.GenEmptyMesh();

        ItemStack? stackHitboxes = blockEntity.ChiseledStackHitboxes;
        ItemStack? stackTextures = blockEntity.ChiseledStackTextures;
        ItemStack? stackToRender = stackTextures ?? stackHitboxes;

        if (stackToRender != null)
        {
            MeshData containedMesh = stackToRender.CreateChiseledMesh(api);
            mesh.AddMeshData(containedMesh);
        }
        return mesh;
    }

    public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        return GenGuiMesh(itemstack);
    }

    public string GetMeshCacheKey(ItemStack itemstack)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(itemstack.Collectible.Code);

        ItemStack? stackHitboxes = GetChiseledStack(itemstack, api.World, EnumStackType.HitBoxes);
        ItemStack? stackTextures = GetChiseledStack(itemstack, api.World, EnumStackType.Textures);
        ItemStack? stackToRender = stackTextures ?? stackHitboxes;

        if (stackToRender != null)
        {
            stringBuilder.Append("-chiseledstack:");
            stringBuilder.Append('-');
            stringBuilder.Append(stackToRender.Collectible.Code);
            stringBuilder.Append('-');
            stringBuilder.Append(stackToRender.Attributes.ToJsonToken());
        }
        return stringBuilder.ToString();
    }
}