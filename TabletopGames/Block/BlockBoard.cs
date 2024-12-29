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
/// <para> Renders shape and textures using attribute based type system. </para>
/// <para> Used for boards. </para>
/// <para> Has rotation. </para>
/// <para> Has "automatic" localization. </para>
/// <para> Has inventory and displays stored items. </para>
/// </summary>
public class BlockBoard : Block, IContainedMeshSource
{
    public Dictionary<string, BoardData> BoardDataByType { get; protected set; } = new();

    public List<string> TabletopTags { get; protected set; } = new();
    public List<string> TabletopTagsIgnored { get; protected set; } = new();
    public Dictionary<string, List<string>> NameByType { get; protected set; } = new();
    public Dictionary<string, List<string>> DescriptionByType { get; protected set; } = new();
    public Dictionary<string, Cuboidf[]> ExtraSelectionBoxesByType { get; protected set; } = new();

    private Dictionary<string, CompositeShape> shapeByType = new();
    private Dictionary<string, Dictionary<string, CompositeTexture>> texturesByType = new();

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        LoadTypes();
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "TabletopGames_boardMeshRefs");
        if (meshRefs?.Count > 0)
        {
            foreach ((string _, MultiTextureMeshRef meshRef) in meshRefs)
            {
                meshRef.Dispose();
            }
            ObjectCacheUtil.Delete(api, "TabletopGames_boardMeshRefs");
        }
    }

    public void LoadTypes()
    {
        if (Attributes != null)
        {
            BoardDataByType = Attributes["boardData"].AsObject<Dictionary<string, BoardData>>();

            TabletopTags = Attributes["tabletopTags"].AsObject<List<string>>();
            TabletopTagsIgnored = Attributes["tabletopTagsIgnored"].AsObject<List<string>>();

            shapeByType = Attributes["shape"].AsObject(defaultValue: new Dictionary<string, CompositeShape>());
            texturesByType = Attributes["textures"].AsObject(defaultValue: new Dictionary<string, Dictionary<string, CompositeTexture>>());
            NameByType = Attributes["name"].AsObject(defaultValue: new Dictionary<string, List<string>>());
            DescriptionByType = Attributes["description"].AsObject(defaultValue: new Dictionary<string, List<string>>());
            ExtraSelectionBoxesByType = Attributes["extraSelectionBoxes"].AsObject(defaultValue: new Dictionary<string, Cuboidf[]>());
        }

        foreach ((string _, BoardData boardData) in BoardDataByType)
        {
            if (!GuiDialogTransformEditor.extraTransforms.Any(x => x.AttributeName == boardData.AttributeTransformCode))
            {
                GuiDialogTransformEditor.extraTransforms.Add(new TransformConfig() { Title = Lang.Get(boardData.AttributeTransformCode), AttributeName = boardData.AttributeTransformCode });
            }
        }
    }

    public BoardData GetBoardData(Materials materials)
    {
        return materials.FindByMaterial(BoardDataByType, out BoardData value) ? value : new BoardData();
    }

    public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
    {
        bool ok = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
        if (ok && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityBoard blockEntiy)
        {
            RotateBy90();
            blockEntiy.OnBlockPlaced(byItemStack);
        }
        return ok;

        void RotateBy90()
        {
            BlockPos targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
            double dx = byPlayer.Entity.Pos.X - (targetPos.X + blockSel.HitPosition.X);
            double dz = (float)byPlayer.Entity.Pos.Z - (targetPos.Z + blockSel.HitPosition.Z);
            float angleHor = (float)Math.Atan2(dx, dz);

            float intervalRad = GameMath.PIHALF;
            float roundRad = (int)Math.Round(angleHor / intervalRad) * intervalRad;
            blockEntiy.MeshAngleRad = roundRad;
        }
    }

    public MeshData GetOrCreateMesh(Materials materials, ITexPositionSource overrideTexturesource = null)
    {
        ICoreClientAPI capi = api as ICoreClientAPI;
        MeshData mesh = new MeshData(4, 3);

        materials.FindByMaterial(shapeByType, out CompositeShape _shape);
        if (_shape == null)
        {
            return mesh;
        }

        CompositeShape rcshape = _shape.Clone();
        rcshape.Base.Path = materials.ReplacePlaceholders(rcshape.Base.Path);
        rcshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");

        Shape shape = capi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();

        ITexPositionSource texSource = null;
        if (overrideTexturesource != null)
        {
            texSource = overrideTexturesource;
        }
        if (texSource == null)
        {
            materials.FindByMaterial(texturesByType, out Dictionary<string, CompositeTexture> _textures);
            _textures ??= new Dictionary<string, CompositeTexture>();

            ShapeTextureSource stexSource = new ShapeTextureSource(capi, shape, rcshape.Base.ToString());
            texSource = stexSource;
            foreach (KeyValuePair<string, CompositeTexture> val in _textures)
            {
                CompositeTexture ctex = val.Value.Clone();
                ctex.Base.Path = materials.ReplacePlaceholders(ctex.Base.Path);
                if (ctex.BlendedOverlays != null)
                {
                    foreach (BlendedOverlayTexture overlayCtex in ctex.BlendedOverlays)
                    {
                        overlayCtex.Base.Path = materials.ReplacePlaceholders(overlayCtex.Base.Path);
                    }
                }
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
        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityBoard blockEntiy)
        {
            float[] mat = Matrixf.Create().Translate(0.5f, 0.5f, 0.5f).RotateY(blockEntiy.MeshAngleRad).Translate(-0.5f, -0.5f, -0.5f).Values;
            MeshData decalMesh = GetOrCreateMesh(blockEntiy.Materials, overrideTexturesource: decalTexSource).Clone().MatrixTransform(mat);
            MeshData blockMesh = GetOrCreateMesh(blockEntiy.Materials).Clone().MatrixTransform(mat);
            decalModelData = decalMesh;
            blockModelData = blockMesh;
            return;
        }

        base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.GetOrCreate(capi, "TabletopGames_boardMeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());

        string key = GetMeshCacheKey(itemstack);
        if (!meshRefs.TryGetValue(key, out MultiTextureMeshRef meshref))
        {
            Materials materials = Materials.FromStack(itemstack);
            MeshData mesh = GetOrCreateMesh(materials);
            meshref = capi.Render.UploadMultiTextureMesh(mesh);
            meshRefs[key] = meshref;
        }

        renderinfo.ModelRef = meshref;
        renderinfo.NormalShaded = true;
        base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
    {
        return world.BlockAccessor.GetBlockEntity(pos) is BlockEntityBoard blockEntiy
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
        ItemStack stack = base.OnPickBlock(world, pos);
        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityBoard blockEntiy)
        {
            blockEntiy.Materials.ToStack(stack);
        }
        return stack;
    }

    public override string GetHeldItemName(ItemStack itemStack)
    {
        Materials materials =  Materials.FromStack(itemStack);
        materials.FindByMaterial(NameByType, out List<string> name);
        return (name?.Any() ?? false)
            ? string.Join("", name.Select(x => Lang.Get(materials.ReplacePlaceholders(x))))
            : base.GetHeldItemName(itemStack);
    }

    public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
    {
        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityBoard blockEntity)
        {
            blockEntity.Materials.FindByMaterial(NameByType, out List<string> name);
            return (name?.Any() ?? false)
                ? string.Join("",name.Select(x => Lang.Get(blockEntity.Materials.ReplacePlaceholders(x))))
                : base.GetPlacedBlockName(world, pos);
        }
        return base.GetPlacedBlockName(world, pos);
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        Materials materials =  Materials.FromStack(inSlot.Itemstack);
        materials.FindByMaterial(DescriptionByType, out List<string> description);
        description ??= new List<string>();
        materials.GetDescription(dsc, description);
    }

    public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        Materials materials = Materials.FromStack(itemstack);
        return GetOrCreateMesh(materials);
    }

    public string GetMeshCacheKey(ItemStack itemstack)
    {
        Materials materials = Materials.FromStack(itemstack);
        return $"{itemstack.Collectible.Code}-{materials}";
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        return world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityBoard blockEntity
            ? blockEntity.OnInteract(byPlayer, blockSel) || base.OnBlockInteractStart(world, byPlayer, blockSel)
            : base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
    {
        if (blockAccessor.GetBlockEntity(pos) is BlockEntityBoard blockEntity)
        {
            blockEntity.Materials.FindByMaterial(ExtraSelectionBoxesByType, out Cuboidf[] boxes);
            boxes ??= Array.Empty<Cuboidf>();
            return blockEntity.GetOrCreateSelectionBoxes().Append(boxes);
        }
        return base.GetSelectionBoxes(blockAccessor, pos);
    }
    
    public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) => TabletopDebug.BoardParticleSelection;
    public override Vec4f GetSelectionColor(ICoreClientAPI capi, BlockPos pos) => TabletopDebug.BoardSelectionColor;
}