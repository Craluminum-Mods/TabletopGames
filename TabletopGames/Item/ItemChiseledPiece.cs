using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace TabletopGames;

public class ItemChiseledPiece : ItemBoardPiece
{
    public enum EnumMode
    {
        Exchange = 0,
        Remove = 1,
        UpAdd = 2,
        UpRemove = 3,
        DownAdd = 4,
        DownRemove = 5,
        Rotate = 6,
        ScaleDown = 7,
        ScaleUp = 8
    }

    public const string InventoryAttributeName = "containedChiseledStacks";
    public const string RotateYAttributeName = "rotateY";
    public const string ScaleAttributeName = "scale";

    private SkillItem[] toolModes = Array.Empty<SkillItem>();

    public override void OnLoaded(ICoreAPI api)
    {
        base.OnLoaded(api);

        toolModes = new SkillItem[]
        {
            new() { Name = Lang.Get("tabletopgames:toolmode-sinkslot-exchange-chiseled-block") },
            new() { Name = Lang.Get("tabletopgames:toolmode-remove-chiseled-block") },
            new() { Name = Lang.Get("tabletopgames:toolmode-sinkslot-up-add-chiseled-block"), Linebreak = true },
            new() { Name = Lang.Get("tabletopgames:toolmode-sinkslot-up-remove-chiseled-block") },
            new() { Name = Lang.Get("tabletopgames:toolmode-sinkslot-down-add-chiseled-block"), Linebreak = true },
            new() { Name = Lang.Get("tabletopgames:toolmode-sinkslot-down-remove-chiseled-block") },
            new() { Name = Lang.Get("tabletopgames:toolmode-increase-rotation-by-90-degrees"), Linebreak = true },
            new() { Name = Lang.Get("tabletopgames:toolmode-scale-down") },
            new() { Name = Lang.Get("tabletopgames:toolmode-scale-up") },
        };

        if (api is not ICoreClientAPI capi) return;

        for (int i = 0; i < toolModes.Length; i++)
        {
            SkillItem toolMode = toolModes[i];
            switch ((EnumMode)i)
            {
                case EnumMode.Exchange:
                    toolMode.WithIcon(capi, capi.Gui.LoadSvgWithPadding("tabletopgames:textures/icons/exchange.svg", 48, 48, 5, color: ColorUtil.WhiteArgb));
                    toolMode.TexturePremultipliedAlpha = false;
                    break;
                case EnumMode.Remove:
                    toolMode.WithIcon(capi, capi.Gui.LoadSvgWithPadding("textures/icons/worldedit/chiselbrush.svg", 48, 48, 5, color: ColorUtil.Hex2Int("#ff8484")));
                    toolMode.TexturePremultipliedAlpha = false;
                    break;
                case EnumMode.UpAdd:
                    toolMode.WithIcon(capi, capi.Gui.LoadSvgWithPadding("tabletopgames:textures/icons/up-add.svg", 48, 48, 5, color: null));
                    toolMode.TexturePremultipliedAlpha = false;
                    break;
                case EnumMode.UpRemove:
                    toolMode.WithIcon(capi, capi.Gui.LoadSvgWithPadding("tabletopgames:textures/icons/up-remove.svg", 48, 48, 5, color: null));
                    toolMode.TexturePremultipliedAlpha = false;
                    break;
                case EnumMode.DownAdd:
                    toolMode.WithIcon(capi, capi.Gui.LoadSvgWithPadding("tabletopgames:textures/icons/down-add.svg", 48, 48, 5, color: null));
                    toolMode.TexturePremultipliedAlpha = false;
                    break;
                case EnumMode.DownRemove:
                    toolMode.WithIcon(capi, capi.Gui.LoadSvgWithPadding("tabletopgames:textures/icons/down-remove.svg", 48, 48, 5, color: null));
                    toolMode.TexturePremultipliedAlpha = false;
                    break;
                case EnumMode.Rotate:
                    toolMode.WithIcon(capi, capi.Gui.LoadSvgWithPadding("textures/icons/worldedit/rotate.svg", 48, 48, 5, color: null));
                    toolMode.TexturePremultipliedAlpha = false;
                    break;
                case EnumMode.ScaleDown:
                    toolMode.WithIcon(capi, capi.Gui.LoadSvgWithPadding("tabletopgames:textures/icons/scale-down.svg", 48, 48, 5, color: null));
                    toolMode.TexturePremultipliedAlpha = false;
                    break;
                case EnumMode.ScaleUp:
                    toolMode.WithIcon(capi, capi.Gui.LoadSvgWithPadding("tabletopgames:textures/icons/scale-up.svg", 48, 48, 5, color: null));
                    toolMode.TexturePremultipliedAlpha = false;
                    break;
            }
            toolModes[i] = toolMode;
        }
    }

