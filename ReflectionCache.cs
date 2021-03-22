using System;
using System.Linq;
using System.Reflection;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;

namespace GravityHelper
{
    internal static class ReflectionCache
    {
        public static Type GetTypeByName(string name) =>
            AppDomain.CurrentDomain.GetAssemblies().Reverse().Select(a => a.GetType(name)).FirstOrDefault(t => t != null);

        public static readonly FieldInfo NormalHitboxFieldInfo = typeof(Player).GetField("normalHitbox", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo NormalHurtboxFieldInfo = typeof(Player).GetField("normalHurtbox", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo DuckHitboxFieldInfo = typeof(Player).GetField("duckHitbox", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo DuckHurtboxFieldInfo = typeof(Player).GetField("duckHurtbox", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo StarFlyHitboxFieldInfo = typeof(Player).GetField("starFlyHitbox", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo StarFlyHurtboxFieldInfo = typeof(Player).GetField("starFlyHurtbox", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo VarJumpTimerFieldInfo = typeof(Player).GetField("varJumpTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo VarJumpSpeedFieldInfo = typeof(Player).GetField("varJumpSpeed", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo DashCooldownTimerFieldInfo = typeof(Player).GetField("dashCooldownTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo PlayerCanUseFieldInfo = typeof(Spring).GetField("playerCanUse", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo BumperRespawnTimer = typeof(Bumper).GetField("respawnTimer", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo PlayerLastClimbMoveFieldInfo = typeof (Player).GetField("lastClimbMove", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo ActorMovementCounterFieldInfo = typeof (Actor).GetField("movementCounter", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly MethodInfo VirtualJoystickSetValueMethodInfo = typeof(VirtualJoystick).GetProperty("Value")?.GetSetMethod(true);
        public static readonly MethodInfo PlayerOrigUpdateSpriteMethodInfo = typeof(Player).GetMethod("orig_UpdateSprite", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly MethodInfo PlayerOrigWallJumpMethodInfo = typeof(Player).GetMethod("orig_WallJump", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly MethodInfo PlayerOrigUpdateMethodInfo = typeof(Player).GetMethod(nameof(Player.orig_Update));
        public static readonly MethodInfo PlayerDashCoroutineMethodInfo = typeof(Player).GetMethod("DashCoroutine", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly MethodInfo LevelNextLevelMethodInfo = typeof(Level).GetMethod("NextLevel", BindingFlags.Instance | BindingFlags.NonPublic);

        public static readonly object[] VirtualJoystickSetValueParams = { Vector2.Zero };

        // optional dependencies

        private static Type upsideDownJumpThruType;
        public static Type UpsideDownJumpThruType => upsideDownJumpThruType ??= GetTypeByName("Celeste.Mod.MaxHelpingHand.Entities.UpsideDownJumpThru");
    }
}