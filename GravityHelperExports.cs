using System;
using MonoMod.ModInterop;

namespace Celeste.Mod.GravityHelper {
    [ModExportName("GravityHelper")]
    public static class GravityHelperExports {
        public static string GravityTypeFromInt(int gravityType) => ((GravityType) gravityType).ToString();

        public static int GravityTypeToInt(string name) =>
            (int) (Enum.TryParse<GravityType>(name, out var value) ? value : GravityType.Normal);

        public static int GetPlayerGravity() =>
            (int) (GravityHelperModule.PlayerComponent?.CurrentGravity ?? GravityType.Normal);

        public static void SetPlayerGravity(int gravityType, float momentumMultiplier) =>
            GravityHelperModule.PlayerComponent?.SetGravity((GravityType) gravityType, momentumMultiplier);

        public static bool IsPlayerInverted() => GravityHelperModule.ShouldInvertPlayer;
    }
}