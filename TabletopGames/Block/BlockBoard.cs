using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

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
    public List<string> TabletopTags { get; protected set; }
    public List<string> TabletopTagsIgnored { get; protected set; }
    public List<string> LangKeys { get; protected set; } = new List<string>();

    private CompositeShape cshape;
    private Dictionary<string, CompositeTexture> textures;

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);
        LoadTypes();
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        var meshRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "TabletopGames_boardMeshRefs");
        if (meshRefs?.Count > 0)
        {
            foreach (var (_, meshRef) in meshRefs)
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
            TabletopTags = Attributes["tabletopTags"].AsObject<List<string>>();
            TabletopTagsIgnored = Attributes["tabletopTagsIgnored"].AsObject<List<string>>();
            cshape = Attributes["shape"].AsObject<CompositeShape>();
            textures = Attributes["textures"].AsObject(defaultValue: new Dictionary<string, CompositeTexture>());
            LangKeys = Attributes["langKeys"].AsObject(defaultValue: new List<string>());

            if (Attributes["fillCreativeInventory"].AsBool())
            {
                RegistryObjectVariantGroup[] unresolvedMaterials = Attributes["types"].AsObject(defaultValue: Array.Empty<RegistryObjectVariantGroup>());
                Dictionary<string, List<string>> resolvedMaterials = api.GatherMaterials(unresolvedMaterials);
                this.AddAllTypesToCreativeInventory(api, resolvedMaterials, Constants.ModID);
            }
        }
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

        CompositeShape rcshape = cshape.Clone();
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
            ShapeTextureSource stexSource = new ShapeTextureSource(capi, shape, rcshape.Base.ToString());
            texSource = stexSource;
            foreach (KeyValuePair<string, CompositeTexture> val in textures)
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

        Materials materials = Materials.FromStack(itemstack);
        string key = $"{itemstack.Collectible.Code}-{materials}";

        if (!meshRefs.TryGetValue(key, out MultiTextureMeshRef meshref))
        {
            MeshData mesh = GetOrCreateMesh(materials);
            meshref = capi.Render.UploadMultiTextureMesh(mesh);
            meshRefs[key] = meshref;
        }

        renderinfo.ModelRef = meshref;
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
            stack.Attributes.SetInt("quantitySlots", blockEntiy.QuantitySlots);
            blockEntiy.Materials.ToStack(stack);
        }
        return stack;
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
        Materials.FromStack(inSlot.Itemstack).GetDescription(dsc, LangKeys, withDebugInfo);
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

    public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos)
    {
        //return true;
        return false;
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        return world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityBoard blockEntity
            ? blockEntity.OnInteract(byPlayer, blockSel)
            : base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
    {
        if (blockAccessor.GetBlockEntity(pos) is BlockEntityBoard blockEntity)
        {
            //return base.GetSelectionBoxes(blockAccessor, pos);

            //int size = 15;
            //Cuboidf[] seleBoxes = new Cuboidf[size * size];

            //for (int dx = 0; dx < 8; dx++)
            //{
            //    for (int dz = 0; dz < 8; dz++)
            //    {
            //        seleBoxes[dz * size + dx] = new Cuboidf()
            //        {
            //            X1 = (0.5f + dx) / 16f,
            //            Y1 = 1 / 16f,
            //            Z1 = (0.5f + dz) / 16f,
            //            X2 = (1.5f + dx) / 16f,
            //            Y2 = 2 / 16f,
            //            Z2 = (1.5f + dz) / 16f,
            //        };
            //    }
            //}

            //return seleBoxes;
        //}







        //var boxes = Array.Empty<Cuboidf>();
        //    for (int i = 0; i < blockEntity.Inventory.Count; i++)
        //    {
        //        //if (UsableSlots.Contains<int>(i)) continue;
        //        ItemSlot slot = blockEntity.Inventory[i];
        //        if (slot.Empty) continue;

        //        // Drop contents which can no longer be held if neighbour removed
        //        Vec3d vec = pos.ToVec3d();
        //        vec.Add(0.5 - GameMath.Cos(blockEntity.MeshAngleRad) * 0.6, 0.15, 0.5 + GameMath.Sin(blockEntity.MeshAngleRad) * 0.6);  // Add appropriate offset for the removed side, depending on orientation
        //        api.World.SpawnItemEntity(slot.Itemstack, vec);
        //        //slot.Itemstack = null;
        //    }







            //return new Cuboidf[] { };
        }
        return base.GetSelectionBoxes(blockAccessor, pos);
    }

    public override Vec4f GetSelectionColor(ICoreClientAPI capi, BlockPos pos)
    {
        return new Vec4f(0, 1, 1, 1); // Cyan color
    }
}