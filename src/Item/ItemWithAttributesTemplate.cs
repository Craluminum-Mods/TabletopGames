using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TabletopGames
{
    class ItemWithAttributesTemplate : Item, ITexPositionSource, IContainedMeshSource
    {
        public Size2i AtlasSize => targetAtlas.Size;
        public Dictionary<int, MeshRef> Meshrefs => ObjectCacheUtil.GetOrCreate(api, MeshRefName, () => new Dictionary<int, MeshRef>());
        public TextureAtlasPosition this[string textureCode] => GetOrCreateTexPos(tmpTextures[textureCode]);
        public ICoreClientAPI capi;
        public ITextureAtlasAPI targetAtlas;
        public readonly Dictionary<string, AssetLocation> tmpTextures = new();
        public SkillItem[] skillItems;

        public virtual string MeshRefName => "tableTopGames_ItemWithAttributes_Meshrefs";

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

        public override void OnUnloaded(ICoreAPI api)
        {
            for (int i = 0; skillItems != null && i < skillItems.Length; i++)
            {
                skillItems[i]?.Dispose();
            }
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return base.GetHeldInteractionHelp(inSlot).Append(new WorldInteraction
            {
                ActionLangCode = "heldhelp-settoolmode",
                HotKeyCode = "toolmodeselect",
                MouseButton = EnumMouseButton.None
            });
        }

        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel) => skillItems;

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            var meshrefid = itemstack.TempAttributes.GetInt("meshRefId", 0);
            if (meshrefid == 0 || !Meshrefs.TryGetValue(meshrefid, out renderinfo.ModelRef))
            {
                var num = Meshrefs.Count + 1;
                var value = capi.Render.UploadMesh(GenMesh(itemstack, capi.ItemTextureAtlas));
                renderinfo.ModelRef = Meshrefs[num] = value;
                itemstack.TempAttributes.SetInt("meshRefId", num);
            }
            base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
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