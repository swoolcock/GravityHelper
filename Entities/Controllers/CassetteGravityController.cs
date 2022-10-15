// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Components;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.Entities.Controllers
{
    [CustomEntity("GravityHelper/CassetteGravityController")]
    [Tracked]
    public class CassetteGravityController : BaseGravityController<CassetteGravityController>
    {
        public GravityType[] CassetteSequence { get; }
        public float MomentumMultiplier { get; }

        // ReSharper disable once UnusedMember.Global
        public CassetteGravityController()
        {
            // ephemeral controller
        }

        // ReSharper disable once UnusedMember.Global
        public CassetteGravityController(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            // cassette sequence is a pipe or comma separated list
            // type,type,type,type
            var cassetteSequenceString = data.Attr("cassetteSequence").Replace("|", ",");
            CassetteSequence = cassetteSequenceString
                .Split(',')
                .Select(s =>
                {
                    if (!int.TryParse(s, out var value)) return default;
                    var type = (GravityType)value;
                    return type >= GravityType.None && type <= GravityType.Toggle ? type : default;
                })
                .ToArray();

            MomentumMultiplier = data.Float("momentumMultiplier", 1f);

            for (int i = 0; i < CassetteSequence.Length; i++)
            {
                var index = i;
                Add(new CassetteComponent(index, data)
                {
                    Enabled = false,
                    OnStateChange = state =>
                    {
                        var type = CassetteSequence[index];
                        if (state == CassetteStates.Appearing)
                        {
                            var manager = Scene?.Tracker.GetEntity<CassetteBlockManager>();
                            var managerData = DynamicData.For(manager);
                            var tempoMult = managerData.Get<float>("tempoMult");
                            var indicators = Scene?.Tracker.GetEntitiesOrEmpty<GravityIndicator>();
                            foreach (GravityIndicator indicator in indicators)
                            {
                                if (!indicator.SyncToPlayer)
                                {
                                    indicator.TurnTarget = type;
                                    indicator.TurnTime = tempoMult * 10f / 60f;
                                }
                            }
                        }
                        else if (state == CassetteStates.On &&
                            CassetteSequence[index] != GravityType.None &&
                            GravityHelperModule.PlayerComponent is { } playerComponent &&
                            playerComponent.Entity is Player player &&
                            !player.IsIntroState)
                        {
                            playerComponent.SetGravity(type, MomentumMultiplier);
                        }
                    },
                });
            }
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            if (scene is Level level)
            {
                level.CassetteBlockBeats = CassetteSequence.Length;
                level.HasCassetteBlocks = true;
            }
        }

        public override void Transitioned()
        {
            var thisActive = ActiveController == this;
            foreach (var component in Components.GetAll<CassetteComponent>())
                component.Enabled = thisActive;
        }
    }
}
