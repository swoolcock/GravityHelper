using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections;
using System.Reflection;

namespace Celeste.Mod.GravityHack {
    public class GravityHackModule : EverestModule {

        private static MethodInfo m_VirtualJoystick_set_Value = typeof(VirtualJoystick).GetProperty("Value").GetSetMethod(true);

        private static IDetour hook_Player_orig_Update;
        public override void Load() {
            IL.Celeste.Player.ctor += Player_ctor;
            //On.Celeste.Player.ctor += Player_ctor;

            IL.Celeste.Actor.IsRiding_JumpThru += Actor_IsRiding;
            IL.Celeste.Actor.IsRiding_Solid += Actor_IsRiding;
            IL.Celeste.Actor.MoveVExact += Actor_MoveVExact;
            On.Celeste.Actor.OnGround_int += Actor_OnGround_int;

            On.Celeste.Player.Update += Player_Update;
            hook_Player_orig_Update = new ILHook(typeof(Player).GetMethod("orig_Update"), Player_orig_Update);
            IL.Celeste.Player.ClimbUpdate += Player_ClimbUpdate;
            IL.Celeste.Player.ClimbHopBlockedCheck += Player_ClimbHopBlockedCheck;

            On.Celeste.Player.TransitionTo += Player_TransitionTo;
            //On.Celeste.Level.TransitionRoutine += Level_TransitionRoutine;
            IL.Celeste.Player.BeforeDownTransition += Player_BeforeDownTransition;
            //IL.Celeste.Player.BeforeUpTransition += Player_BeforeUpTransition;

            IL.Celeste.Solid.GetPlayerOnTop += Solid_GetPlayerOnTop;
            On.Celeste.Solid.MoveVExact += Solid_MoveVExact;
        }

        public override void Unload() {
            IL.Celeste.Player.ctor -= Player_ctor;
            //On.Celeste.Player.ctor -= Player_ctor;

            IL.Celeste.Actor.IsRiding_JumpThru -= Actor_IsRiding;
            IL.Celeste.Actor.IsRiding_Solid -= Actor_IsRiding;
            IL.Celeste.Actor.MoveVExact -= Actor_MoveVExact;
            On.Celeste.Actor.OnGround_int -= Actor_OnGround_int;

            On.Celeste.Player.Update -= Player_Update;
            hook_Player_orig_Update.Dispose();
            IL.Celeste.Player.ClimbUpdate -= Player_ClimbUpdate;
            IL.Celeste.Player.ClimbHopBlockedCheck -= Player_ClimbHopBlockedCheck;

            On.Celeste.Player.TransitionTo -= Player_TransitionTo;
            //On.Celeste.Level.TransitionRoutine -= Level_TransitionRoutine;
            IL.Celeste.Player.BeforeDownTransition -= Player_BeforeDownTransition;
            //IL.Celeste.Player.BeforeUpTransition -= Player_BeforeUpTransition;

            IL.Celeste.Solid.GetPlayerOnTop -= Solid_GetPlayerOnTop;
            On.Celeste.Solid.MoveVExact -= Solid_MoveVExact;
        }

        private void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode) {
            orig(self, position, spriteMode);
            self.Add(new TransitionListener() {
                OnOutBegin = () => {
                    Transitioning = true;
                },
                OnInEnd = () => {
                    Transitioning = false;
                }
            });
        }

