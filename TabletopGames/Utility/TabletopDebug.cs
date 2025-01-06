using Vintagestory.API.MathTools;

namespace TabletopGames;

public static class TabletopDebug
{
    public static bool BoardDataDebugInfo = false;
    public static bool BoardParticleSelection = true;
    public static bool TagsDebugInfo = false;
    public static bool VariantsDebugInfo = false;

    public static Vec4f BoardSelectionColor = new Vec4f(0, 1, 1, 1); // Cyan color

    public static bool ItemRotations = false;
    public static Vec3f ItemRotationsVec = Vec3f.Zero;
}
