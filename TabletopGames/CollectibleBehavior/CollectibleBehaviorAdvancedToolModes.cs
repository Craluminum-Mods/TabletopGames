using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace TabletopGames;

public class CollectibleBehaviorAdvancedToolModes : CollectibleBehavior
{
    private Dictionary<string, List<AdvancedToolMode>> toolModesByType = new();
    private Dictionary<string, LoadedTexture> texturesByKeyResolved = new();
    private Dictionary<string, string> texturesByKey = new();

    public CollectibleBehaviorAdvancedToolModes(CollectibleObject collObj) : base(collObj) { }

    public override void Initialize(JsonObject properties)
    {
        base.Initialize(properties);
        toolModesByType = properties["toolModes"].AsObject(defaultValue: new Dictionary<string, List<AdvancedToolMode>>());
        texturesByKey = properties["textures"].AsObject(defaultValue: new Dictionary<string, string>());
    }

    public override void OnLoaded(ICoreAPI api)
    {
        if (api is not ICoreClientAPI capi) return;

        foreach ((string key, string path) in texturesByKey)
        {
            if (texturesByKeyResolved.ContainsKey(key)) continue;

            if (capi.Assets.TryGet(path) is IAsset asset)
            {
                LoadedTexture _texture = new SkillItem().WithIcon(capi, capi.Gui.LoadSvgWithPadding(asset.Location, 48, 48, 5)).Texture;
                texturesByKeyResolved.Add(key, _texture);
                continue;
            }

            texturesByKeyResolved.Add(key, new SkillItem().WithIcon(capi, path).Texture);
        }
    }

    public override void OnUnloaded(ICoreAPI api)
    {
        texturesByKeyResolved?.Foreach(texture => texture.Value?.Dispose());
    }

    public override void SetToolMode(ItemSlot slot, IPlayer byPlayer, BlockSelection blockSelection, int index)
    {
        if (slot.Empty) return;

        Variants variants = Variants.FromStack(slot.Itemstack);
        if (!variants.FindByVariant(toolModesByType, out List<AdvancedToolMode> toolModes) || toolModes == null || !toolModes.Any())
        {
            return;
        }

        if (toolModes.Count <= index || toolModes[index] is not AdvancedToolMode advMode) return;

        if (TryProcessSinkSlot(advMode, slot, byPlayer, variants)) return;

        JsonItemStack output = advMode.ConvertTo?.Clone();
        output?.Resolve(byPlayer.Entity.World, "");

        slot.Itemstack.OverwriteVariants(out ItemStack finalStack, setVariants: advMode.SetVariants, removeVariants: advMode.RemoveVariants, variants: variants);

        if (output != null && output.ResolvedItemstack != null)
        {
            if (advMode.CopyAttributes && finalStack.Attributes != null)
            {
                output.ResolvedItemstack.Attributes = finalStack.Attributes.Clone();
            }

            slot.Itemstack.SetFrom(output.ResolvedItemstack ?? finalStack);
        }
        else
        {
            slot.Itemstack.SetFrom(finalStack);
        }

        slot.MarkDirty();
        byPlayer.InventoryManager.BroadcastHotbarSlot();
    }

    public override SkillItem[] GetToolModes(ItemSlot slot, IClientPlayer forPlayer, BlockSelection blockSel)
    {
        SkillItem[] _toolModes = Array.Empty<SkillItem>();

        if (slot.Empty) return null;

        Variants variants = Variants.FromStack(slot.Itemstack);
        if (!variants.FindByVariant(toolModesByType, out List<AdvancedToolMode> toolModes) || toolModes == null || !toolModes.Any())
        {
            return null;
        }

        foreach (AdvancedToolMode advMode in toolModes)
        {
            if (advMode == null) continue;
            if (TryAddSinkSlot(ref _toolModes, forPlayer, advMode, texturesByKeyResolved)) continue;

            JsonItemStack output = advMode.ConvertTo?.Clone();
            output?.Resolve(forPlayer.Entity.World, "");

            slot.Itemstack.OverwriteVariants(out ItemStack finalStack, advMode.SetVariants, variants: variants);

            if (output != null && output.ResolvedItemstack != null)
            {
                if (advMode.CopyAttributes && finalStack.Attributes != null)
                {
                    output.ResolvedItemstack.Attributes = finalStack.Attributes.Clone();
                }

                finalStack.SetFrom(output.ResolvedItemstack ?? finalStack);
            }

            SkillItem mode = new()
            {
                Name = advMode.NameExists ? advMode.GetName() : finalStack.GetName(),
                Linebreak = advMode.Linebreak
            };

            mode = WithTextureOrRender(mode, forPlayer.Entity.World.Api, advMode, finalStack);
            _toolModes = _toolModes.Append(mode);
        }

        return _toolModes;
    }

    public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
    {
        handling = EnumHandling.PassThrough;
        return base.GetHeldInteractionHelp(inSlot, ref handling).Append(new WorldInteraction
        {
            ActionLangCode = "heldhelp-settoolmode",
            HotKeyCode = "toolmodeselect"
        });
    }

    private static bool TryProcessSinkSlot(AdvancedToolMode advMode, ItemSlot slot, IPlayer byPlayer, Variants variants)
    {
        ItemSlot mouseslot = byPlayer.InventoryManager.MouseItemSlot;

        if (!advMode.IsSinkSlot || mouseslot.Empty) return false;

        if (!byPlayer.ConsumeIngredientAndGiveStack(mouseslot, variants, advMode.SlotParams))
        {
            slot.HandleInWorldCrafting(byPlayer, mouseslot, variants, advMode.SlotParams);
        }

        byPlayer.Entity.World.Api.Event.PushEvent("keepopentoolmodedlg");
        return true;
    }

    public static bool TryAddSinkSlot(ref SkillItem[] _toolModes, IClientPlayer forPlayer, AdvancedToolMode advMode, Dictionary<string, LoadedTexture> textures = null)
    {
        if (!advMode.IsSinkSlot) return false;

        SkillItem mode = new()
        {
            Name = advMode.GetName(),
            Linebreak = advMode.Linebreak
        };

        mode = WithTextureOrRender(mode, forPlayer.Entity.World.Api, advMode, textures: textures);
        _toolModes = _toolModes.Append(mode);
        return true;
    }

    public static SkillItem WithTextureOrRender(SkillItem mode, ICoreAPI api, AdvancedToolMode advMode, ItemStack forStack = null, Dictionary<string, LoadedTexture> textures = null)
    {
        if (api is not ICoreClientAPI capi) return mode;

        if (textures != null && textures.TryGetValue(advMode.IconTexture, out LoadedTexture _texture) && _texture != null)
        {
            mode.Texture = _texture;
            mode.TexturePremultipliedAlpha = false;
            return mode;
        }

        advMode.IconStack?.Resolve(capi.World, "");
        ItemStack renderedStack = forStack ?? advMode.IconStack?.ResolvedItemstack;

        if (renderedStack != null)
        {
            mode.RenderHandler = renderedStack.RenderItemStack(capi, showStackSize: false);
        }
        return mode;
    }
}