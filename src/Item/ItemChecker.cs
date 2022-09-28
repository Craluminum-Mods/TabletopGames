using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using TabletopGames.CheckersUtils;

namespace TabletopGames
{
    class ItemChecker : Item, ITexPositionSource, IContainedMeshSource
    {
        public Size2i AtlasSize => targetAtlas.Size;
        public virtual Dictionary<int, MeshRef> Meshrefs => ObjectCacheUtil.GetOrCreate(api, "tableTopGames_Checker_Meshrefs", () => new Dictionary<int, MeshRef>());
        public TextureAtlasPosition this[string textureCode] => GetOrCreateTexPos(tmpTextures[textureCode]);
        public ICoreClientAPI capi;
        public ITextureAtlasAPI targetAtlas;
        public readonly Dictionary<string, AssetLocation> tmpTextures = new();
        public SkillItem[] skillItems;

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

            skillItems = api.GetCheckersToolModes(this);
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

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            switch (toolMode)
            {
                case 0: slot.Itemstack.ChangeCheckerAttributes(team: "white", crown: false); break;
                case 1: slot.Itemstack.ChangeCheckerAttributes(team: "white", crown: true); break;
                case 2: slot.Itemstack.ChangeCheckerAttributes(team: "black", crown: false); break;
                case 3: slot.Itemstack.ChangeCheckerAttributes(team: "black", crown: true); break;
            }
        }

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

        public override string GetHeldItemName(ItemStack itemStack)
        {
            string team = itemStack.Attributes.GetString("team");
            bool crown = itemStack.Attributes.GetBool("crown");

            return Lang.GetMatching("tabletopgames:item-checker" + (crown ? "-withcrown" : ""), Lang.Get($"color-{team}"));
        }

        public virtual MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
        {
            this.targetAtlas = targetAtlas;
            tmpTextures.Clear();

            string team = itemstack.Attributes.GetString("team");
            bool crown = itemstack.Attributes.GetBool("crown");

            tmpTextures["team"] = tmpTextures["crown"] = new AssetLocation("block/transparent.png"); // Needed to avoid constant crashes
            tmpTextures["team"] = new AssetLocation(Textures[team].Base.Path);
            if (crown) tmpTextures["crown"] = new AssetLocation(Textures[team + "-crown"].Base.Path);

            capi.Tesselator.TesselateItem(this, out var mesh, this);
            return mesh;
        }

        public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos) => GenMesh(itemstack, targetAtlas);

        public virtual string GetMeshCacheKey(ItemStack itemstack)
        {
            string team = itemstack.Attributes.GetString("team");
            bool crown = itemstack.Attributes.GetBool("crown");

            return Code.ToShortString() + "-" + team + "-" + crown;
        }
    }
}