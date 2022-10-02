using System.Text;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using TabletopGames.ModUtils;

namespace TabletopGames
{
    public class BlockWithAttributes : Block, ITexPositionSource, IContainedMeshSource
    {
        public string woodTexPrefix;
        public SkillItem[] skillItems;

        public ICoreClientAPI capi;

        public ITextureAtlasAPI targetAtlas;
        public Size2i AtlasSize => targetAtlas.Size;
        public Dictionary<int, MeshRef> Meshrefs => ObjectCacheUtil.GetOrCreate(api, MeshRefName, () => new Dictionary<int, MeshRef>());
        public TextureAtlasPosition this[string textureCode] => GetOrCreateTexPos(tmpTextures[textureCode]);
        public readonly Dictionary<string, AssetLocation> tmpTextures = new();

        public string GetTextureLocationPrefix(string key) => Attributes["texturePrefixes"][key].AsString();

        public virtual bool SaveInventory => false;
        public virtual bool HasWoodType => false;
        public virtual bool CanBePickedUp => false;
        public virtual string MeshRefName => "tableTopGames_BlockWithAttributes_Meshrefs";

        protected TextureAtlasPosition GetOrCreateTexPos(AssetLocation texturePath)
        {
            var texAsset = capi.Assets.TryGet(texturePath.Clone().WithPathPrefixOnce("textures/").WithPathAppendixOnce(".png"));
            var texPos = targetAtlas[texturePath];

            if (texPos != null) return texPos;
            if (texAsset != null) targetAtlas.GetOrInsertTexture(texturePath, out var _, out texPos, () => texAsset.ToBitmap(capi));

            return texPos;
        }

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            var meshrefid = itemstack.TempAttributes.GetInt("meshRefId", 0);
            if (meshrefid == 0 || !Meshrefs.TryGetValue(meshrefid, out renderinfo.ModelRef))
            {
                var num = Meshrefs.Count + 1;
                var value = capi.Render.UploadMesh(GenMesh(itemstack, capi.BlockTextureAtlas, null));
                renderinfo.ModelRef = Meshrefs[num] = value;
                itemstack.TempAttributes.SetInt("meshRefId", num);
            }
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            capi = api as ICoreClientAPI;

            woodTexPrefix = GetTextureLocationPrefix("wood");
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
            if (skillItems == null) return base.GetHeldInteractionHelp(inSlot);

            return base.GetHeldInteractionHelp(inSlot).Append(new WorldInteraction
                {
                    ActionLangCode = "heldhelp-settoolmode",
                    HotKeyCode = "toolmodeselect",
                    MouseButton = EnumMouseButton.None
                });
        }

        public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel) => skillItems;

        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) => true;

        public override Vec4f GetSelectionColor(ICoreClientAPI capi, BlockPos pos) => new(1, 1, 0, 1); // Yellow

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            if (!CanBePickedUp) return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer);

            return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer).Append(new WorldInteraction
            {
                ActionLangCode = "blockhelp-behavior-rightclickpickup",
                HotKeyCodes = new string[] { "shift", "ctrl" },
                MouseButton = EnumMouseButton.Right
            });
        }

        public ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, InventoryBase inventory, string woodType)
        {
            ItemStack blockStack;
            if (Variant?["side"] != null) blockStack = new ItemStack(world.GetBlock(CodeWithVariant("side", "east")));
            else blockStack = new ItemStack(this);

            if (SaveInventory) blockStack.SaveInventoryToItemstack(inventory);
            if (HasWoodType) blockStack.Attributes.SetString("wood", woodType);

            return blockStack;
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            return new ItemStack[] { OnPickBlock(world, pos) };
        }

        public override BlockDropItemStack[] GetDropsForHandbook(ItemStack handbookStack, IPlayer forPlayer)
        {
            return new BlockDropItemStack[] { new BlockDropItemStack(handbookStack) };
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);
            dsc.AppendWoodDescription(inSlot.Itemstack);
        }

        public virtual MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
        {
            this.targetAtlas = targetAtlas;
            tmpTextures.Clear();

            foreach (var key in Textures)
            {
                tmpTextures[key.Key] = new AssetLocation("block/transparent.png"); // Needed to avoid constant crashes
                tmpTextures[key.Key] = new AssetLocation(this.TryGetWoodTexturePath(key, woodTexPrefix, itemstack));
            }

            var shape = Vintagestory.API.Common.Shape.TryGet(api, this.GetShapePath());

            capi.Tesselator.TesselateShape("", shape, out var mesh, this);
            return mesh;
        }

        public virtual string GetMeshCacheKey(ItemStack itemstack)
        {
            string wood = itemstack.Attributes.GetString("wood", defaultValue: "oak");
            if (wood != null) return Code.ToShortString() + "-" + wood;
            else return Code.ToShortString();
        }
    }
}