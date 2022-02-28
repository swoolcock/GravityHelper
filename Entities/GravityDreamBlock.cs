// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Reflection;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Entities
{
    [CustomEntity("GravityHelper/GravityDreamBlock")]
    [TrackedAs(typeof(DreamBlock))]
    public class GravityDreamBlock : DreamBlock
    {
        private static readonly Type dream_particle_type = typeof(DreamBlock).GetNestedType("DreamParticle", BindingFlags.NonPublic);
        private static readonly FieldInfo dream_particle_color_fieldinfo = dream_particle_type.GetField("Color", BindingFlags.Instance | BindingFlags.Public);

        private readonly Version _modVersion;
        private readonly Version _pluginVersion;

        private readonly DynData<DreamBlock> _dreamBlockData;

        public GravityType GravityType { get; }

        public GravityDreamBlock(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            _modVersion = data.ModVersion();
            _pluginVersion = data.PluginVersion();

            GravityType = (GravityType)data.Int("gravityType");
            _dreamBlockData = new DynData<DreamBlock>(this);

            var textures = _dreamBlockData.Get<MTexture[]>("particleTextures");
            var prefix = GravityType switch
            {
                GravityType.Normal => "down",
                GravityType.Inverted => "up",
                _ => "double",
            };

            textures[0] = GFX.Game[$"objects/GravityHelper/gravityDreamBlock/{prefix}Arrow"];
            textures[1] = GFX.Game[$"objects/GravityHelper/gravityDreamBlock/{prefix}ArrowSmall"];
        }

        public void UpdateParticleColors()
        {
            var gravityTypeColor = GravityType.Color();
            var particlesObject = _dreamBlockData["particles"];
            if (particlesObject is Array particles)
            {
                for (int i = 0; i < particles.Length; i++)
                {
                    var particle = particles.GetValue(i);
                    var lightness = -0.25f + Calc.Random.NextFloat();
                    dream_particle_color_fieldinfo.SetValue(particle, gravityTypeColor.Lighter(lightness));
                    particles.SetValue(particle, i);
                }
            }
        }
    }
}
