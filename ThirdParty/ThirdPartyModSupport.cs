// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Celeste.Mod.GravityHelper.ThirdParty
{
    public abstract class ThirdPartyModSupport : IDisposable
    {
        // ReSharper disable once InconsistentNaming
        public static readonly List<string> BlacklistedMods = new();

        private bool _loaded;

        public ThirdPartyModAttribute Attribute =>
            GetType().GetCustomAttribute<ThirdPartyModAttribute>(true);

        public EverestModule Module =>
            Everest.Modules.FirstOrDefault(m => m.Metadata.Name == Attribute.Name);

        public bool TryLoad()
        {
            var attr = Attribute;

            if (BlacklistedMods.Contains(attr.Name))
            {
                Logger.Log(LogLevel.Info, nameof(GravityHelperModule), $"{attr.Name} is blacklisted, skipping.");
                return false;
            }

            if (_loaded)
            {
                Logger.Log(LogLevel.Info, nameof(GravityHelperModule), $"{attr.Name} already loaded, skipping.");
                return false;
            }

            var module = Module;

            if (module == null)
            {
                Logger.Log(nameof(GravityHelperModule), $"{attr.Name} not found, skipping.");
                return false;
            }

            if (Version.TryParse(attr.MinimumVersion ?? string.Empty, out var minVersion) && module.Metadata.Version < minVersion)
            {
                Logger.Log(LogLevel.Info, nameof(GravityHelperModule), $"{module.Metadata.Name} ({module.Metadata.VersionString}) is less than minimum version {minVersion}, skipping.");
                return false;
            }

            if (Version.TryParse(attr.MaximumVersion ?? string.Empty, out var maxVersion) && module.Metadata.Version > maxVersion)
            {
                Logger.Log(LogLevel.Info, nameof(GravityHelperModule), $"{module.Metadata.Name} ({module.Metadata.VersionString}) is greater than maximum version {minVersion}, skipping.");
                return false;
            }

            try
            {
                Logger.Log(LogLevel.Info, nameof(GravityHelperModule), $"Loading mod support for {module.Metadata.Name} ({module.Metadata.Version})...");
                Load();
            }
            catch (Exception)
            {
                Logger.Log(LogLevel.Error, nameof(GravityHelperModule), $"Exception loading mod support for {module.Metadata.Name} ({module.Metadata.Version}).");
                throw;
            }

            _loaded = true;

            return true;
        }

        public bool TryUnload()
        {
            var attr = Attribute;

            if (!_loaded)
            {
                Logger.Log(nameof(GravityHelperModule), $"{attr.Name} not yet loaded, skipping.");
                return false;
            }

            var module = Module;

            try
            {
                Logger.Log(LogLevel.Info, nameof(GravityHelperModule), $"Unloading mod support for {module.Metadata.Name} ({module.Metadata.Version})...");
                Unload();
            }
            catch (Exception)
            {
                Logger.Log(LogLevel.Error, nameof(GravityHelperModule), $"Exception unloading mod support for {module.Metadata.Name} ({module.Metadata.Version}).");
                throw;
            }

            _loaded = false;

            return true;
        }

        protected abstract void Load();
        protected abstract void Unload();

        protected virtual void Dispose(bool disposing)
        {
            if (_loaded)
            {
                TryUnload();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
