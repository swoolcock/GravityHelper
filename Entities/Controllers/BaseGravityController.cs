// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
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

        protected BaseGravityController(EntityData data, Vector2 offset)
            : base(data.Position + offset)
        {
            ModVersion = data.ModVersion();
            PluginVersion = data.PluginVersion();

            Persistent = data.Bool("persistent");

            if (Persistent)
            {
                AddTag(Tags.Persistent);
            }
            else
            {
                Add(new TransitionListener
                {
                    OnOutBegin = () => ApplyCurrent(GetType(), this),
                });
            }
        }

        protected static void ApplyCurrent(Type type, BaseGravityController exclude = default)
        {
            var controller = Engine.Scene.GetController(type, exclude: exclude);
            controller?.Apply();
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            ApplyCurrent(GetType());
        }

        protected virtual void Apply()
        {
        }

        public static void LoadHooks()
        {
            Everest.Events.Level.OnLoadLevel += Level_OnLoadLevel;
        }

        public static void UnloadHooks()
        {
            Everest.Events.Level.OnLoadLevel -= Level_OnLoadLevel;
        }

        /// <summary>
        /// This is hooked to ensure that LoadLevel will create and add all persistent controllers on the entire map,
        /// and only once when the map is first loaded, regardless of the current room.
        /// </summary>
        private static void Level_OnLoadLevel(Level level, Player.IntroTypes playerintro, bool isfromloader)
        {
            if (!isfromloader) return;

            // find all persistent gravity controllers
            var entities = level.Session.MapData?.Levels?.SelectMany(l => l.Entities) ?? Enumerable.Empty<EntityData>();
            var controllers = entities.Where(d => d.Name.StartsWith("GravityHelper") && d.Name.Contains("Controller"));
            var persistent = controllers.Where(d => d.Bool("persistent"));

            foreach (var data in persistent)
            {
                // prevent the level loader from loading them, and do it manually
                level.Session.DoNotLoad.Add(new EntityID(level.Session.LevelData.Name, data.ID));
                Level.LoadCustomEntity(data, level);
            }
        }
    }
}
