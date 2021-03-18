using System.Linq;
using System.Reflection;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;

namespace GravityHelper
{
    internal static class ReflectionCache
    {
        public static readonly FieldInfo NormalHitboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "normalHitbox");
        public static readonly FieldInfo NormalHurtboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "normalHurtbox");
        public static readonly FieldInfo DuckHitboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "duckHitbox");
        public static readonly FieldInfo DuckHurtboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "duckHurtbox");
        public static readonly FieldInfo StarFlyHitboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "starFlyHitbox");
        public static readonly FieldInfo StarFlyHurtboxFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "starFlyHurtbox");
        public static readonly FieldInfo VarJumpTimerFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "varJumpTimer");
        public static readonly FieldInfo VarJumpSpeedFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "varJumpSpeed");
        public static readonly FieldInfo DashCooldownTimerFieldInfo = typeof(Player).GetRuntimeFields().First(f => f.Name == "dashCooldownTimer");
        public static readonly FieldInfo PlayerCanUseFieldInfo = typeof(Spring).GetRuntimeFields().First(f => f.Name == "playerCanUse");
        public static readonly FieldInfo BumperRespawnTimer = typeof(Bumper).GetRuntimeFields().First(f => f.Name == "respawnTimer");

        public static readonly MethodInfo VirtualJoystickSetValueMethodInfo = typeof(VirtualJoystick).GetProperty("Value")?.GetSetMethod(true);
        public static readonly MethodInfo UpdateSpriteMethodInfo = typeof(Player).GetRuntimeMethods().First(m => m.Name == "orig_UpdateSprite");
        public static readonly MethodInfo PlayerOrigUpdateMethodInfo = typeof(Player).GetMethod(nameof(Player.orig_Update));
        public static readonly MethodInfo LevelOrigTransitionRoutineMethodInfo = typeof(Level).GetRuntimeMethods().First(m => m.Name == "orig_TransitionRoutine");
        public static readonly MethodInfo PlayerDashCoroutineMethodInfo = typeof(Player).GetMethod("DashCoroutine", BindingFlags.NonPublic | BindingFlags.Instance);

        public static readonly object[] VirtualJoystickSetValueParams = { Vector2.Zero };
    }
}