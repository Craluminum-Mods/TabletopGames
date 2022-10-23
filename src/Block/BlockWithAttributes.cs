using System.Text;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using TabletopGames.Utils;

namespace TabletopGames
{
    public class BlockWithAttributes : Block, ITexPositionSource, IContainedMeshSource
    {
        public SkillItem[] skillItems;

        public ICoreClientAPI capi;

        public ITextureAtlasAPI targetAtlas;
        public Size2i AtlasSize => targetAtlas.Size;
        public Dictionary<int, MeshRef> Meshrefs => ObjectCacheUtil.GetOrCreate(api, MeshRefName, () => new Dictionary<int, MeshRef>());
        public TextureAtlasPosition this[string textureCode] => GetOrCreateTexPos(tmpTextures[textureCode]);
        public readonly Dictionary<string, AssetLocation> tmpTextures = new();

        public virtual bool SaveInventory => false;
        public virtual bool HasWoodType => false;
        public virtual bool HasCheckerboardTypes => false;
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

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack stack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            var meshrefid = stack.TempAttributes.GetInt("meshRefId", 0);
            if (meshrefid == 0 || !Meshrefs.TryGetValue(meshrefid, out renderinfo.ModelRef))
            {
                var num = Meshrefs.Count + 1;
                var value = capi.Render.UploadMesh(GenMesh(stack, capi.BlockTextureAtlas, null));
                renderinfo.ModelRef = Meshrefs[num] = value;
                stack.TempAttributes.SetInt("meshRefId", num);
            }
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

        public ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, InventoryBase inventory, string woodType, int quantitySlots = 0, bool isInvSizeDynamic = false, string darkType = "", string lightType = "")
        {
            ItemStack blockStack;
            if (Variant?["side"] != null) blockStack = new ItemStack(world.GetBlock(CodeWithVariant("side", "east")));
            else blockStack = new ItemStack(this);

            if (isInvSizeDynamic && quantitySlots != 0) blockStack.Attributes.SetInt("quantitySlots", quantitySlots);
            if (SaveInventory) blockStack.TransferInventory(inventory);
            if (HasWoodType) blockStack.Attributes.SetString("wood", woodType);
            if (HasCheckerboardTypes) blockStack.Attributes.SetString("dark", darkType);
            if (HasCheckerboardTypes) blockStack.Attributes.SetString("light", lightType);

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
            dsc.AppendWoodText(inSlot.Itemstack);
            dsc.AppendInventorySlotsText(inSlot.Itemstack);
        }

        public virtual MeshData GenMesh(ItemStack stack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
        {
            this.targetAtlas = targetAtlas ?? capi.BlockTextureAtlas;
            tmpTextures.Clear();

            foreach (var key in Textures)
            {
                tmpTextures[key.Key] = new AssetLocation("block/transparent.png"); // Needed to avoid constant crashes
                tmpTextures[key.Key] = stack.TryGetTexturePath(key);
            }

            var shape = Vintagestory.API.Common.Shape.TryGet(api, this.GetShapePath());

            capi.Tesselator.TesselateShape("", shape, out var mesh, this);
            return mesh;
        }

        public virtual string GetMeshCacheKey(ItemStack stack)
        {
            string wood = stack.Attributes.GetString("wood", defaultValue: "oak");
            if (wood != null) return Code.ToShortString() + "-" + wood;
            else return Code.ToShortString();
        }

        // Move to BlockBehavior
        public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
        {
            var slotsTree = byItemStack.Attributes?.GetTreeAttribute("box")?.GetTreeAttribute("slots");
            var quantitySlots = byItemStack.Attributes?.GetAsInt("quantitySlots");

            if (slotsTree == null || !byItemStack.Attributes.HasAttribute("quantitySlots")) return base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);

            if (quantitySlots < slotsTree.Count)
            {
                byItemStack.TryDropAllSlots(byPlayer, world.Api);
            }

            return base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
        }
    }
}