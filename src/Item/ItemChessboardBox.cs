using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using TabletopGames.BoxUtils;
using System.Text;

namespace TabletopGames
{
    class ItemChessboardBox : ItemWithAttributesTemplate
    {
        public override Dictionary<int, MeshRef> Meshrefs => ObjectCacheUtil.GetOrCreate(api, "tableTopGames_ChessboardBox_Meshrefs", () => new Dictionary<int, MeshRef>());

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);
            capi = api as ICoreClientAPI;
            skillItems = capi.GetBoxToolModes("unpack");
        }

        public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int toolMode)
        {
            switch (toolMode)
            {
                case 0:
                    {
                        if (slot == null || slot.Itemstack == null || slot.Itemstack.Attributes == null) break;

                        var boardStack = slot.Itemstack.Attributes.GetItemstack("chessboard");

                        if (boardStack?.ResolveBlockOrItem(api.World) != true) break;

                        slot.Itemstack.SetFrom(boardStack);
                        slot.MarkDirty();
                        break;
                    }
            }
        }

        public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
        {
            base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

            var board = inSlot?.Itemstack?.Attributes?.GetItemstack("chessboard");

            if (board != null) dsc.AppendLine(Lang.Get("Contents: {0}", board.GetName()));
            else dsc.AppendLine(Lang.Get("Empty"));
        }

        public override MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas)
        {
            this.targetAtlas = targetAtlas;
            tmpTextures.Clear();

            tmpTextures["black"] = tmpTextures["board"] = tmpTextures["wood"] = new AssetLocation("block/transparent.png"); // Needed to avoid constant crashes
            tmpTextures["black"] = new AssetLocation(Textures["black"].Base.Path);
            tmpTextures["board"] = new AssetLocation("tabletopgames:textures/block/checkerboard-8x8");
            tmpTextures["wood"] = new AssetLocation(Textures["wood"].Base.Path);

            capi.Tesselator.TesselateItem(this, out var mesh, this);
            return mesh;
        }

        public override string GetMeshCacheKey(ItemStack itemstack) => Code.ToShortString();
    }
}