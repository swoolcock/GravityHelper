using Celeste;
using Microsoft.Xna.Framework;
using System.Reflection;

namespace GravityHelper
{
    internal static class ReflectionCache
    {
        public static PropertyInfo Player_OnSafeGround = typeof(Player).GetProperty("OnSafeGround");
        public static MethodInfo Player_UnDuck = typeof(Player).GetMethod("UnDuck", BindingFlags.Instance | BindingFlags.NonPublic);
        public static FieldInfo Player_OnGround = typeof(Player).GetField("onGround", BindingFlags.Instance | BindingFlags.NonPublic);
        public static FieldInfo Vector2_unitYVector = typeof(Vector2).GetField("unitYVector", BindingFlags.Static | BindingFlags.NonPublic);
        public static FieldInfo Booster_respawnTimer = typeof(Booster).GetField("respawnTimer", BindingFlags.Instance | BindingFlags.NonPublic);
    }
}
