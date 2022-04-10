// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Components;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities.Controllers
{
    [CustomEntity("GravityHelper/SoundGravityController")]
    [Tracked]
    public class SoundGravityController : BaseGravityController
    {
        public string NormalSound { get; }
        public string InvertedSound { get; }
        public string ToggleSound { get; }
        public string MusicParam { get; }

        private const float sound_muffle_time_seconds = 0.25f;

        private static float _soundMuffleRemaining;

        public SoundGravityController(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            NormalSound = data.Attr("normalSound", string.Empty);
            InvertedSound = data.Attr("invertedSound", string.Empty);
            ToggleSound = data.Attr("toggleSound", string.Empty);
            MusicParam = data.Attr("musicParam", string.Empty);

            if (Persistent)
            {
                Add(new PlayerGravityListener
                {
                    GravityChanged = (_, args) =>
                    {
                        setParam();

                        if (!args.Changed)
                            return;

                        var soundName = args.WasToggled ? GravityHelperModule.Session.ToggleSound : string.Empty;
                        if (string.IsNullOrEmpty(soundName))
                            soundName = args.NewValue == GravityType.Normal ? GravityHelperModule.Session.NormalSound : GravityHelperModule.Session.InvertedSound;

                        if (!string.IsNullOrEmpty(soundName) && _soundMuffleRemaining <= 0 && args.PlayerTriggered)
                        {
                            _soundMuffleRemaining = sound_muffle_time_seconds;
                            Audio.Play(soundName);
                        }
                    },
                });
            }
        }

        protected override void Apply()
        {
            GravityHelperModule.Session.NormalSound = NormalSound;
            GravityHelperModule.Session.InvertedSound = InvertedSound;
            GravityHelperModule.Session.ToggleSound = ToggleSound;
            GravityHelperModule.Session.MusicParam = MusicParam;
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            if (Persistent)
                setParam();
        }

        public override void Update()
        {
            base.Update();
            if (_soundMuffleRemaining > 0)
                _soundMuffleRemaining -= Engine.DeltaTime;
        }

        private void setParam()
        {
            if (!string.IsNullOrEmpty(GravityHelperModule.Session.MusicParam))
                Audio.SetMusicParam(GravityHelperModule.Session.MusicParam, GravityHelperModule.ShouldInvertPlayer ? 1 : 0);
        }
    }
}
