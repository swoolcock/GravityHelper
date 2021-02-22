using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper
{
    public class GravityHelperModule : EverestModule
    {
        [Command("gravity", "[Gravity Helper] Sets the gravity:\n 0 -> Normal\n 1 -> Inverted")]
        public static void CmdSetGravity(int type)
        {
            if (Engine.Scene is Level && type < 2)
            {
                Instance.Gravity = (GravityTypes)type;
                PlayerHooks.lastGrav = (GravityTypes)type;
            }
        }

        public static GravityHelperModule Instance;

        public GravityHelperModule()
        {
            Instance = this;
        }

        // no save data needed
        public override Type SaveDataType => null;

        public override Type SettingsType => typeof(GravityHelperModuleSettings);

        public static GravityHelperModuleSettings Settings => (GravityHelperModuleSettings)Instance._Settings;

        public GravityTypes Gravity
        {
            get => Engine.Scene is Level level ? (GravityTypes)level.Session.GetCounter(Constants.CurrentGravityCounterKey) : GravityTypes.Normal;
            set
            {
                if (!(Engine.Scene is Level level))
                    return;

                if (value == Gravity)
                    return;

                level.Session.SetCounter(Constants.CurrentGravityCounterKey, (int)value);

                if (value == GravityTypes.FakeInverted)
                    return;

                OnChangeGravity?.Invoke(value);

                foreach (Component component in Engine.Scene.Tracker.GetComponents<GravityListener>())
                {
                    GravityListener gravityListener = (GravityListener)component;
                    gravityListener.OnChangeGravity?.Invoke(value);
                }
            }
        }

        public static event Action<GravityTypes> OnChangeGravity;

        public override void LoadContent(bool firstLoad)
        {
        }

        public override void Load()
        {
            // BASE FUNCTIONALITY
            PlayerHooks.Load();
            // Interactions with objects in reverse gravity
            InteractionHooks.Load();
            // Changing Gravity
            On.Celeste.EventTrigger.OnEnter += EventTrigger_OnEnter;

            DecalRegistry.AddPropertyHandler("gf_portalYellow", (d, attrs) =>
                d.SceneAs<Level>().Add(new PortalParticles(d.Position, Color.Yellow)));

            DecalRegistry.AddPropertyHandler("gf_portalBlue", (d, attrs) =>
                d.SceneAs<Level>().Add(new PortalParticles(d.Position, Color.Blue)));

            On.Celeste.Level.Update += levelOnUpdate;
        }

        public override void Initialize()
        {
        }

        public override void Unload()
        {
            // BASE FUNCTIONALITY
            PlayerHooks.Unload();
            // Interactions with objects in reverse gravity
            InteractionHooks.Unload();
            // Changing Gravity
            On.Celeste.EventTrigger.OnEnter -= EventTrigger_OnEnter;
            On.Celeste.Level.Update -= levelOnUpdate;
        }

        private void levelOnUpdate(On.Celeste.Level.orig_Update orig, Level self)
        {
            if (!Settings.Enabled)
                Gravity = GravityTypes.Normal;
            else if (Settings.ToggleInvertGravity.Pressed)
            {
                Settings.ToggleInvertGravity.ConsumePress();
                Gravity = Gravity == GravityTypes.Normal ? GravityTypes.Inverted : GravityTypes.Normal;
            }

            orig(self);
        }

        private void EventTrigger_OnEnter(On.Celeste.EventTrigger.orig_OnEnter orig, EventTrigger self, Player player)
        {
            switch (self.Event)
            {
                case Constants.SetInvertedGravityEvent:
                    Gravity = GravityTypes.Inverted;
                    break;

                case Constants.SetNormalGravityEvent:
                    Gravity = GravityTypes.Normal;
                    break;

                case Constants.ToggleGravityEvent:
                    Gravity = Gravity == GravityTypes.Normal ? GravityTypes.Inverted : GravityTypes.Normal;
                    break;

                case Constants.RefreshBoostersEvent:
                    try
                    {
                        foreach (Booster b in self.Scene.Entities.FindAll<Booster>())
                        {
                            b.Respawn();
                            ReflectionCache.Booster_respawnTimer.SetValue(b, 0f);
                        }
                    }
                    catch
                    {
                        // ignored
                    }

                    break;

                default:
                    orig(self, player);
                    break;
            }
        }

        public enum GravityTypes
        {
            Normal,
            Inverted,
            FakeInverted
        }
    }
}
