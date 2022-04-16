// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities.Controllers
{
    public abstract class BaseGravityController<TController> : BaseGravityController
        where TController : BaseGravityController
    {
        protected BaseGravityController(EntityData data, Vector2 offset)
            : base(data, offset) {
        }

        public TController ActiveController => SceneAs<Level>().GetActiveController<TController>() ?? this as TController;
    }

    public abstract class BaseGravityController : Entity
    {
        protected readonly Version ModVersion;
        protected readonly Version PluginVersion;

        /// <summary>
        /// Whether or not this is the master controller that should handle everything for this type.
        /// Get the persistent controller with Scene.GetPersistentController if you want to call a method.
        /// Get the active controller with Scene.GetActiveController if you want to read a property.
        /// </summary>
        public bool Persistent { get; }

        protected BaseGravityController(EntityData data, Vector2 offset)
            : base(data.Position + offset)
        {
            ModVersion = data.ModVersion();
            PluginVersion = data.PluginVersion();
            Position = data.Level.Position + new Vector2(16, 16);

            Active = Persistent = data.Bool("persistent", true);

            AddTag(Tags.Global);

            if (Persistent)
            {

                // note that we must use OnOutBegin since OnInBegin will only be called on
                // entities that are part of the newly loaded room
                Add(new TransitionListener
                {
                    OnOutBegin = Transitioned,
                });
            }
        }

        public virtual void Transitioned()
        {
        }

        public static void LoadHooks()
        {
            On.Celeste.Level.LoadLevel += Level_LoadLevel;
        }

        public static void UnloadHooks()
        {
            On.Celeste.Level.LoadLevel -= Level_LoadLevel;
        }

        /// <summary>
        /// This is hooked to ensure that LoadLevel will create and add all controllers on the entire map,
        /// and only once when the map is first loaded, regardless of the current room.
        /// </summary>
        private static void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerintro, bool isfromloader)
        {
            if (!isfromloader)
            {
                orig(self, playerintro, false);
                return;
            }

            // find all gravity controllers
            var entities = self.Session.MapData?.Levels?.SelectMany(l => l.Entities) ?? Enumerable.Empty<EntityData>();
            var controllers = entities.Where(d => d.Name.StartsWith("GravityHelper") && d.Name.Contains("Controller")).ToArray();

            foreach (var data in controllers)
            {
                self.Session.DoNotLoad.Add(new EntityID(data.Level.Name, data.ID));
                Level.LoadCustomEntity(data, self);
            }

            orig(self, playerintro, true);

            // apply each controller type (this should probably be automatic)
            self.GetPersistentController<BehaviorGravityController>()?.Transitioned();
            self.GetPersistentController<SoundGravityController>()?.Transitioned();
            self.GetPersistentController<VisualGravityController>()?.Transitioned();
            self.GetPersistentController<VvvvvvGravityController>()?.Transitioned();
        }
    }
}
