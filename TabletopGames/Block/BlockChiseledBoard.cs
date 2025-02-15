using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;

namespace TabletopGames;

public class BlockChiseledBoard : Block, IContainedMeshSource
{
    public enum EnumMode
    {
        HitBoxes = 0,
        Textures = 1
    }

    public enum EnumStackType
    {
        HitBoxes = 0,
        Textures = 1
    }

    public const string ChiseledStackHitboxesAttributeName = "chiseledStackHitboxes";
    public const string ChiseledStackTexturesAttributeName = "chiseledStackTextures";

    public string AttributeTransformCode { get; protected set; }

    private SkillItem[] toolModes = Array.Empty<SkillItem>();

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        if (Attributes != null)
        {
            AttributeTransformCode = Attributes["attributeTransformCode"].AsString();
        }

        if (!GuiDialogTransformEditor.extraTransforms.Any(x => x.AttributeName == AttributeTransformCode))
        {
            GuiDialogTransformEditor.extraTransforms.Add(new TransformConfig()
            {
                Title = Lang.Get($"{TabletopConstants.ModID}:transform-{AttributeTransformCode}"),
                AttributeName = AttributeTransformCode
            });
        }

        toolModes = new SkillItem[]
        {
            new() { Name = Lang.Get("tabletopgames:toolmode-sinkslot-chiseled-block-set-hitboxes") },
            new() { Name = Lang.Get("tabletopgames:toolmode-sinkslot-chiseled-block-set-textures") },
        };

        if (api is not ICoreClientAPI capi)
        {
            return;
        }

        for (int i = 0; i < toolModes.Length; i++)
        {
            SkillItem toolMode = toolModes[i];
            switch ((EnumMode)i)
            {
                case EnumMode.HitBoxes:
                    toolMode.WithIcon(capi, capi.Gui.LoadSvgWithPadding("tabletopgames:textures/icons/exchange.svg", 48, 48, 5, color: ColorUtil.Hex2Int("#aec6cf")));
                    toolMode.TexturePremultipliedAlpha = false;
                    break;
                case EnumMode.Textures:
                    toolMode.WithIcon(capi, capi.Gui.LoadSvgWithPadding("tabletopgames:textures/icons/exchange.svg", 48, 48, 5, color: ColorUtil.Hex2Int("#b39eb5")));
                    toolMode.TexturePremultipliedAlpha = false;
                    break;
            }
            toolModes[i] = toolMode;
        }
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        base.OnUnloaded(api);

        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.TryGet<Dictionary<string, MultiTextureMeshRef>>(api, "TabletopGames_BlockChiseledBoard_MeshRefs");
        meshRefs?.Foreach(meshRef => meshRef.Value?.Dispose());
        ObjectCacheUtil.Delete(api, "TabletopGames_BlockChiseledBoard_MeshRefs");