    public override void LoadTypes() { }

    public override void OnUnloaded(ICoreAPI api)
    {
        base.OnUnloaded(api);
        toolModes?.Foreach(mode => mode?.Dispose());
    }

    public override void OnHeldIdle(ItemSlot slot, EntityAgent byEntity)
    {
        base.OnHeldIdle(slot, byEntity);
        SelfDestroyIfEmpty(slot);
    }

    public override void OnGroundIdle(EntityItem entityItem)
    {
        base.OnGroundIdle(entityItem);
        SelfDestroyIfEmpty(entityItem?.Slot);
    }

    public override string GetHeldItemName(ItemStack itemStack)
    {
        string name = GetChiseledStack(itemStack, Vec3i.Zero, api.World)?.Attributes.GetString("blockName");
        return !string.IsNullOrEmpty(name) ? name : base.GetHeldItemName(itemStack);
    }

    public override void GetHeldItemInfo(ItemSlot inSlot, StringBuilder dsc, IWorldAccessor world, bool withDebugInfo)
    {
        base.GetHeldItemInfo(inSlot, dsc, world, withDebugInfo);

        int rotateY = inSlot.Itemstack.Attributes.GetInt(RotateYAttributeName);
        if (rotateY != 0)
        {
            dsc.AppendLine(Lang.Get("tabletopgames:rotation-y", rotateY));
        }

        float scale = inSlot.Itemstack.Attributes.GetFloat(ScaleAttributeName, 1);
        if (scale != 1)
        {
            dsc.AppendLine(Lang.Get("tabletopgames:scale", scale));
        }
    }

