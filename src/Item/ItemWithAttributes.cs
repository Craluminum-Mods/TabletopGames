using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TabletopGames
{
    class ItemWithAttributes : Item, ITexPositionSource, IContainedMeshSource
    {
        public Size2i AtlasSize => targetAtlas.Size;
        public Dictionary<int, MeshRef> Meshrefs => ObjectCacheUtil.GetOrCreate(api, MeshRefName, () => new Dictionary<int, MeshRef>());
        public TextureAtlasPosition this[string textureCode] => GetOrCreateTexPos(tmpTextures[textureCode]);
        public ICoreClientAPI capi;
        public ITextureAtlasAPI targetAtlas;
        public readonly Dictionary<string, AssetLocation> tmpTextures = new();

        public int CurrentMeshRefid => Clone().GetHashCode();

        public virtual string MeshRefName => $"tableTopGames_{this}_Meshrefs";

        protected TextureAtlasPosition GetOrCreateTexPos(AssetLocation texturePath)
        {
            var texAsset = capi.Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
            var texPos = targetAtlas[texturePath];

            if (texPos != null) return texPos;
            if (texAsset != null) targetAtlas.GetOrInsertTexture(texturePath, out var _, out texPos, () => texAsset.ToBitmap(capi));

            return texPos;
        }

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

            var meshrefid = stack.Attributes.GetInt("meshRefId");
            if (meshrefid == CurrentMeshRefid || !Meshrefs.TryGetValue(meshrefid, out renderinfo.ModelRef))
            {
                var num = Meshrefs.Count + 1;
                var value = capi.Render.UploadMesh(GenMesh(stack, capi.ItemTextureAtlas));
                renderinfo.ModelRef = Meshrefs[num] = value;
                stack.Attributes.SetInt("meshRefId", num);
            }
            base.OnBeforeRender(capi, stack, target, ref renderinfo);
        }

        public virtual MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
        {
            capi.Tesselator.TesselateItem(this, out var mesh, this);
            return mesh;
        }

        public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos) => GenMesh(itemstack, targetAtlas);
        public virtual string GetMeshCacheKey(ItemStack itemstack) => Code.ToShortString();
    }
}