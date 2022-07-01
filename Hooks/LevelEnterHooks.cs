// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections;

namespace Celeste.Mod.GravityHelper.Hooks
{
    public static class LevelEnterHooks
    {
        private static bool _hasVvvvvv;
        private static Postcard _vvvvvvPostcard;

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
                self.Add(_vvvvvvPostcard = new Postcard(Dialog.Get("GRAVITYHELPER_POSTCARD_VVVVVV"),
                    "event:/ui/main/postcard_csides_in", "event:/ui/main/postcard_csides_out"));
                yield return _vvvvvvPostcard.DisplayRoutine();
                _vvvvvvPostcard = null;
            }

            yield return new SwapImmediately(orig(self));
        }

        private static void LevelEnter_Go(On.Celeste.LevelEnter.orig_Go orig, Session session, bool fromsavedata)
        {
            checkForVvvvvv(session);
            orig(session, fromsavedata);
        }

        private static void checkForVvvvvv(Session session)
        {
            if (AreaData.Areas.Count <= session.Area.ID ||
                AreaData.Areas[session.Area.ID].Mode.Length <= (int)session.Area.Mode ||
                AreaData.Areas[session.Area.ID].Mode[(int)session.Area.Mode] == null)
                return;

            _hasVvvvvv = session.MapData.Levels.Exists(levelData => levelData.Entities.Exists(e => e.Name == "GravityHelper/VvvvvvGravityController"));
        }
    }
}
