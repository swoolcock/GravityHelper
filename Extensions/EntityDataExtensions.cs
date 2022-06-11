// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.GravityHelper.Extensions
{
    public static class EntityDataExtensions
    {
        private const string mod_version_key = "modVersion";
        private const string plugin_version_key = "pluginVersion";

        public static Version ModVersion(this EntityData self) => self.ModVersion(new Version(0, 1));
        public static Version PluginVersion(this EntityData self) => self.PluginVersion(new Version(0, 1));
        public static Version ModVersion(this EntityData self, Version defaultValue) => self.Version(mod_version_key, defaultValue);
        public static Version PluginVersion(this EntityData self, Version defaultValue) => self.Version(plugin_version_key, defaultValue);

        public static Version Version(this EntityData self, string key, Version defaultValue = default) => self.TryVersion(key, out var value) ? value : defaultValue;

        public static bool TryVersion(this EntityData self, string key, out Version version)
        {
            var value = self.Attr(key, string.Empty) ?? string.Empty;
            return System.Version.TryParse(value, out version);
        }

        public static bool TryAttr(this EntityData self, string key, out string value)
        {
            value = self.Attr(key);
            return self.Has(key);
        }

        public static bool TryFloat(this EntityData self, string key, out float value)
        {
            value = self.Float(key);
            return self.Has(key);
        }

        public static bool TryInt(this EntityData self, string key, out int value)
        {
            value = self.Int(key);
            return self.Has(key);
        }

        public static bool TryHexColor(this EntityData self, string key, out Color value)
        {
            value = self.HexColor(key);
            return self.Has(key);
        }

        public static bool TryBool(this EntityData self, string key, out bool value)
        {
            value = self.Bool(key);
            return self.Has(key);
        }

        public static bool TryEnum<TEnum>(this EntityData self, string key, out TEnum value)
            where TEnum : struct
        {
            value = self.Enum<TEnum>(key);
            return self.Has(key);
        }

        public static bool TryChar(this EntityData self, string key, out char value)
        {
            value = self.Char(key);
            return self.Has(key);
        }

        public static string NullableAttr(this EntityData self, string key)
        {
            var value = self.Attr(key, null);
            return string.IsNullOrEmpty(value) ? null : value;
        }

        public static float? NullableFloat(this EntityData self, string key) =>
            float.TryParse(self.Attr(key), out var value) ? value : null;

        public static int? NullableInt(this EntityData self, string key) =>
            int.TryParse(self.Attr(key), out var value) ? value : null;

        public static Color? NullableHexColor(this EntityData self, string key) =>
            string.IsNullOrWhiteSpace(self.Attr(key)) ? null : self.HexColor(key);

        public static TEnum? NullableEnum<TEnum>(this EntityData self, string key)
            where TEnum : struct =>
            string.IsNullOrWhiteSpace(self.Attr(key)) ? null : self.Enum<TEnum>(key);

        public static char? NullableChar(this EntityData self, string key) =>
            string.IsNullOrEmpty(self.Attr(key)) ? null : self.Char(key);
    }
}
