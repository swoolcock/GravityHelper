// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace Celeste.Mod.GravityHelper.Hooks;

internal static class PufferHooks
{
    public static void Load()
    {
        // Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Puffer)} hooks...");
        // IL.Celeste.Puffer.Explode += Puffer_Explode;
    }

    public static void Unload()
    {
        // Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Puffer)} hooks...");
        // IL.Celeste.Puffer.Explode -= Puffer_Explode;
    }

    // private static void Puffer_Explode(ILContext il) => HookUtils.SafeHook(() =>
    // {
    //     var cursor = new ILCursor(il);
    //     if (!cursor.TryGotoNext(instr => instr.MatchCallvirt<Player>(nameof(Player.ExplodeLaunch))) ||
    //         !cursor.TryGotoPrev(MoveType.After, instr => instr.MatchLdfld<Entity>(nameof(Entity.Position))))
    //         throw new HookException("Couldn't find Entity.Position.");
    //
    //     cursor.Emit(OpCodes.Ldloc_1);
    //     cursor.EmitDelegate<Func<Vector2, Player, Vector2>>((v, p) =>
    //         !GravityHelperModule.ShouldInvertPlayer ? v : new Vector2(v.X, p.CenterY - (v.Y - p.CenterY)));
    // });
}
