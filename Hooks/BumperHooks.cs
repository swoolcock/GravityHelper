using System;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;

// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class BumperHooks
    {
        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Bumper)} hooks...");
            IL.Celeste.Bumper.OnPlayer += Bumper_OnPlayer;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Bumper)} hooks...");
            IL.Celeste.Bumper.OnPlayer -= Bumper_OnPlayer;
        }

        private static void Bumper_OnPlayer(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(instr => instr.MatchCallvirt<Player>(nameof(Player.ExplodeLaunch)));
            cursor.GotoPrev(MoveType.After, instr => instr.MatchLdfld<Entity>(nameof(Entity.Position)));

            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<Vector2, Player, Vector2>>((v, p) =>
            {
                if (!GravityHelperModule.ShouldInvert) return v;
                return new Vector2(v.X, p.CenterY - (v.Y - p.CenterY));
            });
        });
    }
}
