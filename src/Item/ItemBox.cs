using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using TabletopGames.Utils;
using System.Text;

namespace TabletopGames
{
    class ItemBox : ItemWithAttributes
    {
        public override string MeshRefName => base.MeshRefName + Code.ToShortString();

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            var board = inSlot.Itemstack.Attributes.GetItemstack("containedStack");

            if (board.ResolveBlockOrItem(api.World))
            {
                dsc.AppendWoodText(board);

                if (board != null) dsc.AppendLine(Lang.Get("Contents: {0}", board.GetName()));
                else dsc.AppendLine(Lang.Get("Empty"));
            }
        }

        public override string GetHeldItemName(ItemStack stack)
        {
            var containedStack = stack.Attributes.GetItemstack("containedStack");

            if (containedStack.ResolveBlockOrItem(api.World) && containedStack != null)
            {
                return Lang.Get("tabletopgames:Packed", containedStack?.GetName());
            }

            return base.GetHeldItemName(stack);
        }

        public override MeshData GenMesh(ItemStack stack, ITextureAtlasAPI targetAtlas)
        {
            this.targetAtlas = targetAtlas;
            tmpTextures.Clear();

            foreach (var key in Textures)
            {
                tmpTextures[key.Key] = new AssetLocation("block/transparent.png"); // Needed to avoid constant crashes
                tmpTextures[key.Key] = stack.GetTexturePath(key);
            }

            capi.Tesselator.TesselateItem(this, out var mesh, this);
            return mesh;
        }

        public override string GetMeshCacheKey(ItemStack stack)
        {
            string wood = stack.Attributes.GetString("wood", defaultValue: "oak");
            return Code.ToShortString() + "-" + wood;
        }
    }
}