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
/// Has inventory, renders shape and textures using attribute based type system.
/// </summary>
public class BlockBoard : Block, IContainedMeshSource
{
    public Dictionary<string, BoardData> BoardDataByType { get; protected set; } = new();
    public Dictionary<string, TabletopTags> TabletopTagsByType { get; protected set; } = new();

    public Dictionary<string, List<object>> NameByType { get; protected set; } = new();
    public Dictionary<string, List<object>> DescriptionByType { get; protected set; } = new();
    public Dictionary<string, Cuboidf[]> ExtraSelectionBoxesByType { get; protected set; } = new();

    protected Dictionary<string, CompositeShape> shapeByType = new();
    protected Dictionary<string, Dictionary<string, CompositeTexture>> texturesByType = new();
    protected Transforms transforms;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        LoadTypes();
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        base.OnUnloaded(api);
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

    public virtual void LoadTypes()
    {
        if (Attributes != null)
        {
            BoardDataByType = Attributes["boardData"].AsObject(defaultValue: new Dictionary<string, BoardData>());
            TabletopTagsByType = Attributes["tabletopTags"].AsObject(defaultValue: new Dictionary<string, TabletopTags>());

            NameByType = Attributes["name"].AsObject(defaultValue: new Dictionary<string, List<object>>());
            DescriptionByType = Attributes["description"].AsObject(defaultValue: new Dictionary<string, List<object>>());
            ExtraSelectionBoxesByType = Attributes["extraSelectionBoxes"].AsObject(defaultValue: new Dictionary<string, Cuboidf[]>());

            shapeByType = Attributes["shape"].AsObject(defaultValue: new Dictionary<string, CompositeShape>());
            texturesByType = Attributes["textures"].AsObject(defaultValue: new Dictionary<string, Dictionary<string, CompositeTexture>>());
            transforms = Attributes["transforms"].AsObject(defaultValue: new Transforms());
        }

        foreach (BoardData boardData in BoardDataByType.Values)
        {
            if (!GuiDialogTransformEditor.extraTransforms.Any(x => x.AttributeName == boardData.AttributeTransformCode))
            {
                GuiDialogTransformEditor.extraTransforms.Add(new TransformConfig()
                {
                    Title = Lang.Get($"{TabletopConstants.ModID}:transform-{boardData.AttributeTransformCode}"),
                    AttributeName = boardData.AttributeTransformCode
                });
            }
        }
    }

    public virtual BoardData GetBoardData(Variants variants)
    {
        return variants.FindByVariant(BoardDataByType, out BoardData value) ? value : new BoardData();
    }

    public virtual TabletopTags GetTags(Variants variants, int slotId, bool resolve = true)
    {
        if (variants.FindByVariant(TabletopTagsByType, out TabletopTags tags))
        {
            return resolve ? tags.GetResolvedTags(slotId) : tags;
        }
        return new TabletopTags();
    }

    public virtual ItemSlot CreateSlot(Variants variants, InventoryBase inventory, int slotId)
    {
        BoardData boardData = GetBoardData(variants);
        TabletopTags tags = GetTags(variants, slotId);
        EnumSlotType slotType = EnumSlotType.Normal;

        if (boardData.SlotTypes.Any())
        {
            string id = slotId.ToString();
            foreach ((string wildcard, EnumSlotType _slotType) in boardData.SlotTypes)
            {
                if (WildcardUtil.Match(wildcard, id))
                {
                    slotType = _slotType;
                    break;
                }
            }
        }
        return new ItemSlotTabletop(inventory, tags, slotType);
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

    public virtual MeshData GetOrCreateMesh(Variants variants, ITexPositionSource overrideTexturesource = null)
    {
        ICoreClientAPI capi = api as ICoreClientAPI;
        MeshData mesh = new MeshData(4, 3);

        variants.FindByVariant(shapeByType, out CompositeShape _shape);
        if (_shape == null)
        {
            return mesh;
        }

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
                if (ctex.BlendedOverlays != null)
                {
                    foreach (BlendedOverlayTexture overlayCtex in ctex.BlendedOverlays)
                    {
                        overlayCtex.Base.Path = variants.ReplacePlaceholders(overlayCtex.Base.Path);
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
        transforms?.TryApplyTransform(target, variants, ref renderinfo.Transform);

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
        ItemStack stack = base.OnPickBlock(world, pos).Clone();
        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityBoard blockEntiy)
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
        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityBoard blockEntity)
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

        Variants variants =  Variants.FromStack(inSlot.Itemstack);
        variants.FindByVariant(DescriptionByType, out List<object> description);
        variants.GetDescription(dsc, description);
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
            blockEntity.Variants.FindByVariant(ExtraSelectionBoxesByType, out Cuboidf[] boxes);
            boxes ??= Array.Empty<Cuboidf>();
            boxes = boxes.Select(x => x.RotatedCopy(0, blockEntity.MeshAngleRad * GameMath.RAD2DEG, 0, new Vec3d(0.5, 0.5, 0.5))).ToArray();
            return blockEntity.GetOrCreateSelectionBoxes().Append(boxes);
        }
        return base.GetSelectionBoxes(blockAccessor, pos);
    }

    public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) => TabletopDebug.BoardParticleSelection;
    public override Vec4f GetSelectionColor(ICoreClientAPI capi, BlockPos pos) => TabletopDebug.BoardSelectionColor;

    public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        Variants variants = Variants.FromStack(itemstack);
        return GetOrCreateMesh(variants);
    }

    public string GetMeshCacheKey(ItemStack itemstack)
    {
        Variants variants = Variants.FromStack(itemstack);
        return $"{itemstack.Collectible.Code}-{variants}";
    }
}