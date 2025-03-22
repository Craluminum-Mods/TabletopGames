using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TabletopGames;

/// <summary>
/// Playing card meshes for every EnumCardRenderType
/// </summary>
public static class PlayingCardMeshes
{
    /// <summary>
    /// Predefined random rotations for EnumCardRenderType.Stack
    /// </summary>
    public static readonly float[] stackRotations = new float[128] {
    -0.0167f, -0.0980f,  0.0650f, -0.0403f, -0.0263f, -0.0613f,  0.0132f, -0.0677f,
    -0.0751f, -0.0134f,  0.0124f, -0.0651f,  0.0106f, -0.0290f,  0.0916f, -0.0817f,
     0.0957f, -0.0176f,  0.0008f, -0.0704f,  0.0438f, -0.0620f, -0.0317f, -0.0953f,
    -0.0321f,  0.0935f,  0.0958f,  0.0489f, -0.0993f,  0.0880f,  0.0742f,  0.0542f,
    -0.0642f, -0.0801f, -0.0171f,  0.0771f,  0.0156f,  0.0473f, -0.0535f,  0.0047f,
     0.0419f,  0.0650f,  0.0614f, -0.0535f,  0.0746f, -0.0567f,  0.0604f,  0.0110f,
    -0.0628f,  0.0177f,  0.0036f,  0.0917f, -0.0917f, -0.0672f,  0.0967f,  0.0664f,
    -0.0699f, -0.0542f,  0.0078f, -0.0686f, -0.0352f, -0.0901f,  0.0423f, -0.0843f,

    -0.0167f, -0.0980f,  0.0650f, -0.0403f, -0.0263f, -0.0613f,  0.0132f, -0.0677f,
    -0.0751f, -0.0134f,  0.0124f, -0.0651f,  0.0106f, -0.0290f,  0.0916f, -0.0817f,
     0.0957f, -0.0176f,  0.0008f, -0.0704f,  0.0438f, -0.0620f, -0.0317f, -0.0953f,
    -0.0321f,  0.0935f,  0.0958f,  0.0489f, -0.0993f,  0.0880f,  0.0742f,  0.0542f,
    -0.0642f, -0.0801f, -0.0171f,  0.0771f,  0.0156f,  0.0473f, -0.0535f,  0.0047f,
     0.0419f,  0.0650f,  0.0614f, -0.0535f,  0.0746f, -0.0567f,  0.0604f,  0.0110f,
    -0.0628f,  0.0177f,  0.0036f,  0.0917f, -0.0917f, -0.0672f,  0.0967f,  0.0664f,
    -0.0699f, -0.0542f,  0.0078f, -0.0686f, -0.0352f, -0.0901f,  0.0423f, -0.0843f,
    };

    /// <summary>
    /// Generates mesh for a single card
    /// </summary>
    /// <param name="itemstack">First card item</param>
    /// <returns>Single card mesh</returns>
    public static MeshData GenOneMesh(this ItemPlayingCard mainCard, ItemStack itemstack, ITextureAtlasAPI targetAtlas, EnumCardRenderType renderType)
    {
        ICoreClientAPI capi = mainCard.clientApi;
        MeshData mesh = new MeshData(32, 32).WithXyzFaces().WithRenderpasses().WithColorMaps();

        Variants variants = Variants.FromStack(itemstack);
        bool isFlipped = ItemPlayingCard.IsCardFlipped(itemstack);

        CompositeShape _shape = null;
        variants.FindByVariant(mainCard.ShapeByType, out _shape);

        if (_shape == null) return mesh;

        CompositeShape rcshape = _shape.Clone();
        rcshape.Base.Path = variants.ReplacePlaceholders(rcshape.Base.Path);
        rcshape.Base.WithPathAppendixOnce(".json").WithPathPrefixOnce("shapes/");

        Shape? shape = capi.Assets.TryGet(rcshape.Base)?.ToObject<Shape>();

        Dictionary<string, CompositeTexture> _textures = null;
        if (isFlipped || renderType == EnumCardRenderType.HandSafe)
        {
            variants.FindByVariant(mainCard.SafeTexturesByType, out _textures);
        }
        else
        {
            variants.FindByVariant(mainCard.TexturesByType, out _textures);
        }

        _textures ??= new Dictionary<string, CompositeTexture>();

        UniversalShapeTextureSource stexSource = new UniversalShapeTextureSource(capi, targetAtlas, shape, rcshape.Base.ToString());

        foreach (KeyValuePair<string, CompositeTexture> val in _textures)
        {
            CompositeTexture ctex = val.Value.Clone();
            ctex = variants.ReplacePlaceholders(ctex);
            ctex.Bake(capi.Assets);
            stexSource.textures[val.Key] = ctex;
        }

        if (shape == null) return mesh;
        capi.Tesselator.TesselateShape("ItemPlayingCard item", shape, out mesh, stexSource);
        return mesh;
    }

