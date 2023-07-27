// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections;
using System.Linq;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class LevelEnterHooks
    {
        private static bool _hasGravityHelper;
        private static bool _hasVvvvvv;
        private static bool _hasMHHUDJT;

        private static Postcard _vvvvvvPostcard;
        private static Postcard _udjtCorrectionPostcard;

        public static void Load()
        {
            Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(LevelEnter)} hooks...");
            On.Celeste.LevelEnter.Go += LevelEnter_Go;
            On.Celeste.LevelEnter.Routine += LevelEnter_Routine;
            On.Celeste.LevelEnter.BeforeRender += LevelEnter_BeforeRender;
        }

        public static void Unload()
        {
            Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(LevelEnter)} hooks...");
            On.Celeste.LevelEnter.Go -= LevelEnter_Go;
            On.Celeste.LevelEnter.Routine -= LevelEnter_Routine;
            On.Celeste.LevelEnter.BeforeRender -= LevelEnter_BeforeRender;
        }

        private static void LevelEnter_BeforeRender(On.Celeste.LevelEnter.orig_BeforeRender orig, LevelEnter self)
        {
            orig(self);
            _vvvvvvPostcard?.BeforeRender();
            _udjtCorrectionPostcard?.BeforeRender();
        }

        private static IEnumerator LevelEnter_Routine(On.Celeste.LevelEnter.orig_Routine orig, LevelEnter self)
        {
            if (_hasVvvvvv &&
                (GravityHelperModule.Settings.VvvvvvMode != GravityHelperModuleSettings.VvvvvvSetting.Default ||
                GravityHelperModule.Settings.VvvvvvAllowDashing != GravityHelperModuleSettings.VvvvvvSetting.Default ||
                GravityHelperModule.Settings.VvvvvvAllowGrabbing != GravityHelperModuleSettings.VvvvvvSetting.Default ||
                GravityHelperModule.Settings.VvvvvvFlipSound != GravityHelperModuleSettings.VvvvvvSetting.Default))
            {
                GravityHelperModule.Settings.VvvvvvMode = GravityHelperModuleSettings.VvvvvvSetting.Default;
                GravityHelperModule.Settings.VvvvvvAllowDashing = GravityHelperModuleSettings.VvvvvvSetting.Default;
                GravityHelperModule.Settings.VvvvvvAllowGrabbing = GravityHelperModuleSettings.VvvvvvSetting.Default;
                GravityHelperModule.Settings.VvvvvvFlipSound = GravityHelperModuleSettings.VvvvvvSetting.Default;
                self.Add(_vvvvvvPostcard = new Postcard(Dialog.Clean("GRAVITYHELPER_POSTCARD_VVVVVV"),
                    "event:/ui/main/postcard_csides_in", "event:/ui/main/postcard_csides_out"));
                yield return _vvvvvvPostcard.DisplayRoutine();
                _vvvvvvPostcard = null;
            }

            if (!_hasGravityHelper && _hasMHHUDJT && GravityHelperModule.Settings.AllowInAllMaps)
            {
                self.Add(_udjtCorrectionPostcard = new Postcard(Dialog.Clean("GRAVITYHELPER_POSTCARD_UDJT_CORRECTION"),
                    "event:/ui/main/postcard_csides_in", "event:/ui/main/postcard_csides_out"));
                yield return _udjtCorrectionPostcard.DisplayRoutine();
                _udjtCorrectionPostcard = null;
            }

            yield return new SwapImmediately(orig(self));
        }

        private static void LevelEnter_Go(On.Celeste.LevelEnter.orig_Go orig, Session session, bool fromsavedata)
        {
            var found = checkForEntities(session, "GravityHelper/", "GravityHelper/VvvvvvGravityController", "MaxHelpingHand/UpsideDownJumpThru");
            _hasGravityHelper = found[0];
            _hasVvvvvv = found[1];
            _hasMHHUDJT = found[2];

            orig(session, fromsavedata);
        }

        private static bool[] checkForEntities(Session session, params string[] entityNames)
        {
            var rv = new bool[entityNames.Length];
            if (AreaData.Areas.Count <= session.Area.ID ||
                AreaData.Areas[session.Area.ID].Mode.Length <= (int)session.Area.Mode ||
                AreaData.Areas[session.Area.ID].Mode[(int)session.Area.Mode] == null)
                return rv;

            // check whether the requested entity names are just mod names
            var wildcards = entityNames.Select(n => n.EndsWith("/")).ToArray();

            // loop through (potentially) every entity in the map
            int found = 0;
            foreach (var e in session.MapData.Levels.SelectMany(levelData => levelData.Entities.Concat(levelData.Triggers)))
            {
                // check the entity names or mods we're looking for
                for (int i = 0; i < entityNames.Length; i++)
                {
                    if (!rv[i] && (wildcards[i] ? e.Name.StartsWith(entityNames[i]) : e.Name == entityNames[i]))
                    {
                        rv[i] = true;
                        found++;
                    }
                }
                // if we've found all of them at least once, we don't need to check any more
                if (found >= entityNames.Length)
                    break;
            }

            return rv;
        }
    }
}
