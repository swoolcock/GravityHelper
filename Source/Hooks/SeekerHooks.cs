// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Celeste.Mod.GravityHelper.Hooks;

internal static class SeekerHooks
{
    public static void Load()
    {
        Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Seeker)} hooks...");

        On.Celeste.Seeker.Update += Seeker_Update;
    }

    public static void Unload()
    {
        Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Seeker)} hooks...");

        On.Celeste.Seeker.Update -= Seeker_Update;
    }

    private static void Seeker_Update(On.Celeste.Seeker.orig_Update orig, Seeker self)
    {
        var bounceHitbox = self.bounceHitbox;
        var attackHitbox = self.attackHitbox;

        if (GravityHelperModule.ShouldInvertPlayer != bounceHitbox.Top > attackHitbox.Top)
        {
            bounceHitbox.Top = -bounceHitbox.Bottom;
            attackHitbox.Top = -attackHitbox.Bottom;
        }

        orig(self);
    }
}
