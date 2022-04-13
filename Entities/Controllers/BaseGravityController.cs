// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities.Controllers
{
    public abstract class BaseGravityController : Entity
    {
        protected readonly Version ModVersion;
        protected readonly Version PluginVersion;

        public bool Persistent { get; }

        protected BaseGravityController CurrentChild { get; private set; }

        protected BaseGravityController(EntityData data, Vector2 offset)
            : base(data.Position + offset)
        {
            ModVersion = data.ModVersion();
            PluginVersion = data.PluginVersion();

            Persistent = data.Bool("persistent", true);

            if (Persistent)
            {
                AddTag(Tags.Global);

                // note that we must use OnOutBegin since OnInBegin will only be called on
                // entities that are part of the newly loaded room
                Add(new TransitionListener
                {
                    OnOutBegin = applyCurrent,
                });
            }
        }

        private void applyCurrent()
        {
            var level = SceneAs<Level>();
            CurrentChild = level.Entities.FirstOrDefault(e =>
                e.GetType() == GetType() &&
                (e as BaseGravityController)?.Persistent == false &&
                level.IsInBounds(e)) as BaseGravityController;
            Logger.Log(nameof(GravityHelperModule), $"applyCurrent: CurrentChild is {CurrentChild}");
            Apply();
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            // only persistent controllers should be tracked
            if (Persistent)
            {
                if (!scene.Tracker.Entities.TryGetValue(GetType(), out var list))
                    list = scene.Tracker.Entities[GetType()] = new List<Entity>();
                if (!list.Contains(this))
                    list.Add(this);
            }
        }

        public virtual void Apply()
        {
        }

        public override void Update()
        {
            base.Update();

            if (Persistent && CurrentChild != null && CurrentChild.Scene == null)
                CurrentChild = null;
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
        /// This is hooked to ensure that LoadLevel will create and add all persistent controllers on the entire map,
        /// and only once when the map is first loaded, regardless of the current room.
        /// </summary>
        private static void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerintro, bool isfromloader)
        {
            if (!isfromloader)
            {
                orig(self, playerintro, false);
                return;
            }

            // find all persistent gravity controllers
            var entities = self.Session.MapData?.Levels?.SelectMany(l => l.Entities) ?? Enumerable.Empty<EntityData>();
            var controllers = entities.Where(d => d.Name.StartsWith("GravityHelper") && d.Name.Contains("Controller"));
            var persistent = controllers.Where(d => d.Bool("persistent"));

            foreach (var data in persistent)
            {
                self.Session.DoNotLoad.Add(new EntityID(self.Session.LevelData.Name, data.ID));
                Level.LoadCustomEntity(data, self);
            }

            orig(self, playerintro, true);

            // apply all persistent controllers (they will defer to the current room)
            foreach (var controller in self.Entities
                .OfType<BaseGravityController>()
                .Where(c => c.Persistent))
                controller.applyCurrent();
        }
    }
}