        private void Player_ctor(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchNewobj<Hitbox>())) {
                cursor.EmitDelegate<Func<Hitbox, Hitbox>>(box => {
                    return new Hitbox(box.Width, box.Height, box.Position.X, -(box.Position.Y + box.Height));
                });
            }
        }

        private bool Actor_OnGround_int(On.Celeste.Actor.orig_OnGround_int orig, Actor self, int downCheck) {
            if (self is Player)
                downCheck = -downCheck;
            return orig(self, downCheck);
        }

        private void Actor_MoveVExact(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Actor, bool>>(a => a is Player && !SolidMoving && !Transitioning);
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Neg);
            cursor.Emit(OpCodes.Starg, 1);
        }

        private void Actor_IsRiding(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(instr => instr.MatchCall<Vector2>("op_Addition"));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Isinst, typeof(Player));
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.Emit(OpCodes.Call, typeof(Vector2).GetMethod("op_Subtraction"));
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
        }

        private void Player_Update(On.Celeste.Player.orig_Update orig, Player self) {
            float AimY = Input.Aim.Value.Y;
            int MoveY = Input.MoveY.Value;
            Input.MoveY.Value = -MoveY;
            m_VirtualJoystick_set_Value.Invoke(Input.Aim, new object[] { new Vector2(Input.Aim.Value.X, -AimY) });
            orig(self);
            Input.MoveY.Value = MoveY;
            m_VirtualJoystick_set_Value.Invoke(Input.Aim, new object[] { new Vector2(Input.Aim.Value.X, AimY) });
        }

        private void Player_orig_Update(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            // (Speed.Y >= 0f) -> (Speed.Y <= 0f)
            cursor.GotoNext(instr => instr.Match(OpCodes.Blt_Un_S));
            cursor.Next.OpCode = OpCodes.Bgt_Un_S;

            // (Position + Vector2.UnitY) -> (Position - Vector2.UnitY)
            cursor.GotoNext(instr => instr.MatchCall<Vector2>("op_Addition"));
            cursor.Emit(OpCodes.Call, typeof(Vector2).GetMethod("op_Subtraction"));
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
            cursor.GotoNext(instr => instr.MatchCall<Vector2>("op_Addition"));
            cursor.Emit(OpCodes.Call, typeof(Vector2).GetMethod("op_Subtraction"));
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);

            // Math.Min(base.Y, highestAirY) -> Math.Max(base.Y, highestAirY)
            cursor.GotoNext(instr => instr.MatchCall("System.Math", "Min"));
            (cursor.Next.Operand as MethodReference).Name = "Max";

            // (Position + Vector2.UnitY) -> (Position - Vector2.UnitY)
            cursor.GotoNext(instr => instr.MatchCall<Vector2>("op_Addition"));
            cursor.Emit(OpCodes.Call, typeof(Vector2).GetMethod("op_Subtraction"));
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
            cursor.GotoNext(instr => instr.MatchCall<Vector2>("op_Addition"));
            cursor.Emit(OpCodes.Call, typeof(Vector2).GetMethod("op_Subtraction"));
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
        }

        private void Player_ClimbUpdate(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instr => instr.MatchCall<Vector2>("get_UnitY") && 
                instr.Next.MatchCall<Vector2>("op_Subtraction"));
            cursor.Emit(OpCodes.Ldc_I4_1);
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.Emit(OpCodes.Call, typeof(Vector2).GetMethod("op_Addition"));
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
        }

        private void Player_ClimbHopBlockedCheck(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(instr => instr.MatchCall<Vector2>("op_Subtraction"));
            cursor.Emit(OpCodes.Ldc_I4_1);
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.Emit(OpCodes.Call, typeof(Vector2).GetMethod("op_Addition"));
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
        }

        private static bool Transitioning = false;
        private IEnumerator Level_TransitionRoutine(On.Celeste.Level.orig_TransitionRoutine orig, Level self, LevelData next, Vector2 direction) {
            Transitioning = true;
            IEnumerator origEnum = orig(self, next, direction);
            while (origEnum.MoveNext())
                yield return origEnum.Current;
            Transitioning = false;
        }

        private bool Player_TransitionTo(On.Celeste.Player.orig_TransitionTo orig, Player self, Vector2 target, Vector2 direction) {
            Transitioning = true;
            bool val = orig(self, target, direction);
            Transitioning = false;
            return val;
        }

        private void Player_BeforeDownTransition(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(instr => instr.MatchCall("System.Math", "Max"));
            (cursor.Next.Operand as MethodReference).Name = "Min";
        }

        private void Player_BeforeUpTransition(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instr => instr.MatchLdcR4(-105f));
            cursor.Emit(OpCodes.Neg);
        }

        private void Solid_GetPlayerOnTop(ILContext il) {
            ILCursor cursor = new ILCursor(il);
            cursor.GotoNext(instr => instr.MatchCall<Vector2>("op_Subtraction"));
            cursor.Emit(OpCodes.Ldc_I4_1);
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.Emit(OpCodes.Call, typeof(Vector2).GetMethod("op_Addition"));
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
        }

        private static bool SolidMoving = false;
        private void Solid_MoveVExact(On.Celeste.Solid.orig_MoveVExact orig, Solid self, int move) {
            SolidMoving = true;
            orig(self, move);
            SolidMoving = false;
        }

    }
}
