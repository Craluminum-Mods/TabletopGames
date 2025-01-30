using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TabletopGames;

/// <summary>
/// Base class for blocks that render shapes and textures dynamically from attributes.
/// Implements a type system for variations in shape, texture, and selection boxes.
/// </summary>
public abstract class BlockShapeTexturesFromAttributes : Block, IContainedMeshSource
{
    public Dictionary<string, List<object>> NameByType { get; protected set; } = new();
    public Dictionary<string, List<object>> DescriptionByType { get; protected set; } = new();
    public Dictionary<string, Cuboidf[]> ExtraSelectionBoxesByType { get; protected set; } = new();

    protected Dictionary<string, CompositeShape> shapeByType = new();
    protected Dictionary<string, Dictionary<string, CompositeTexture>> texturesByType = new();

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        LoadTypes();
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        base.OnUnloaded(api);
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "TabletopGames_boardMeshRefs");
        meshRefs?.Foreach(meshRef => meshRef.Value?.Dispose());
        ObjectCacheUtil.Delete(api, "TabletopGames_boardMeshRefs");
    }

    public virtual void LoadTypes()
    {
        if (Attributes != null)
        {
            NameByType = Attributes["name"].AsObject(defaultValue: new Dictionary<string, List<object>>());
            DescriptionByType = Attributes["description"].AsObject(defaultValue: new Dictionary<string, List<object>>());
            ExtraSelectionBoxesByType = Attributes["extraSelectionBoxes"].AsObject(defaultValue: new Dictionary<string, Cuboidf[]>());

            shapeByType = Attributes["shape"].AsObject(defaultValue: new Dictionary<string, CompositeShape>());
            texturesByType = Attributes["textures"].AsObject(defaultValue: new Dictionary<string, Dictionary<string, CompositeTexture>>());
        }
    }

    public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
    {
        bool ok = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
        if (ok && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityDisplayShapeTexturesFromAttributes blockEntiy)
        {
            BlockPos targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
            double dx = byPlayer.Entity.Pos.X - (targetPos.X + blockSel.HitPosition.X);
            double dz = (float)byPlayer.Entity.Pos.Z - (targetPos.Z + blockSel.HitPosition.Z);
            float angleHor = (float)Math.Atan2(dx, dz);

            float intervalRad = GameMath.PIHALF;
            float roundRad = (int)Math.Round(angleHor / intervalRad) * intervalRad;
            blockEntiy.MeshAngleRad = roundRad;

            blockEntiy.OnBlockPlaced(byItemStack);
        }
        return ok;
    }

    public virtual MeshData GetOrCreateMesh(Variants variants, ITexPositionSource overrideTexturesource = null)
    {
        ICoreClientAPI capi = api as ICoreClientAPI;
        MeshData mesh = new MeshData(4, 3);

        variants.FindByVariant(shapeByType, out CompositeShape _shape);
        if (_shape == null) return mesh;

        CompositeShape rcshape = _shape.Clone();
        rcshape.Base.Path = variants.ReplacePlaceholders(rcshape.Base.Path);
        rcshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");

        Shape shape = capi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();

        ITexPositionSource texSource = null;
        if (overrideTexturesource != null)
        {
            texSource = overrideTexturesource;
        }
        if (texSource == null)
        {
            variants.FindByVariant(texturesByType, out Dictionary<string, CompositeTexture> _textures);
            _textures ??= new Dictionary<string, CompositeTexture>();

            ShapeTextureSource stexSource = new ShapeTextureSource(capi, shape, rcshape.Base.ToString());
            texSource = stexSource;
            foreach (KeyValuePair<string, CompositeTexture> val in _textures)
            {
                CompositeTexture ctex = val.Value.Clone();
                ctex.Base.Path = variants.ReplacePlaceholders(ctex.Base.Path);
                ctex.BlendedOverlays?.Foreach(overlay => overlay.Base.Path = variants.ReplacePlaceholders(overlay.Base.Path));
                ctex.Bake(capi.Assets);
                stexSource.textures[val.Key] = ctex;
            }
        }
        if (shape == null) return mesh;
        capi.Tesselator.TesselateShape("ShapeTexturesFromAttributes block", shape, out mesh, texSource);
        return mesh;
    }

    public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
    {
        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityDisplayShapeTexturesFromAttributes blockEntiy)
        {
            float[] mat = Matrixf.Create().Translate(0.5f, 0.5f, 0.5f).RotateY(blockEntiy.MeshAngleRad).Translate(-0.5f, -0.5f, -0.5f).Values;
            MeshData decalMesh = GetOrCreateMesh(blockEntiy.Variants, overrideTexturesource: decalTexSource).Clone().MatrixTransform(mat);
            MeshData blockMesh = GetOrCreateMesh(blockEntiy.Variants).Clone().MatrixTransform(mat);
            decalModelData = decalMesh;
            blockModelData = blockMesh;
            return;
        }

        base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.GetOrCreate(capi, "TabletopGames_boardMeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());

        Variants variants = Variants.FromStack(itemstack);
        string key = GetMeshCacheKey(itemstack);
        if (!meshRefs.TryGetValue(key, out MultiTextureMeshRef meshref))
        {
            MeshData mesh = GetOrCreateMesh(variants);
            meshref = capi.Render.UploadMultiTextureMesh(mesh);
            meshRefs[key] = meshref;
        }

        renderinfo.ModelRef = meshref;
        renderinfo.NormalShaded = true;

        base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
    {
        return world.BlockAccessor.GetBlockEntity(pos) is BlockEntityDisplayShapeTexturesFromAttributes blockEntiy
            ? (new ItemStack[1] { OnPickBlock(world, pos) })
            : base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
    }

    public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
    {
        BlockDropItemStack[] drops = base.GetDropsForHandbook(handbookStack, forPlayer);
        drops[0] = drops[0].Clone();
        drops[0].ResolvedItemstack.SetFrom(handbookStack);
        return drops;
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    {
        ItemStack stack = base.OnPickBlock(world, pos).Clone();
        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityDisplayShapeTexturesFromAttributes blockEntiy)
        {
            blockEntiy.Variants.ToStack(stack);
        }
        return stack;
    }

    public override string GetHeldItemName(ItemStack itemStack)
    {
        Variants variants = Variants.FromStack(itemStack);
        variants.FindByVariant(NameByType, out List<object> _langKeys);
        string defaultName = base.GetHeldItemName(itemStack);
        return variants.GetName(_langKeys, defaultName);
    }

    public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
    {
        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityDisplayShapeTexturesFromAttributes blockEntity)
        {
            blockEntity.Variants.FindByVariant(NameByType, out List<object> _langKeys);
            string defaultName = base.GetPlacedBlockName(world, pos);
            return blockEntity.Variants.GetName(_langKeys, defaultName);
        }
        return base.GetPlacedBlockName(world, pos);
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        Variants variants = Variants.FromStack(inSlot.Itemstack);
        variants.FindByVariant(DescriptionByType, out List<object> description);
        variants.GetDescription(dsc, description);
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        return world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityDisplayShapeTexturesFromAttributes blockEntity
            ? blockEntity.OnInteract(byPlayer, blockSel) || base.OnBlockInteractStart(world, byPlayer, blockSel)
            : base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
    {
        return blockAccessor.GetBlockEntity(pos) is BlockEntityDisplayShapeTexturesFromAttributes blockEntity
            ? blockEntity.GetOrCreateSelectionBoxes().Append(blockEntity.GetExtraSelectionBoxes())
            : base.GetSelectionBoxes(blockAccessor, pos);
    }

    public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        return GetOrCreateMesh(Variants.FromStack(itemstack));
    }

    public string GetMeshCacheKey(ItemStack itemstack)
    {
        return $"{itemstack.Collectible.Code}-{Variants.FromStack(itemstack)}";
    }
}
