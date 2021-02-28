using System.Reflection;
using Celeste;
using Microsoft.Xna.Framework;

namespace GravityHelper
{
    // ReSharper disable InconsistentNaming
    internal static class ReflectionCache
    {
        public static readonly PropertyInfo Player_OnSafeGround = typeof(Player).GetProperty("OnSafeGround");
        public static readonly MethodInfo Player_UnDuck = typeof(Player).GetMethod("UnDuck", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo Player_OnGround = typeof(Player).GetField("onGround", BindingFlags.Instance | BindingFlags.NonPublic);
        public static readonly FieldInfo Vector2_unitYVector = typeof(Vector2).GetField("unitYVector", BindingFlags.Static | BindingFlags.NonPublic);
        public static readonly FieldInfo Booster_respawnTimer = typeof(Booster).GetField("respawnTimer", BindingFlags.Instance | BindingFlags.NonPublic);
    }
}
