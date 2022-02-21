// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace Celeste.Mod.GravityHelper.Extensions
{
    public static class EntityDataExtensions
    {
        public static Version ModVersion(this EntityData self) => self?.Version("modVersion");
        public static Version PluginVersion(this EntityData self) => self?.Version("pluginVersion");

        public static Version Version(this EntityData self, string key) => self != null && self.TryVersion(key, out var value) ? value : null;

        public static bool TryVersion(this EntityData self, string key, out Version version)
        {
            var value = self?.Attr(key, string.Empty) ?? string.Empty;
            return System.Version.TryParse(value, out version);
        }
    }
}
