// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Extensions;
using MonoMod.ModInterop;

namespace Celeste.Mod.GravityHelper {
    [ModExportName("GravityHelper")]
    public static class GravityHelperExports {
        public static string GravityTypeFromInt(int gravityType) => ((GravityType) gravityType).ToString();

        public static int GravityTypeToInt(string name) =>
            (int) (Enum.TryParse<GravityType>(name, out var value) ? value : GravityType.Normal);

        public static int GetPlayerGravity() =>
            (int) (GravityHelperModule.PlayerComponent?.CurrentGravity ?? GravityType.Normal);

        public static int GetActorGravity(Actor actor) => (int)(actor?.GetGravity() ?? GravityType.Normal);

        public static void SetPlayerGravity(int gravityType, float momentumMultiplier) =>
            GravityHelperModule.PlayerComponent?.SetGravity((GravityType) gravityType, momentumMultiplier);

        public static void SetActorGravity(Actor actor, int gravityType, float momentumMultiplier) =>
            actor?.SetGravity((GravityType)gravityType, momentumMultiplier);

        public static bool IsPlayerInverted() => GravityHelperModule.ShouldInvertPlayer;

        public static bool IsActorInverted(Actor actor) => actor?.ShouldInvert() ?? false;
    }
}
