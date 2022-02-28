// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace Celeste.Mod.GravityHelper.Extensions
{
    public static class EntityDataExtensions
    {
        private const string mod_version_key = "modVersion";
        private const string plugin_version_key = "pluginVersion";

        public static Version ModVersion(this EntityData self) => self.ModVersion(new Version(1, 0));
        public static Version PluginVersion(this EntityData self) => self.PluginVersion(new Version(1, 0));
        public static Version ModVersion(this EntityData self, Version defaultValue) => self.Version(mod_version_key, defaultValue);
        public static Version PluginVersion(this EntityData self, Version defaultValue) => self.Version(plugin_version_key, defaultValue);

        public static Version Version(this EntityData self, string key, Version defaultValue = default) => self.TryVersion(key, out var value) ? value : defaultValue;

        public static bool TryVersion(this EntityData self, string key, out Version version)
        {
            var value = self.Attr(key, string.Empty) ?? string.Empty;
            return System.Version.TryParse(value, out version);
        }
    }
}
