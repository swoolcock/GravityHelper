// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Celeste.Mod.Entities;
using Celeste.Mod.GravityHelper.Components;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities.Controllers
{
    [CustomEntity("GravityHelper/SoundGravityController")]
    public class SoundGravityController : BaseGravityController
    {
        public string NormalSound => CurrentChild?.NormalSound ?? _normalSound;
        public string InvertedSound => CurrentChild?.InvertedSound ?? _invertedSound;
        public string ToggleSound => CurrentChild?.ToggleSound ?? _toggleSound;
        public string MusicParam => CurrentChild?.MusicParam ?? _musicParam;

        protected new SoundGravityController CurrentChild => base.CurrentChild as SoundGravityController;

        private const float sound_muffle_time_seconds = 0.25f;

        private static float _soundMuffleRemaining;
        private readonly string _normalSound;
        private readonly string _invertedSound;
        private readonly string _toggleSound;
        private readonly string _musicParam;

        public SoundGravityController(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            _normalSound = data.Attr("normalSound", string.Empty);
            _invertedSound = data.Attr("invertedSound", string.Empty);
            _toggleSound = data.Attr("toggleSound", string.Empty);
            _musicParam = data.Attr("musicParam", string.Empty);

            if (Persistent)
            {
                Add(new PlayerGravityListener
                {
                    GravityChanged = (_, args) =>
                    {
                        setParam();

                        if (!args.Changed)
                            return;

                        var soundName = args.WasToggled ? ToggleSound : string.Empty;
                        if (string.IsNullOrEmpty(soundName))
                            soundName = args.NewValue == GravityType.Normal ? NormalSound : InvertedSound;

                        if (!string.IsNullOrEmpty(soundName) && _soundMuffleRemaining <= 0 && args.PlayerTriggered)
                        {
                            _soundMuffleRemaining = sound_muffle_time_seconds;
                            Audio.Play(soundName);
                        }
                    },
                });
            }
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
            if (!string.IsNullOrEmpty(MusicParam))
                Audio.SetMusicParam(MusicParam, GravityHelperModule.ShouldInvertPlayer ? 1 : 0);
        }
    }
}