    /// <summary>
    /// Generates mesh for card in gui slot
    /// </summary>
    /// <param name="itemstack">First card item</param>
    /// <returns>Mesh of hand of cards</returns>
    public static MeshData GenGuiMesh(this ItemPlayingCard mainCard, ItemStack itemstack, ITextureAtlasAPI targetAtlas, EnumCardRenderType renderType)
    {
        MeshData mesh = mainCard.GenOneMesh(itemstack, targetAtlas, renderType);
        MeshData contentMesh = null;
        PlayingCardInventory inventory = mainCard.GetInventory(itemstack);

        if (inventory.Empty) return mesh;

        const float BASE_ROTATION = GameMath.DEG2RAD * 5.0f;
        const float ROTATION_STEP = GameMath.DEG2RAD * 8.5f;
        const float MAGIC_SCALAR = 8f;

        float translationY = 0;
        float previousFanStartingTranslationY = 0;

        float cardHeight = 0;
        float rotation = BASE_ROTATION;

        Vec3f rotationOrigin = new Vec3f(0, 0, 0.35f);
        mesh = mesh.Translate(-0.5f, -0.5f, -0.5f);
        mesh = mesh.Rotate(rotationOrigin, 0, rotation, 0);
        mesh = mesh.Translate(0.5f, 0.5f, 0.5f);

        for (int cardIndex = 0; cardIndex < inventory.Slots.Length; cardIndex++)
        {
            ItemSlot slot = inventory.Slots[cardIndex];
            if (slot.Empty
                || slot.Itemstack.Collectible is not ItemPlayingCard otherCard
                || otherCard.GetOrCreateMesh(slot.Itemstack, targetAtlas, renderType) is not MeshData containedMesh
                || containedMesh == null)
            {
                continue;
            }

            float cardThickness = otherCard.GetStackingTranslation(slot.Itemstack);

            // not adding 1 breaks things, since 0 slot is the 2nd card
            bool isStartingNewFan = (cardIndex + 1) % 16 == 0;
            if (isStartingNewFan)
            {
                rotation = BASE_ROTATION;
                cardHeight -= 0.2f;
                translationY = previousFanStartingTranslationY - cardThickness * MAGIC_SCALAR;
                previousFanStartingTranslationY = translationY;
            }
            else
            {
                translationY += cardThickness;
            }

            rotation -= ROTATION_STEP;
            containedMesh = containedMesh.Translate(-0.5f, -0.5f, -0.5f);
            containedMesh = containedMesh.Rotate(rotationOrigin, 0, rotation, 0);
            containedMesh = containedMesh.Translate(0.5f, 0.5f, 0.5f);

            containedMesh = containedMesh.Translate(0, translationY, cardHeight);

            if (contentMesh != null) contentMesh.AddMeshData(containedMesh);
            else contentMesh = containedMesh;
        }

        if (contentMesh != null) mesh.AddMeshData(contentMesh);
        return mesh;
    }

