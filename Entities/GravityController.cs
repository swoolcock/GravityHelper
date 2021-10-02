// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities
{
    [CustomEntity("GravityHelper/GravityController")]
    [Tracked]
    public class GravityController : Entity
    {
        private static float soundMuffleRemaining;

        private const float sound_muffle_time_seconds = 0.25f;
        private const string default_normal_gravity_sound = "event:/char/madeline/climb_ledge";
        private const string default_inverted_gravity_sound = "event:/char/madeline/crystaltheo_lift";

        public string NormalGravitySound { get; }
        public string InvertedGravitySound { get; }
        public string ToggleGravitySound { get; }
        public bool AlwaysTrigger { get; }
        public float ArrowOpacity { get; }
        public float FieldOpacity { get; }
        public float ParticleOpacity { get; }

        public GravityController(EntityData data, Vector2 offset)
            : base(data.Position + offset)
        {
            NormalGravitySound = data.Attr("normalGravitySound", default_normal_gravity_sound);
            InvertedGravitySound = data.Attr("invertedGravitySound", default_inverted_gravity_sound);
            ToggleGravitySound = data.Attr("toggleGravitySound");
            AlwaysTrigger = data.Bool("alwaysTrigger");
            ArrowOpacity = Calc.Clamp(data.Float("arrowOpacity", GravityField.DEFAULT_ARROW_OPACITY), 0f, 1f);
            FieldOpacity = Calc.Clamp(data.Float("fieldOpacity", GravityField.DEFAULT_FIELD_OPACITY), 0f, 1f);
            ParticleOpacity = Calc.Clamp(data.Float("particleOpacity", GravityField.DEFAULT_PARTICLE_OPACITY), 0f, 1f);

            Add(new GravityListener
            {
                GravityChanged = args =>
                {
                    if (!args.Changed && !AlwaysTrigger)
                        return;

                    var soundName = args.WasToggled ? ToggleGravitySound : string.Empty;
                    if (string.IsNullOrEmpty(soundName))
                        soundName = args.NewValue == GravityType.Normal ? NormalGravitySound : InvertedGravitySound;

                    if (!string.IsNullOrEmpty(soundName) && soundMuffleRemaining <= 0 && args.PlayerTriggered)
                    {
                        soundMuffleRemaining = sound_muffle_time_seconds;
                        Audio.Play(soundName);
                    }
                },
            });
        }

        public override void Update()
        {
            base.Update();
            soundMuffleRemaining -= Engine.DeltaTime;
        }
    }
}
