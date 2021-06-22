// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Reflection;
using Celeste;
using GravityHelper.Triggers;

namespace GravityHelper.Entities
{
    public static class EntityHookChecker
    {
        private static readonly Type[] mandatory_types =
        {
            typeof(GravityField),
            typeof(GravityRefill),
            typeof(UpsideDownTalkComponentUI),
            typeof(UpsideDownWatchTower),
            typeof(GravityTrigger),
            typeof(SpawnGravityTrigger),
        };

        private static readonly Type[] optional_types =
        {
            typeof(GravitySpring),
        };

        public static bool IsHookRequiredForSession(Session session) =>
            session.MapData?.Levels?
                .Any(level => (level.Triggers?.Any(IsHookRequiredForEntityData) ?? false) ||
                              (level.Entities?.Any(IsHookRequiredForEntityData) ?? false)) ?? false;

        public static bool IsHookRequiredForEntityData(EntityData data)
        {
            // we only care about GravityHelper types
            if (!data.Name.StartsWith("GravityHelper"))
                return false;

            // handle mandatory types
            if (mandatory_types.Any(type => data.Name.Contains(type.Name)))
                return true;

            // handle optional types
            return optional_types.Any(type =>
            {
                if (!data.Name.Contains(type.Name)) return false;
                var method = type.GetMethod("RequiresHooks", BindingFlags.Public | BindingFlags.Static);
                return method != null && (bool)method.Invoke(null, new object[] {data});
            });
        }
    }
}