        Dictionary<string, MeshData> meshes = ObjectCacheUtil.TryGet<Dictionary<string, MeshData>>(api, "TabletopGames_BlockChiseledBoard_Meshes");
        meshRefs?.Foreach(mesh => mesh.Value?.Dispose());
        ObjectCacheUtil.Delete(api, "TabletopGames_BlockChiseledBoard_Meshes");
    }

    public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
    {
        base.OnHeldIdle(slot, byEntity);
        SelfDestroyIfEmpty(slot, api.World);
    }

    public override void OnGroundIdle(EntityItem entityItem)
    {
        base.OnGroundIdle(entityItem);
        SelfDestroyIfEmpty(entityItem?.Slot, api.World);
    }

    public override string GetHeldItemName(ItemStack itemStack)
    {
        string name = GetChiseledStack(itemStack, api.World, EnumStackType.HitBoxes)?.Attributes.GetString("blockName");
        return !string.IsNullOrEmpty(name) ? name : base.GetHeldItemName(itemStack);
    }

    public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
    {
        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityChiseledBoard blockEntity && blockEntity.ChiseledStackHitboxes != null)
        { 
            string name = blockEntity.ChiseledStackHitboxes?.Attributes.GetString("blockName");
            return !string.IsNullOrEmpty(name) ? name : base.GetPlacedBlockName(world, pos);
        }
        return base.GetPlacedBlockName(world, pos);
    }

    public override string GetItemDescText() => Lang.Get("tabletopgames:blockdesc-chiseledboard-held") + "\n";

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        ItemStack chiseledStackHitboxes = GetChiseledStack(inSlot.Itemstack, api.World, EnumStackType.HitBoxes);
        ItemStack chiseledStackTextures = GetChiseledStack(inSlot.Itemstack, api.World, EnumStackType.Textures);

        dsc.AppendLine(chiseledStackHitboxes == null ? Lang.Get("tabletopgames:missing-chiseled-block-for-hitboxes") : Lang.Get("tabletopgames:contains-chiseled-block-for-hitboxes"));
        dsc.AppendLine(chiseledStackTextures == null ? Lang.Get("tabletopgames:missing-chiseled-block-for-textures") : Lang.Get("tabletopgames:contains-chiseled-block-for-textures"));
    }

    public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int index)
    {
        if (slot.Empty || toolModes?.Length <= index) return;

        ItemStack stackHitboxes = GetChiseledStack(slot.Itemstack, api.World, EnumStackType.HitBoxes)?.Clone();
        ItemStack stackTextures = GetChiseledStack(slot.Itemstack, api.World, EnumStackType.Textures)?.Clone();

        if (stackHitboxes == null && stackTextures == null) return;

        ItemSlot mouseslot = byPlayer.InventoryManager.MouseItemSlot;
        ItemStack giveStack = null;
        bool keepOpen = true;

        switch ((EnumMode)index)
        {
            case EnumMode.HitBoxes:
                {
                    if (mouseslot.Empty)
                    {
                        if (stackTextures == null)
                        {
                            giveStack = GetChiseledStack(slot.Itemstack, api.World, EnumStackType.HitBoxes, removeAttribute: true);
                            giveStack.StackSize = slot.StackSize;
                            keepOpen = false;
                        }
                        break;
                    }

                    if (TriggerErrorOnNotChiseledBlock(mouseslot)) break;
                    if (TriggerErrorOnStackSizeMismatch(slot.StackSize, mouseslot.StackSize)) break;

                    ItemStack removedStack = stackHitboxes?.Clone();
                    if (removedStack != null)
                    {
                        removedStack.StackSize = slot.StackSize;
                    }

                    ItemStack clonedMouseStack = mouseslot.Itemstack.Clone();
                    clonedMouseStack.StackSize = 1;

                    if (removedStack == null)
                    {
                        mouseslot.TakeOutWhole();
                    }
                    else
                    {
                        mouseslot.Itemstack.SetFrom(removedStack);
                    }

                    SetChiseledStack(slot.Itemstack, inputStack: clonedMouseStack, EnumStackType.HitBoxes);
                }
                break;
            case EnumMode.Textures:
                {
                    if (mouseslot.Empty)
                    {
                        if (stackTextures != null)
                        {
                            giveStack = GetChiseledStack(slot.Itemstack, api.World, EnumStackType.Textures, removeAttribute: true);
                            giveStack.StackSize = slot.StackSize;
                            keepOpen = false;
                        }
                        break;
                    }

                    if (TriggerErrorOnNotChiseledBlock(mouseslot)) break;
                    if (TriggerErrorOnStackSizeMismatch(slot.StackSize, mouseslot.StackSize)) break;

                    ItemStack removedStack = stackTextures?.Clone();
                    if (removedStack != null)
                    {
                        removedStack.StackSize = slot.StackSize;
                    }

                    ItemStack clonedMouseStack = mouseslot.Itemstack.Clone();
                    clonedMouseStack.StackSize = 1;

                    if (removedStack == null)
                    {
                        mouseslot.TakeOutWhole();
                    }
                    else
                    {
                        mouseslot.Itemstack.SetFrom(removedStack);
                    }

                    SetChiseledStack(slot.Itemstack, inputStack: clonedMouseStack, EnumStackType.Textures);
                }
                break;
        }

        slot.MarkDirty();
        mouseslot.MarkDirty();
        byPlayer.InventoryManager.BroadcastHotbarSlot();

        if (keepOpen)
        {
            byPlayer.Entity.World.Api.Event.PushEvent("keepopentoolmodedlg");
        }
        if (giveStack != null && !byPlayer.InventoryManager.TryGiveItemstack(giveStack))
        {
            byPlayer.Entity.World.SpawnItemEntity(giveStack, byPlayer.Entity.SidedPos.AsBlockPos);
        }
    }

    public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel) => toolModes;

    private bool TriggerErrorOnStackSizeMismatch(int firstStackSize, int secondStackSize)
    {
        bool trigger = firstStackSize != secondStackSize;
        if (trigger)
        {
            (api as ICoreClientAPI)?.TriggerIngameError(this, "tabletopgames:ingameerror-stacksize-mismatch", Lang.Get("tabletopgames:ingameerror-stacksize-mismatch"));
        }
        return trigger;
    }

    private bool TriggerErrorOnNotChiseledBlock(ItemSlot slot)
    {
        bool trigger = slot.Empty || slot.Itemstack.Collectible is not BlockChisel;
        if (trigger)
        {
            (api as ICoreClientAPI)?.TriggerIngameError(this, "tabletopgames:ingameerror-chiseled-block-only", Lang.Get("tabletopgames:ingameerror-chiseled-block-only"));
        }
        return trigger;
    }

    public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
    {
        return blockAccessor.GetBlockEntity(pos)?.GetBehavior<BEBehaviorChiseledBoardSelection>() is BEBehaviorChiseledBoardSelection bebehavior
            ? bebehavior.GetOrCreateSelectionBoxes()
            : base.GetSelectionBoxes(blockAccessor, pos);
    }

    public override bool DoParticalSelection(IWorldAccessor world, BlockPos pos) => TabletopDebug.BoardParticleSelection;
    public override Vec4f GetSelectionColor(ICoreClientAPI capi, BlockPos pos) => TabletopDebug.BoardSelectionColor;

    public override bool DoPlaceBlock(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ItemStack byItemStack)
    {
        bool ok = base.DoPlaceBlock(world, byPlayer, blockSel, byItemStack);
        if (ok && world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityChiseledBoard blockEntiy)
        {
            BlockPos targetPos = blockSel.DidOffset ? blockSel.Position.AddCopy(blockSel.Face.Opposite) : blockSel.Position;
            double dx = byPlayer.Entity.Pos.X - (targetPos.X + blockSel.HitPosition.X);
            double dz = (float)byPlayer.Entity.Pos.Z - (targetPos.Z + blockSel.HitPosition.Z);
            float angleHor = (float)Math.Atan2(dx, dz);

            float intervalRad = GameMath.PIHALF;
            float roundRad = (int)Math.Round(angleHor / intervalRad) * intervalRad;
            blockEntiy.MeshAngleRad = roundRad;

            blockEntiy.ChiseledStackHitboxes = GetChiseledStack(byItemStack, world, EnumStackType.HitBoxes);
            blockEntiy.ChiseledStackTextures = GetChiseledStack(byItemStack, world, EnumStackType.Textures);

            blockEntiy.OnBlockPlaced(byItemStack);
        }
        return ok;
    }

    public override void GetDecal(IWorldAccessor world, BlockPos pos, ITexPositionSource decalTexSource, ref MeshData decalModelData, ref MeshData blockModelData)
    {
        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityChiseledBoard blockEntiy)
        {
            float[] mat = Matrixf.Create().Translate(0.5f, 0.5f, 0.5f).RotateY(blockEntiy.MeshAngleRad).Translate(-0.5f, -0.5f, -0.5f).Values;
            MeshData decalMesh = GetOrCreateMesh(blockEntiy, overrideTexturesource: decalTexSource)?.Clone()?.MatrixTransform(mat);
            MeshData blockMesh = GetOrCreateMesh(blockEntiy)?.Clone()?.MatrixTransform(mat);
            if (decalMesh != null && blockMesh != null)
            {
                decalModelData = decalMesh;
                blockModelData = blockMesh;
                return;
            }
        }

        base.GetDecal(world, pos, decalTexSource, ref decalModelData, ref blockModelData);
    }

    public override void OnBeforeRender(ICoreClientAPI capi, ItemStack itemstack, EnumItemRenderTarget target, ref ItemRenderInfo renderinfo)
    {
        Dictionary<string, MultiTextureMeshRef> meshRefs = ObjectCacheUtil.GetOrCreate(capi, "TabletopGames_BlockChiseledBoard_MeshRefs", () => new Dictionary<string, MultiTextureMeshRef>());

        string key = GetMeshCacheKey(itemstack);
        if (!meshRefs.TryGetValue(key, out MultiTextureMeshRef meshref))
        {
            MeshData mesh = GenGuiMesh(itemstack);
            meshref = capi.Render.UploadMultiTextureMesh(mesh);
            meshRefs[key] = meshref;
        }

        renderinfo.ModelRef = meshref;
        renderinfo.NormalShaded = true;

        base.OnBeforeRender(capi, itemstack, target, ref renderinfo);
    }

    public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
    {
        return world.BlockAccessor.GetBlockEntity(pos) is BlockEntityChiseledBoard blockEntiy
            ? (new ItemStack[1] { OnPickBlock(world, pos) })
            : base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
    }

    public override ItemStack OnPickBlock(IWorldAccessor world, BlockPos pos)
    {
        ItemStack stack = base.OnPickBlock(world, pos).Clone();
        if (world.BlockAccessor.GetBlockEntity(pos) is BlockEntityChiseledBoard blockEntiy)
        {
            SetChiseledStack(stack, blockEntiy.ChiseledStackHitboxes, EnumStackType.HitBoxes);
            SetChiseledStack(stack, blockEntiy.ChiseledStackTextures, EnumStackType.Textures);
        }
        return stack;
    }

    public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
    {
        return world.BlockAccessor.GetBlockEntity(blockSel.Position) is BlockEntityChiseledBoard blockEntity
            ? blockEntity.OnInteract(byPlayer, blockSel) || base.OnBlockInteractStart(world, byPlayer, blockSel)
            : base.OnBlockInteractStart(world, byPlayer, blockSel);
    }

    public static void SelfDestroyIfEmpty(ItemSlot slot, IWorldAccessor world)
    {
        ItemStack stackHitboxes = GetChiseledStack(slot.Itemstack, world, EnumStackType.HitBoxes);
        ItemStack stackTextures = GetChiseledStack(slot.Itemstack, world, EnumStackType.Textures);
        if (stackHitboxes == null && stackTextures == null)
        {
            slot.Itemstack = null;
            slot.MarkDirty();
        }
    }

    public static void SetChiseledStack(ItemStack ownStack, ItemStack inputStack, EnumStackType stackType)
    {
        string attribute = stackType switch
        {
            EnumStackType.HitBoxes => ChiseledStackHitboxesAttributeName,
            EnumStackType.Textures => ChiseledStackTexturesAttributeName
        };

        ownStack.Attributes.SetItemstack(attribute, inputStack);
    }

    public static ItemStack GetChiseledStack(ItemStack ownStack, IWorldAccessor worldForResolving, EnumStackType stackType, bool removeAttribute = false)
    {
        string attribute = stackType switch
        {
            EnumStackType.HitBoxes => ChiseledStackHitboxesAttributeName,
            EnumStackType.Textures => ChiseledStackTexturesAttributeName
        };

        ItemStack stack = ownStack.Attributes.GetItemstack(attribute);
        stack?.ResolveBlockOrItem(worldForResolving);
        if (removeAttribute)
        {
            ownStack.Attributes.RemoveAttribute(attribute);
        }
        return stack;
    }

    public MeshData GenGuiMesh(ItemStack stack)
    {
        ICoreClientAPI capi = api as ICoreClientAPI;
        MeshData mesh = new MeshData(4, 3);

        ItemStack stackHitboxes = GetChiseledStack(stack, api.World, EnumStackType.HitBoxes);
        ItemStack stackTextures = GetChiseledStack(stack, api.World, EnumStackType.Textures);
        ItemStack stackToRender = stackTextures ?? stackHitboxes;

        if (stackToRender != null)
        {
            MeshData containedMesh = stackToRender.CreateChiseledMesh(api);
            mesh.AddMeshData(containedMesh);
        }
        return mesh;
    }

    public MeshData GetOrCreateMesh(BlockEntityChiseledBoard blockEntity, ITexPositionSource overrideTexturesource = null)
    {
        ICoreClientAPI capi = api as ICoreClientAPI;
        MeshData mesh = new MeshData(4, 3);

        ItemStack stackHitboxes = blockEntity.ChiseledStackHitboxes;
        ItemStack stackTextures = blockEntity.ChiseledStackTextures;
        ItemStack stackToRender = stackTextures ?? stackHitboxes;

        if (stackToRender != null)
        {
            MeshData containedMesh = stackToRender.CreateChiseledMesh(api);
            mesh.AddMeshData(containedMesh);
        }
        return mesh;
    }

    public MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        return GenGuiMesh(itemstack);
    }

    public string GetMeshCacheKey(ItemStack itemstack)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(itemstack.Collectible.Code);

        ItemStack stackHitboxes = GetChiseledStack(itemstack, api.World, EnumStackType.HitBoxes);
        ItemStack stackTextures = GetChiseledStack(itemstack, api.World, EnumStackType.Textures);
        ItemStack stackToRender = stackTextures ?? stackHitboxes;

        if (stackToRender != null)
        {
            stringBuilder.Append("-chiseledstack:");
            stringBuilder.Append('-');
            stringBuilder.Append(stackToRender.Collectible.Code);
            stringBuilder.Append('-');
            stringBuilder.Append(stackToRender.Attributes.ToJsonToken());
        }
        return stringBuilder.ToString();
    }
}