    /// <summary>
    /// Generates mesh for cards stacked on each other
    /// </summary>
    /// <param name="itemstack">First card item</param>
    /// <returns>Mesh of stack of cards</returns>
    public static MeshData GenStackMesh(this ItemPlayingCard mainCard, ItemStack itemstack, ITextureAtlasAPI targetAtlas, EnumCardRenderType renderType)
    {
        MeshData mesh = mainCard.GenOneMesh(itemstack, targetAtlas, renderType);
        MeshData contentMesh = null;
        PlayingCardInventory inventory = mainCard.GetInventory(itemstack);

        if (inventory.Empty) return mesh;

        float translationY = 0;
        int totalItems = inventory.TotalItemCount;

        for (int cardIndex = 0; cardIndex < inventory.Slots.Length; cardIndex++)
        {
            ItemSlot slot = inventory.Slots[cardIndex];

            if (slot.Empty
                || slot.Itemstack.Collectible is not ItemPlayingCard otherCard
                || otherCard.GetOrCreateMesh(slot.Itemstack, targetAtlas, renderType) is not MeshData containedMesh
                || containedMesh == null)
            {
                continue;
            }

            // 0 index is 2nd card
            bool rotateVeryfirstCard = cardIndex == 0 && !ItemPlayingCard.IsCardFlipped(itemstack);
            if (rotateVeryfirstCard)
            {
                mesh = mesh.Translate(-0.5f, -0.5f, -0.5f);
                mesh = mesh.Rotate(Vec3f.Zero, 0, GameMath.DEG2RAD * 90f, 0);
                mesh = mesh.Translate(0.5f, 0.5f, 0.5f);
            }

            float cardThickness = otherCard.GetStackingTranslation(slot.Itemstack);
            float rotation = stackRotations.Length > cardIndex ? stackRotations[cardIndex] : 0;

            containedMesh = containedMesh.Translate(-0.5f, -0.5f, -0.5f);
            containedMesh = containedMesh.Rotate(Vec3f.Zero, 0, rotation, 0);
            containedMesh = containedMesh.Translate(0.5f, 0.5f, 0.5f);

            translationY += cardThickness;
            containedMesh = containedMesh.Translate(0, translationY, 0);

            if (contentMesh != null) contentMesh.AddMeshData(containedMesh);
            else contentMesh = containedMesh;
        }

        if (contentMesh != null) mesh.AddMeshData(contentMesh);
        return mesh;
    }

    /// <summary>
    /// Generates mesh for hand of cards 
    /// </summary>
    /// <param name="itemstack">First card item</param>
    /// <returns>Mesh of hand of cards</returns>
    public static MeshData GenHandMesh(this ItemPlayingCard mainCard, ItemStack itemstack, ITextureAtlasAPI targetAtlas, EnumCardRenderType renderType)
    {
        MeshData mesh = mainCard.GenOneMesh(itemstack, targetAtlas, renderType);
        MeshData contentMesh = null;
        PlayingCardInventory inventory = mainCard.GetInventory(itemstack);

        if (inventory.Empty) return mesh;

        const float BASE_ROTATION = GameMath.DEG2RAD * 65.0f;
        const float ROTATION_STEP = GameMath.DEG2RAD * 8.5f;
        const float MAGIC_SCALAR = 8f;

        float translationY = 0;
        float previousFanStartingTranslationY = 0;

        float cardHeight = 0;
        float rotation = BASE_ROTATION;

        Vec3f rotationOrigin = new Vec3f(0, 0, 0.35f);
        mesh = mesh.Translate(-0.5f, -0.5f, -0.5f);
        mesh = mesh.Rotate(rotationOrigin, 0, rotation, 0);
        mesh = mesh.Translate(0.5f, 0.5f, 0.5f);

        for (int cardIndex = 0; cardIndex < inventory.Slots.Length; cardIndex++)
        {
            ItemSlot slot = inventory.Slots[cardIndex];

            if (slot.Empty
                || slot.Itemstack.Collectible is not ItemPlayingCard otherCard
                || otherCard.GetOrCreateMesh(slot.Itemstack, targetAtlas, renderType) is not MeshData containedMesh
                || containedMesh == null)
            {
                continue;
            }

            float cardThickness = otherCard.GetStackingTranslation(slot.Itemstack);

            // not adding 1 breaks things, since 0 slot is the 2nd card
            bool isStartingNewFan = (cardIndex + 1) % 16 == 0;
            if (isStartingNewFan)
            {
                rotation = BASE_ROTATION;
                cardHeight -= 0.2f;
                translationY = previousFanStartingTranslationY - cardThickness * MAGIC_SCALAR;
                previousFanStartingTranslationY = translationY;
            }
            else
            {
                translationY += cardThickness;
            }

            rotation -= ROTATION_STEP;
            containedMesh = containedMesh.Translate(-0.5f, -0.5f, -0.5f);
            containedMesh = containedMesh.Rotate(rotationOrigin, 0, rotation, 0);
            containedMesh = containedMesh.Translate(0.5f, 0.5f, 0.5f);

            containedMesh = containedMesh.Translate(0, translationY, cardHeight);

            if (contentMesh != null) contentMesh.AddMeshData(containedMesh);
            else contentMesh = containedMesh;
        }

        if (contentMesh != null) mesh.AddMeshData(contentMesh);
        return mesh;
    }
}