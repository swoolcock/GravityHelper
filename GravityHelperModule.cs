using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;

namespace GravityHelper
{
    // ReSharper disable InconsistentNaming
    public class GravityHelperModule : EverestModule
    {
        public static GravityHelperModule Instance;

        public GravityHelperModule()
        {
            Instance = this;
        }

        public override Type SettingsType => typeof(GravityHelperModuleSettings);

        public static GravityHelperModuleSettings Settings => (GravityHelperModuleSettings)Instance._Settings;

        private static readonly MethodInfo m_VirtualJoystick_set_Value = typeof(VirtualJoystick).GetProperty("Value")?.GetSetMethod(true);

        private static IDetour hook_Player_orig_Update;

        private static GravityType gravity;

        public static GravityType Gravity {
            get => gravity;
            set {
                if (gravity == value)
                    return;

                gravity = value;
                GravityChanged?.Invoke(value);
            }
        }

        public static event Action<GravityType> GravityChanged;

        private static bool transitioning;
        private static bool solidMoving;

        public override void Load()
        {
            // return;
            // IL.Celeste.Player.ctor += Player_ctor;
            On.Celeste.Player.ctor += Player_ctor;

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

            On.Celeste.Level.Update += LevelOnUpdate;

            GravityChanged += type => {
                var player = Engine.Scene.Entities.FindFirst<Player>();
                if (player == null)
                    return;

                updateHitboxes(player);
            };
        }

        public override void Unload()
        {
            // return;
            // IL.Celeste.Player.ctor -= Player_ctor;
            On.Celeste.Player.ctor -= Player_ctor;

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

            On.Celeste.Level.Update -= LevelOnUpdate;
        }

        private static void updateHitboxes(Player player) {
            var normalHitboxFI = typeof(Player).GetRuntimeFields().First(f => f.Name == "normalHitbox");
            var duckHitboxFI = typeof(Player).GetRuntimeFields().First(f => f.Name == "duckHitbox");
            var normalHurtboxFI = typeof(Player).GetRuntimeFields().First(f => f.Name == "normalHurtbox");
            var duckHurtboxFI = typeof(Player).GetRuntimeFields().First(f => f.Name == "duckHurtbox");
            var normalHitbox = (Hitbox)normalHitboxFI.GetValue(player);
            var duckHitbox = (Hitbox)duckHitboxFI.GetValue(player);
            var normalHurtbox = (Hitbox)normalHurtboxFI.GetValue(player);
            var duckHurtbox = (Hitbox)duckHurtboxFI.GetValue(player);

            if (Gravity == GravityType.Inverted && normalHitbox.Top < -1 || Gravity == GravityType.Normal && normalHitbox.Bottom > 1) {
                player.Position.Y = Gravity == GravityType.Inverted ? normalHitbox.AbsoluteTop : normalHitbox.AbsoluteBottom;
                normalHitbox.Position.Y = -normalHitbox.Position.Y - normalHitbox.Height;
                duckHitbox.Position.Y = -duckHitbox.Position.Y - duckHitbox.Height;
                normalHurtbox.Position.Y = -normalHurtbox.Position.Y - normalHurtbox.Height;
                duckHurtbox.Position.Y = -duckHurtbox.Position.Y - duckHurtbox.Height;
            }
        }

        #region On Hooks

        private void LevelOnUpdate(On.Celeste.Level.orig_Update orig, Level self)
        {
            if (Settings.ToggleInvertGravity.Pressed)
            {
                Settings.ToggleInvertGravity.ConsumePress();
                Gravity = Gravity == GravityType.Normal ? GravityType.Inverted : GravityType.Normal;
            }

            orig(self);
        }

        private static void Solid_MoveVExact(On.Celeste.Solid.orig_MoveVExact orig, Solid self, int move)
        {
            if (Gravity == GravityType.Normal)
            {
                orig(self, move);
                return;
            }

            solidMoving = true;
            orig(self, move);
            solidMoving = false;
        }

        private static void Player_ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode)
        {
            orig(self, position, spriteMode);

            updateHitboxes(self);

            self.Add(new TransitionListener
            {
                OnOutBegin = () => transitioning = true,
                OnInEnd = () => transitioning = false
            });
        }

        private static bool Actor_OnGround_int(On.Celeste.Actor.orig_OnGround_int orig, Actor self, int downCheck) =>
            orig(self, Gravity == GravityType.Inverted && self is Player ? -downCheck : downCheck);

        private static void Player_Update(On.Celeste.Player.orig_Update orig, Player self)
        {
            if (Gravity == GravityType.Normal) {
                orig(self);
                return;
            }

            float aimY = Input.Aim.Value.Y;
            int moveY = Input.MoveY.Value;
            Input.MoveY.Value = -moveY;
            m_VirtualJoystick_set_Value.Invoke(Input.Aim, new object[] { new Vector2(Input.Aim.Value.X, -aimY) });
            orig(self);
            Input.MoveY.Value = moveY;
            m_VirtualJoystick_set_Value.Invoke(Input.Aim, new object[] { new Vector2(Input.Aim.Value.X, aimY) });
        }

        private static IEnumerator Level_TransitionRoutine(On.Celeste.Level.orig_TransitionRoutine orig, Level self, LevelData next, Vector2 direction)
        {
            transitioning = true;
            IEnumerator origEnum = orig(self, next, direction);
            while (origEnum.MoveNext())
                yield return origEnum.Current;
            transitioning = false;
        }

