using System.Collections.Generic;
using TabletopGames.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TabletopGames
{
    class ItemWithAttributes : Item
    {
        public ICoreClientAPI capi;

        public virtual string MeshName => $"tableTopGames_{this}_Meshes";
        public virtual string MeshRefName => $"tableTopGames_{this}_Meshrefs";

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            capi = api as ICoreClientAPI;
        }

        public override bool Equals(ItemStack thisStack, ItemStack otherStack, params string[] ignoreAttributeSubTrees)
        {
            if (ignoreAttributeSubTrees != null)
            {
                ignoreAttributeSubTrees = ignoreAttributeSubTrees.Append("meshRefId", "rotation");
            }

            return base.Equals(thisStack, otherStack, ignoreAttributeSubTrees);
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack stack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            renderinfo.NormalShaded = true;

            if (stack.Collectible is ItemChessPiece)
            {
                if (stack.Attributes.HasAttribute("type"))
                {
                    var currentType = stack.Attributes.GetString("type");
                    var modelTransform = stack.Collectible.Attributes["tabletopgames"]["chesspiece"]["modelTransform"];
                    var guiTransform = modelTransform[currentType]?["guiTransform"].AsObject<ModelTransform>();

                    if (target is EnumItemRenderTarget.Gui)
                    {
                        renderinfo.Transform = guiTransform ?? GuiTransform;
                    }
                }
            }
            if (stack.Collectible is ItemDominoPiece)
            {
                if (stack.Attributes.HasAttribute("rotation"))
                {
                    var currentRotation = stack.Attributes.GetAsInt("rotation");
                    var modelTransform = stack.Collectible.Attributes["tabletopgames"]["dominopiece"]["modelTransformByRotation"];

                    if (target is EnumItemRenderTarget.Gui)
                    {
                        renderinfo.Transform = modelTransform[currentRotation.ToString()]?["guiTransform"].AsObject<ModelTransform>() ?? GuiTransform;
                    }

                    if (target is EnumItemRenderTarget.HandFp)
                    {
                        renderinfo.Transform = modelTransform[currentRotation.ToString()]?["fpHandTransform"].AsObject<ModelTransform>() ?? FpHandTransform;
                    }
                }
            }

            Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.GetOrCreate(capi, MeshRefName, () => new Dictionary<string, MultiTextureMeshRef>());

            var key = GetMeshCacheKey(stack);
            if (!meshRefs.TryGetValue(key, out MultiTextureMeshRef meshRef))
            {
                MeshData mesh = GetOrCreateMesh(stack);
                meshRef = meshRefs[key] = capi.Render.UploadMultiTextureMesh(mesh);
            }
            base.OnBeforeRender(capi, stack, target, ref renderinfo);
        }

        public virtual MeshData GetOrCreateMesh(ItemStack stack)
        {
            Dictionary<string, MeshData> cMeshes = ObjectCacheUtil.GetOrCreate(api, MeshName, () => new Dictionary<string, MeshData>());
            ICoreClientAPI capi = base.api as ICoreClientAPI;
            string key = GetMeshCacheKey(stack);
            if (!cMeshes.TryGetValue(key, out var mesh))
            {
                mesh = new MeshData(4, 3);
                CompositeShape rcshape = Shape.Clone();

                int rotation = stack.Attributes.GetInt("rotation");
                var meshRotationDeg = new Vec3f(0, rotation, 0);

                // Shape shape = capi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();
                Shape shape = api.GetShapeFromAttributes(stack);
                ITexPositionSource texSource = null;
                if (texSource == null)
                {
                    ShapeTextureSource stexSource = new ShapeTextureSource(capi, shape, rcshape.Base.ToString());
                    texSource = stexSource;
                    foreach (KeyValuePair<string, CompositeTexture> val in this.Textures)
                    {
                        CompositeTexture ctex = val.Value.Clone();
                        ctex.Base.Path = stack.GetTexturePath(val).ToString();
                        ctex.Bake(capi.Assets);
                        stexSource.textures[val.Key] = ctex;
                    }
                }
                if (shape == null)
                {
                    return mesh;
                }
                capi.Tesselator.TesselateShape(this + " item", shape, out mesh, texSource, meshRotationDeg);
            }
            return mesh;


            // capi.Tesselator.TesselateItem(this, out MeshData mesh);
            // return mesh;
        }

        public virtual string GetMeshCacheKey(ItemStack stack)
        {
            // int rotation = stack.Attributes.GetInt("rotation");
            // var meshRotationDeg = new Vec3f(0, rotation, 0);
            return Code.ToShortString();
        }
    }
}