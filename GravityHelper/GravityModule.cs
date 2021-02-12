using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using Mono.Cecil;

namespace GravityHelper
{

    public class GravityModule : EverestModule
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

        /*
        [Command("logil", "[Gravity Helper TO DELETE] Logs the IL code of an Instanced method:\n 0 -> Normal\n 1 -> Inverted")]
        public static void CmdLogIL(string type, string methodName)
        {
            try
            {
                Type Type = typeof(Celeste.Celeste).Assembly.GetType(type, true, true);
                MethodBase methodInfo = Type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
                MethodDefinition def = new DynamicMethodDefinition(methodInfo).Definition;
                ILContext cursor = new ILContext(def);
                foreach (var instr in cursor.Instrs)
                {
                    Logger.Log("ILLog", instr.ToString());
                }
                var map = (Dictionary<object, object>)new DynData<ILHook>()["_Map"];
            } catch (Exception e)
            {
                Logger.LogDetailed(e, "");
            }
        } */

        // Only one alive module instance can exist at any given time.
        public static GravityModule Instance;

        public GravityModule()
        {
            Instance = this;
        }
        // no save data needed
        public override Type SaveDataType => null;

        public override Type SettingsType => typeof(GravityModuleSettings);

        public static GravityModuleSettings Settings => (GravityModuleSettings)Instance._Settings;

        //public override Type SessionType => typeof(GravitySession);

        public enum GravityTypes
        {
            Normal,
            Inverted,
            FakeInverted
        }

        public GravityTypes Gravity {
            get
            {
                if ((Engine.Scene as Level) != null)
                    return (GravityTypes)(Engine.Scene as Level).Session.GetCounter("jtpGravity");
                return GravityTypes.Normal;
            }
            set
            {
                if ((Engine.Scene as Level) != null)
                {
                    SetGravity(value, (Engine.Scene as Level).Session);
                }
            }

        }

        public static event Action<GravityTypes> OnChangeGravity;

        public void SetGravity(GravityTypes value, Session session)
        {
            int prev = session.GetCounter("jtpGravity");
            session.SetCounter("jtpGravity", (int)value);
            if ((GravityTypes)prev != value && value != GravityTypes.FakeInverted)
            {
                OnChangeGravity?.Invoke(value);
                if (Engine.Scene is Level)
                {
                    foreach (Component component in Engine.Scene.Tracker.GetComponents<GravityListener>())
                    {
                        GravityListener gravityListener = (GravityListener)component;
                        gravityListener.OnChangeGravity?.Invoke(value);
                    }
                }
            }
        }

        public override void LoadContent(bool firstLoad)
        {

        }

        // Set up any hooks, event handlers and your mod in general here.
        // Load runs before Celeste itself has initialized properly.
        public override void Load()
        {
            // BASE FUNCTIONALITY
            PlayerHooks.Load();
            // Interactions with objects in reverse gravity
            InteractionHooks.Load();
            // Changing Gravity
            On.Celeste.EventTrigger.OnEnter += EventTrigger_OnEnter;

            DecalRegistry.AddPropertyHandler("gf_portalYellow", delegate(Decal d, XmlAttributeCollection attrs)
            {
                d.SceneAs<Level>().Add(new PortalParticles(d.Position, Color.Yellow));
            });
            DecalRegistry.AddPropertyHandler("gf_portalBlue", delegate (Decal d, XmlAttributeCollection attrs)
            {
                d.SceneAs<Level>().Add(new PortalParticles(d.Position, Color.Blue));
            });

            On.Celeste.Level.Update += levelOnUpdate;
        }


        // Optional, initialize anything after Celeste has initialized itself properly.
        public override void Initialize()
        {
        }

        // Unload the entirety of your mod's content, remove any event listeners and undo all hooks.
        public override void Unload()
        {
            // BASE FUNCTIONALITY
            PlayerHooks.Unload();
            // Interactions with objects in reverse gravity
            InteractionHooks.Unload();
            // Changing Gravity
            On.Celeste.EventTrigger.OnEnter -= EventTrigger_OnEnter;
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
                case "jtpGravInv":
                    Gravity = GravityTypes.Inverted;
                    break;
                case "jtpGravNorm":
                    Gravity = GravityTypes.Normal;
                    break;
                case "jtpRefreshBoosters":
                    try
                    {
                        foreach (Booster b in self.Scene.Entities.FindAll<Booster>())
                        {
                            if (true)
                            {
                                b.Respawn();
                                ReflectionCache.Booster_respawnTimer.SetValue(b, 0f);
                            }
                        }
                    } catch (Exception e)
                    {
                        Logger.LogDetailed(e, "asda");
                    }
                    break;
                default:
                    orig(self, player);
                    break;
            }
        }
    }
}
