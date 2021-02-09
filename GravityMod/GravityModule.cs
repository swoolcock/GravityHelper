using System;

namespace Celeste.Mod.Gravity {
    public class GravityModule : EverestModule {

        public static GravityModule Instance;

        public override Type SettingsType => typeof(GravityModuleSettings);
        public static GravityModuleSettings Settings => (GravityModuleSettings)Instance._Settings;

        public GravityModule() {
            Instance = this;
        }

        public override void Load() {
            On.Celeste.Player.Render += RenderPlayer;
            On.Celeste.PlayerSprite.Render += RenderPlayerSprite;
        }

        public override void Unload() {
            On.Celeste.PlayerSprite.Render -= RenderPlayerSprite;
            On.Celeste.Player.Render -= RenderPlayer;
        }

        public static void RenderPlayer(On.Celeste.Player.orig_Render orig, Player self) {
            orig(self);
        }

        public static void RenderPlayerSprite(On.Celeste.PlayerSprite.orig_Render orig, PlayerSprite self) {
            orig(self);
        }
    }
}
