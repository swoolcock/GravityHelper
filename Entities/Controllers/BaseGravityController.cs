// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities.Controllers
{
    public abstract class BaseGravityController<TController> : BaseGravityController
        where TController : BaseGravityController
    {
        protected BaseGravityController()
        {
        }

        protected BaseGravityController(EntityData data, Vector2 offset)
            : base(data, offset) {
        }

        public TController ActiveController => (Scene as Level)?.GetActiveController<TController>() ?? this as TController;
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

        public bool Ephemeral { get; }

        protected BaseGravityController()
        {
            ModVersion = new Version(0, 1);
            PluginVersion = new Version(0, 1);

            Visible = Collidable = false;
            Ephemeral = Active = Persistent = true;

            AddTag(Tags.Global);

            Add(new TransitionListener
            {
                OnOutBegin = Transitioned,
            });
        }

        protected BaseGravityController(EntityData data, Vector2 offset)
            : base(data.Position + offset)
        {
            ModVersion = data.ModVersion();
            PluginVersion = data.PluginVersion();
            Position = data.Level.Position + new Vector2(16, 16);

            Visible = Collidable = false;
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
    }
}
