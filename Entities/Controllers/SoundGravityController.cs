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
    public class SoundGravityController : BaseGravityController<SoundGravityController>
    {
        private const float sound_muffle_time_seconds = 0.25f;

        private float _soundMuffleRemaining;

        public string NormalSound { get; }
        public string InvertedSound { get; }
        public string ToggleSound { get; }
        public string MusicParam { get; }

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
                        var active = ActiveController;

                        setParam(active.MusicParam);

                        if (!args.Changed)
                            return;

                        var soundName = args.WasToggled ? active.ToggleSound : string.Empty;
                        if (string.IsNullOrEmpty(soundName))
                            soundName = args.NewValue == GravityType.Normal ? active.NormalSound : active.InvertedSound;

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
            if (!Persistent) return;

            setParam(ActiveController.MusicParam);
        }

        public override void Update()
        {
            base.Update();
            if (!Persistent) return;

            if (_soundMuffleRemaining > 0)
                _soundMuffleRemaining -= Engine.DeltaTime;
        }

        private static void setParam(string param)
        {
            if (!string.IsNullOrEmpty(param))
                Audio.SetMusicParam(param, GravityHelperModule.ShouldInvertPlayer ? 1 : 0);
        }
    }
}
