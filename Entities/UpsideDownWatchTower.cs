// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities
{
    [CustomEntity("GravityHelper/UpsideDownWatchTower")]
    [TrackedAs(typeof(Lookout))]
    public class UpsideDownWatchTower : Lookout
    {
        private static readonly string[] prefixes = {"", "badeline_", "nobackpack_"};

        private static readonly string[][] pairs =
        {
            new[] {"lookingUp", "lookingDown"},
            new[] {"lookingUpRight", "lookingDownRight"},
            new[] {"lookingUpLeft", "lookingDownLeft"},
        };

        private readonly Version _modVersion;
        private readonly Version _pluginVersion;

        private bool _addedUI;

        public UpsideDownWatchTower(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            _modVersion = data.ModVersion();
            _pluginVersion = data.PluginVersion();

            Collider.TopCenter = -Collider.BottomCenter;

            var talkComponent = Get<TalkComponent>();
            talkComponent.Bounds.Y = -talkComponent.Bounds.Bottom;
            talkComponent.DrawAt.Y *= -1;

            var vertexLight = Get<VertexLight>();
            vertexLight.Y *= -1;

            var sprite = Get<Sprite>();
            sprite.Position.Y *= -1;
            sprite.Scale.Y *= -1;

            var animations = sprite.GetAnimations();
            bool failedKey = false;

            foreach (var prefix in prefixes)
            {
                foreach (var pair in pairs)
                {
                    var upAnimString = prefix + pair[0];
                    var downAnimString = prefix + pair[1];

                    if (!animations.TryGetValue(upAnimString, out var upAnim))
                    {
                        Logger.Log(LogLevel.Warn, nameof(GravityHelperModule), $"Couldn't find up animation {upAnimString}");
                        failedKey = true;
                        continue;
                    }

                    if (!animations.TryGetValue(upAnimString, out var downAnim))
                    {
                        Logger.Log(LogLevel.Warn, nameof(GravityHelperModule), $"Couldn't find down animation {downAnimString}");
                        failedKey = true;
                        continue;
                    }

                    animations[upAnimString] = downAnim;
                    animations[downAnimString] = upAnim;
                }
            }

            // if we failed getting any animation key, dump out the available ones
            if (failedKey)
            {
                var animKeys = string.Join(",", animations.Keys);
                Logger.Log(LogLevel.Warn, nameof(GravityHelperModule), $"WatchTower animations available: {animKeys}");
            }
        }

        public override void Update()
        {
            if (!_addedUI)
            {
                var talkComponent = Get<TalkComponent>();
                if (talkComponent.UI == null)
                {
                    _addedUI = true;
                    Scene.Add(talkComponent.UI = new UpsideDownTalkComponentUI(talkComponent));
                }
            }

            base.Update();
        }
    }
}
