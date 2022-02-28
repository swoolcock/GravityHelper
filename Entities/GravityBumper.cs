// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities
{
    [CustomEntity("GravityHelper/GravityBumper")]
    public class GravityBumper : Bumper
    {
        // ReSharper disable InconsistentNaming
        private static ParticleType P_Ambience_Normal;
        private static ParticleType P_Ambience_Inverted;
        private static ParticleType P_Ambience_Toggle;
        private static ParticleType P_Launch_Normal;
        private static ParticleType P_Launch_Inverted;
        private static ParticleType P_Launch_Toggle;
        // ReSharper restore InconsistentNaming

        private readonly Version _modVersion;
        private readonly Version _pluginVersion;

        public GravityType GravityType { get; }

        public GravityBumper(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            _modVersion = data.ModVersion();
            _pluginVersion = data.PluginVersion();

            GravityType = (GravityType)data.Int("gravityType");
        }

        public ParticleType GetAmbientParticleType()
        {
            if (P_Ambience_Normal == null)
            {
                const float lightness = 0.5f;
                P_Ambience_Normal = new ParticleType(P_Ambience)
                {
                    Color = GravityType.Normal.Color(),
                    Color2 = GravityType.Normal.Color().Lighter(lightness),
                };
                P_Ambience_Inverted = new ParticleType(P_Ambience)
                {
                    Color = GravityType.Inverted.Color(),
                    Color2 = GravityType.Inverted.Color().Lighter(lightness),
                };
                P_Ambience_Toggle = new ParticleType(P_Ambience)
                {
                    Color = GravityType.Toggle.Color(),
                    Color2 = GravityType.Toggle.Color().Lighter(lightness),
                };
            }

            return GravityType switch
            {
                GravityType.Normal => P_Ambience_Normal,
                GravityType.Inverted => P_Ambience_Inverted,
                GravityType.Toggle => P_Ambience_Toggle,
                _ => P_Ambience,
            };
        }

        public ParticleType GetLaunchParticleType()
        {
            if (P_Launch_Normal == null)
            {
                const float lightness = 0.5f;
                P_Launch_Normal = new ParticleType(P_Launch)
                {
                    Color = GravityType.Normal.Color(),
                    Color2 = GravityType.Normal.Color().Lighter(lightness),
                };
                P_Launch_Inverted = new ParticleType(P_Launch)
                {
                    Color = GravityType.Inverted.Color(),
                    Color2 = GravityType.Inverted.Color().Lighter(lightness),
                };
                P_Launch_Toggle = new ParticleType(P_Launch)
                {
                    Color = GravityType.Toggle.Color(),
                    Color2 = GravityType.Toggle.Color().Lighter(lightness),
                };
            }

            return GravityType switch
            {
                GravityType.Normal => P_Launch_Normal,
                GravityType.Inverted => P_Launch_Inverted,
                GravityType.Toggle => P_Launch_Toggle,
                _ => P_Launch,
            };
        }
    }
}
