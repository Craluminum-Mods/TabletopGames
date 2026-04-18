using HarmonyLib;
using System.Collections.Generic;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace TabletopGames;

[HarmonyPatchCategory("Client")]
[HarmonyPatch(typeof(BlockEntityKnappingSurface), nameof(BlockEntityKnappingSurface.RegenMeshAndSelectionBoxes))]
public static class RegenMeshAndSelectionBoxesPatch
{
    [HarmonyPostfix]
    public static void Postfix(BlockEntityKnappingSurface __instance, ref Cuboidf[] ___selectionBoxes, ref KnappingRenderer ___workitemRenderer)
    {
        if (___workitemRenderer != null && __instance.BaseMaterial != null)
        {
            __instance.BaseMaterial.ResolveBlockOrItem(__instance.Api.World);

            string firstCodePart = __instance.BaseMaterial.Collectible.FirstCodePart();
            string secondCodePart = __instance.BaseMaterial.Collectible.FirstCodePart(1);
            string thirdCodePart = __instance.BaseMaterial.Collectible.FirstCodePart(2);
            if (secondCodePart != null)
            {
                if (thirdCodePart != null)
                {
                    ___workitemRenderer.Material = $"{secondCodePart}-{thirdCodePart}";
                }
                else
                {
                    ___workitemRenderer.Material = secondCodePart;
                }
            }
            else
            {
                ___workitemRenderer.Material = firstCodePart;
            }

            ___workitemRenderer.RegenMesh(__instance.Voxels, __instance.SelectedRecipe);
        }
        List<Cuboidf> boxes = new List<Cuboidf>();
        for (int x = 0; x < 16; x++)
        {
            for (int z = 0; z < 16; z++)
            {
                boxes.Add(new Cuboidf((float)x / 16f, 0f, (float)z / 16f, (float)x / 16f + 0.0625f, 0.0625f, (float)z / 16f + 0.0625f));
            }
        }
        ___selectionBoxes = boxes.ToArray();
    }
}