        private static bool Player_TransitionTo(On.Celeste.Player.orig_TransitionTo orig, Player self, Vector2 target, Vector2 direction)
        {
            transitioning = true;
            bool val = orig(self, target, direction);
            transitioning = false;
            return val;
        }

        #endregion

        #region IL Hooks

        // private static void Player_ctor(ILContext il)
        // {
        //     var cursor = new ILCursor(il);
        //
        //     while (cursor.TryGotoNext(MoveType.After, instr => instr.MatchNewobj<Hitbox>()))
        //         cursor.EmitDelegate<Func<Hitbox, Hitbox>>(box =>
        //             new Hitbox(box.Width, box.Height, box.Position.X, -(box.Position.Y + box.Height)));
        // }

        private static void Actor_MoveVExact(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Actor, bool>>(a => a is Player && !solidMoving && !transitioning && Gravity == GravityType.Inverted);
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Neg);
            cursor.Emit(OpCodes.Starg, 1);
        }

        private static void Actor_IsRiding(ILContext il)
        {
            replaceAdditionWithDelegate(new ILCursor(il));
            // var cursor = new ILCursor(il);
            // cursor.GotoNext(instr => instr.MatchCall<Vector2>("op_Addition"));
            // cursor.Emit(OpCodes.Ldarg_0);
            // cursor.Emit(OpCodes.Isinst, typeof(Player));
            // cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            // cursor.Emit(OpCodes.Call, typeof(Vector2).GetMethod("op_Subtraction"));
            // cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
        }

        private static void Player_orig_Update(ILContext il)
        {
            var cursor = new ILCursor(il);
            // (Speed.Y >= 0f) -> (Speed.Y <= 0f)
            cursor.GotoNext(instr => instr.Match(OpCodes.Blt_Un_S));
            cursor.Next.OpCode = OpCodes.Bgt_Un_S;

            // (Position + Vector2.UnitY) -> (Position - Vector2.UnitY)
            replaceAdditionWithDelegate(cursor);
            replaceAdditionWithDelegate(cursor);

            // Math.Min(base.Y, highestAirY) -> Math.Max(base.Y, highestAirY)
            // cursor.GotoNext(instr => instr.MatchCall("System.Math", "Min"));
            // if (cursor.Next.Operand is MethodReference methodReference)
            //     methodReference.Name = "Max";
            replaceMaxWithDelegate(cursor);

            // (Position + Vector2.UnitY) -> (Position - Vector2.UnitY)
            replaceAdditionWithDelegate(cursor);
            replaceAdditionWithDelegate(cursor);
        }

        private static void Player_ClimbUpdate(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, instr =>
                instr.MatchCall<Vector2>("get_UnitY") && instr.Next.MatchCall<Vector2>("op_Subtraction"));
            cursor.Emit(OpCodes.Ldc_I4_1);
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.Emit(OpCodes.Call, typeof(Vector2).GetMethod("op_Addition"));
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
            // cursor.GotoNext();
            // cursor.Remove();
            // cursor.Emit(OpCodes.Call, typeof(GravityHelperModule).GetMethod(nameof(subtractToAdd)));
        }

        private static void Player_ClimbHopBlockedCheck(ILContext il)
        {
            // if (!inverted) return;

            var cursor = new ILCursor(il);
            cursor.GotoNext(instr => instr.MatchCall<Vector2>("op_Subtraction"));
            cursor.Emit(OpCodes.Ldc_I4_1);
            cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
            cursor.Emit(OpCodes.Call, typeof(Vector2).GetMethod("op_Addition"));
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
        }

        private static void Player_BeforeDownTransition(ILContext il) => replaceMaxWithDelegate(new ILCursor(il));

        private static void Player_BeforeUpTransition(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(instr => instr.MatchLdcR4(-105f));
            // cursor.Remove();
            cursor.EmitDelegate<Func<float>>(() => Gravity == GravityType.Normal ? -105f : 105f);
            cursor.Emit(OpCodes.Br_S, cursor.Next.Next);

            // cursor.GotoNext(MoveType.After, instr => instr.MatchLdcR4(-105f));
            // cursor.Emit(OpCodes.Neg);
        }

        private static void Solid_GetPlayerOnTop(ILContext il) => replaceSubtractionWithDelegate(new ILCursor(il));

        #endregion

        #region IL Helpers

        private delegate Vector2 VectorBinaryOperation(Vector2 lhs, Vector2 rhs);

        private delegate float FloatMathMinMax(float a, float b);

        private static void replaceAdditionWithDelegate(ILCursor cursor)
        {
            cursor.GotoNext(instr => instr.MatchCall<Vector2>("op_Addition"));
            cursor.Remove();
            cursor.EmitDelegate<VectorBinaryOperation>((lhs, rhs) => Gravity == GravityType.Inverted ? lhs - rhs : lhs + rhs);
            // cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
        }

        private static void replaceSubtractionWithDelegate(ILCursor cursor)
        {
            cursor.GotoNext(instr => instr.MatchCall<Vector2>("op_Subtraction"));
            cursor.Remove();
            cursor.EmitDelegate<VectorBinaryOperation>((lhs, rhs) => Gravity == GravityType.Inverted ? lhs + rhs : lhs - rhs);
            // cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
        }

        private static void replaceMaxWithDelegate(ILCursor cursor)
        {
            cursor.GotoNext(instr => instr.MatchCall("System.Math", "Max"));
            cursor.Remove();
            cursor.EmitDelegate<FloatMathMinMax>((a, b) => Gravity == GravityType.Inverted ? Math.Min(a, b) : Math.Max(a, b));
            // cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
        }

        #endregion
    }
}
