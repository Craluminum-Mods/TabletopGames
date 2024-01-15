using System.Text;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using TabletopGames.Utils;
using Vintagestory.GameContent;

namespace TabletopGames
{
    public class BlockWithAttributes : Block
    {
        public ICoreClientAPI capi;

        public virtual bool HasWoodType => false;
        public virtual bool HasCheckerboardTypes => false;
        public virtual bool CanBePickedUp => false;
        public virtual string MeshRefName => $"tableTopGames_{this}_Meshrefs";
        public virtual string MeshName => $"tableTopGames_{this}_Meshes";

        public override void OnBeforeRender(ICoreClientAPI capi, ItemStack stack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
        {
            base.OnBeforeRender(capi, stack, target, ref renderinfo);
            Dictionary<string, MultiTextureMeshRef> Meshrefs = ObjectCacheUtil.GetOrCreate(api, MeshRefName, () => new Dictionary<string, MultiTextureMeshRef>());

            renderinfo.NormalShaded = true;

            var key = GetMeshCacheKey(stack);
            if (!Meshrefs.TryGetValue(key, out MultiTextureMeshRef meshRef))
            {
                MeshData mesh = GetOrCreateMesh(stack);
                meshRef = Meshrefs[key] = capi.Render.UploadMultiTextureMesh(mesh);
            }
        }

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            capi = api as ICoreClientAPI;
        }

        public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) => true;

        public override Vec4f GetSelectionColor(ICoreClientAPI capi, BlockPos pos) => new(1, 1, 0, 1); // Yellow

        public ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos, InventoryBase inventory, string woodType, int quantitySlots = 0, bool isInvSizeDynamic = false, string darkType = "", string lightType = "", bool saveInventory = false)
        {
            var blockStack = base.OnPickBlock(world, pos);

            if (isInvSizeDynamic && quantitySlots != 0) blockStack.Attributes.SetInt("quantitySlots", quantitySlots);
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

        public virtual MeshData GetOrCreateMesh(ItemStack stack)
        {
            Dictionary<string, MeshData> cMeshes = ObjectCacheUtil.GetOrCreate(api, MeshName, () => new Dictionary<string, MeshData>());
            ICoreClientAPI capi = base.api as ICoreClientAPI;
            string key = GetMeshCacheKey(stack);
            if (!cMeshes.TryGetValue(key, out var mesh))
            {
                mesh = new MeshData(4, 3);
                CompositeShape rcshape = Shape.Clone();

                Shape shape = capi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();
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
                capi.Tesselator.TesselateShape(this + " block", shape, out mesh, texSource, null, 0, 0, 0);
            }
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