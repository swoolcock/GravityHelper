using Microsoft.Xna.Framework;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class PlayerDeadBodyHooks
    {
        private static IDetour hook_PlayerDeadBody_DeathRoutine;

        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(PlayerDeadBody)} hooks...");

            IL.Celeste.PlayerDeadBody.Render += PlayerDeadBody_Render;
            hook_PlayerDeadBody_DeathRoutine = new ILHook(ReflectionCache.PlayerDeadBody_DeathRoutine.GetStateMachineTarget(), PlayerDeadBody_DeathRoutine);
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(PlayerDeadBody)} hooks...");

            IL.Celeste.PlayerDeadBody.Render -= PlayerDeadBody_Render;
            hook_PlayerDeadBody_DeathRoutine?.Dispose();
            hook_PlayerDeadBody_DeathRoutine = null;
        }

        private static void PlayerDeadBody_DeathRoutine(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // playerDeadBody1.deathEffect = new DeathEffect(playerDeadBody1.initialHairColor, new Vector2?(playerDeadBody1.Center - playerDeadBody1.Position));
            cursor.GotoNext(instr => instr.MatchLdfld<PlayerDeadBody>("initialHairColor"));
            cursor.GotoNextSubtraction(MoveType.After);
            cursor.EmitInvertVectorDelegate();

            // playerDeadBody1.Position = playerDeadBody1.Position + Vector2.UnitY * -5f;
            cursor.GotoPrev(MoveType.After, instr => instr.MatchLdcR4(-5));
            cursor.EmitInvertFloatDelegate();
        });

        private static void PlayerDeadBody_Render(ILContext il) => HookUtils.SafeHook(() =>
        {
            var cursor = new ILCursor(il);

            // this.sprite.Scale.Y = this.scale;
            cursor.GotoNext(instr => instr.MatchStfld<Vector2>(nameof(Vector2.Y)));
            cursor.EmitInvertFloatDelegate();
        });
    }
}