    public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int index)
    {
        if (slot.Empty || toolModes?.Length <= index) return;

        ITreeAttribute chiseledStacksTree = slot.Itemstack.Attributes.GetTreeAttribute(InventoryAttributeName);
        if (chiseledStacksTree == null || !chiseledStacksTree.Any()) return;

        ItemSlot mouseslot = byPlayer.InventoryManager.MouseItemSlot;
        ItemStack giveStack = null;
        bool keepOpen = true;

        int upLimit = api.World.Config.GetInt("tabletopgames_chiseledPieceMaxUp", 6);
        int downLimit = api.World.Config.GetInt("tabletopgames_chiseledPieceMaxDown", 2);

        switch ((EnumMode)index)
        {
            case EnumMode.Exchange:
                {
                    if (TriggerErrorOnNotChiseledBlock(mouseslot)) break;
                    if (TriggerErrorOnStackSizeMismatch(slot.StackSize, mouseslot.StackSize)) break;

                    ItemStack removedStack = GetChiseledStack(slot.Itemstack, Vec3i.Zero, api.World).Clone();
                    removedStack.StackSize = slot.StackSize;

                    ItemStack clonedMouseStack = mouseslot.Itemstack.Clone();
                    clonedMouseStack.StackSize = 1;
                    mouseslot.Itemstack.SetFrom(removedStack);

                    SetChiseledStack(slot.Itemstack, inputStack: clonedMouseStack, Vec3i.Zero);
                }
                break;
            case EnumMode.Remove:
                if (mouseslot.Empty && chiseledStacksTree.Count == 1)
                {
                    giveStack = GetChiseledStack(slot.Itemstack, Vec3i.Zero, api.World, removeAttribute: true);
                    giveStack.StackSize = slot.StackSize;
                    keepOpen = false;
                }
                break;
            case EnumMode.UpAdd:
                {
                    if (TriggerErrorOnNotChiseledBlock(mouseslot)) break;
                    if (TriggerErrorOnStackSizeMismatch(slot.StackSize, mouseslot.StackSize)) break;

                    Vec3i curOffset = Vec3i.Zero;
                    for (int i = 0; i < upLimit; i++)
                    {
                        curOffset.Y = i;
                        if (!chiseledStacksTree.HasAttribute(ToXYZString(curOffset)))
                        {
                            ItemStack clonedMouseStack = mouseslot.TakeOutWhole();
                            clonedMouseStack.StackSize = 1;
                            SetChiseledStack(slot.Itemstack, clonedMouseStack, curOffset);
                            break;
                        }
                    }
                }
                break;
            case EnumMode.UpRemove:
                {
                    Vec3i curOffset = Vec3i.Zero;
                    for (int i = 10; i > 0; i--)
                    {
                        curOffset.Y = i;
                        if (chiseledStacksTree.HasAttribute(ToXYZString(curOffset)))
                        {
                            giveStack = GetChiseledStack(slot.Itemstack, curOffset, api.World, removeAttribute: true);
                            giveStack.StackSize = slot.StackSize;
                            break;
                        }
                    }
                }
                break;
            case EnumMode.DownAdd:
                {
                    if (TriggerErrorOnNotChiseledBlock(mouseslot)) break;
                    if (TriggerErrorOnStackSizeMismatch(slot.StackSize, mouseslot.StackSize)) break;

                    Vec3i curOffset = Vec3i.Zero;
                    for (int i = 0; i >= -downLimit; i--)
                    {
                        curOffset.Y = i;
                        if (!chiseledStacksTree.HasAttribute(ToXYZString(curOffset)))
                        {
                            ItemStack clonedMouseStack = mouseslot.TakeOutWhole();
                            clonedMouseStack.StackSize = 1;
                            SetChiseledStack(slot.Itemstack, clonedMouseStack, curOffset);
                            break;
                        }
                    }
                }
                break;
            case EnumMode.DownRemove:
                {
                    Vec3i curOffset = Vec3i.Zero;
                    for (int i = -10; i < 0; i++)
                    {
                        curOffset.Y = i;
                        if (chiseledStacksTree.HasAttribute(ToXYZString(curOffset)))
                        {
                            giveStack = GetChiseledStack(slot.Itemstack, curOffset, api.World, removeAttribute: true);
                            giveStack.StackSize = slot.StackSize;
                            break;
                        }
                    }
                }
                break;
            case EnumMode.Rotate:
                int rotateY = slot.Itemstack.Attributes.GetInt(RotateYAttributeName);
                slot.Itemstack.Attributes.SetInt(RotateYAttributeName, value: (rotateY + 90) % 360);
                break;
            case EnumMode.ScaleDown:
                {
                    keepOpen = true;
                    float scale = slot.Itemstack.Attributes.GetFloat(ScaleAttributeName, 1);
                    scale = byPlayer.Entity.Controls.ShiftKey ? scale - 0.25f : scale - 1;
                    if (scale <= 0) break;
                    slot.Itemstack.Attributes.SetFloat(ScaleAttributeName, scale);
                    break;
                }
            case EnumMode.ScaleUp:
                {
                    keepOpen = true;
                    float scale = slot.Itemstack.Attributes.GetFloat(ScaleAttributeName, 1);
                    scale = byPlayer.Entity.Controls.ShiftKey ? scale + 0.25f : scale + 1;
                    if (scale <= 0) break;
                    slot.Itemstack.Attributes.SetFloat(ScaleAttributeName, scale);
                    break;
                }
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

    private static void SelfDestroyIfEmpty(ItemSlot slot)
    {
        ITreeAttribute stacks = slot?.Itemstack?.Attributes?.GetTreeAttribute(InventoryAttributeName);
        if (stacks == null || !stacks.Any())
        {
            slot.Itemstack = null;
            slot.MarkDirty();
        }
    }

    public static void SetChiseledStack(ItemStack ownStack, ItemStack inputStack, Vec3i pos)
    {
        ownStack.Attributes.GetOrAddTreeAttribute(InventoryAttributeName).SetItemstack(ToXYZString(pos), inputStack);
    }

    public static ItemStack GetChiseledStack(ItemStack ownStack, Vec3i xyz, IWorldAccessor worldForResolving, bool removeAttribute = false)
    {
        return GetChiseledStack(ownStack, ToXYZString(xyz), worldForResolving, removeAttribute);
    }

    public static ItemStack GetChiseledStack(ItemStack ownStack, string xyz, IWorldAccessor worldForResolving, bool removeAttribute = false)
    {
        ItemStack stack = ownStack.Attributes.GetTreeAttribute(InventoryAttributeName)?.GetItemstack(xyz);
        stack?.ResolveBlockOrItem(worldForResolving);
        if (removeAttribute)
        {
            ownStack.Attributes.GetTreeAttribute(InventoryAttributeName)?.RemoveAttribute(xyz);
        }
        return stack;
    }

    public static string ToXYZString(Vec3i pos)
    {
        return $"{pos.X},{pos.Y},{pos.X}";
    }

    public static Vec3i FromXYZString(string xyzString)
    {
        string[] xyz = xyzString.Split(',');
        if (xyz.Length == 3)
        {
            return new Vec3i(xyz[0].ToInt(), xyz[1].ToInt(), xyz[2].ToInt());
        }
        return null;
    }

    public override MeshData GenMesh(ItemStack itemstack, ITextureAtlasAPI targetAtlas, BlockPos atBlockPos)
    {
        ICoreClientAPI capi = api as ICoreClientAPI;
        MeshData mesh = new MeshData(4, 3);

        ITreeAttribute chiseledStacksTree = itemstack.Attributes.GetTreeAttribute(InventoryAttributeName);
        if (chiseledStacksTree != null && chiseledStacksTree.Any())
        {
            foreach (KeyValuePair<string, IAttribute> attr in chiseledStacksTree)
            {
                Vec3i offset = FromXYZString(attr.Key);
                if (offset == null) continue;

                ItemStack containedStack = GetChiseledStack(itemstack, xyz: attr.Key, api.World);
                if (containedStack == null) continue;

                MeshData containedMesh = containedStack.CreateChiseledMesh(api);
                mesh.AddMeshData(containedMesh, offset.X, offset.Y, offset.Z);
            }
        }
        if (itemstack.Attributes.HasAttribute(RotateYAttributeName))
        {
            mesh = mesh.Clone().Rotate(Vec3f.Half, 0, GameMath.DEG2RAD * itemstack.Attributes.GetInt(RotateYAttributeName), 0);
        }
        if (itemstack.Attributes.HasAttribute(ScaleAttributeName))
        {
            float scale = itemstack.Attributes.GetFloat(ScaleAttributeName, 1);
            mesh = mesh.Clone().Scale(new Vec3f(0.5f, 0, 0.5f), scale, scale, scale);
        }

        return mesh;
    }

    public override string GetMeshCacheKey(ItemStack itemstack)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(itemstack.Collectible.Code);
        stringBuilder.Append("-rotY:");
        stringBuilder.Append(itemstack.Attributes.GetInt(RotateYAttributeName));
        stringBuilder.Append("-scale:");
        stringBuilder.Append(itemstack.Attributes.GetFloat(ScaleAttributeName, 1));

        ITreeAttribute chiseledStacksTree = itemstack.Attributes.GetTreeAttribute(InventoryAttributeName);
        if (chiseledStacksTree != null && chiseledStacksTree.Any())
        {
            stringBuilder.Append("-inv:");
            foreach (KeyValuePair<string, IAttribute> attr in chiseledStacksTree)
            {
                ItemStack containedStack = GetChiseledStack(itemstack, xyz: attr.Key, api.World);
                stringBuilder.Append(attr.Key);
                stringBuilder.Append('-');
                stringBuilder.Append(containedStack?.Collectible.Code);
                stringBuilder.Append('-');
                stringBuilder.Append(containedStack?.Attributes.ToJsonToken());
            }
        }

        return stringBuilder.ToString();
    }
}