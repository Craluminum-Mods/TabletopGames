using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace TabletopGames;

/// <summary>
/// Better version of Vintagestory.GameContent.ShapeTextureSource that supports both items and blocks
/// </summary>
public class UniversalShapeTextureSource : ITexPositionSource
{
    ICoreClientAPI capi;
    ITextureAtlasAPI targetAtlas;
    Shape shape;
    string filenameForLogging;
    public Dictionary<string, CompositeTexture> textures = new Dictionary<string, CompositeTexture>();
    public TextureAtlasPosition firstTexPos;

    HashSet<AssetLocation> missingTextures = new HashSet<AssetLocation>();

    public UniversalShapeTextureSource(ICoreClientAPI capi, ITextureAtlasAPI targetAtlas, Shape shape, string filenameForLogging)
    {
        this.capi = capi;
        this.targetAtlas = targetAtlas;
        this.shape = shape;
        this.filenameForLogging = filenameForLogging;
    }

    public TextureAtlasPosition this[string textureCode]
    {
        get
        {
            TextureAtlasPosition texPos;

            if (textures.TryGetValue(textureCode, out var ctex))
            {
                targetAtlas.GetOrInsertTexture(ctex, out _, out texPos);
            }
            else
            {
                shape.Textures.TryGetValue(textureCode, out var texturePath);

                if (texturePath == null)
                {
                    if (!missingTextures.Contains(texturePath))
                    {
                        Core.GetInstance(capi).Mod.Logger.Warning("Shape {0} has an element using texture code {1}, but no such texture exists", filenameForLogging, textureCode);
                        missingTextures.Add(texturePath);
                    }

                    return targetAtlas.UnknownTexturePosition;
                }

                targetAtlas.GetOrInsertTexture(texturePath, out _, out texPos);
            }


            if (texPos == null)
            {
                return targetAtlas.UnknownTexturePosition;
            }

            if (firstTexPos == null)
            {
                firstTexPos = texPos;
            }

            return texPos;
        }
    }

    public Size2i AtlasSize => targetAtlas.Size;
}