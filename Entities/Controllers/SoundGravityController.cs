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
        public const string DEFAULT_NORMAL_SOUND = "event:/ui/game/lookout_off";
        public const string DEFAULT_INVERTED_SOUND = "event:/ui/game/lookout_on";
        public const string DEFAULT_TOGGLE_SOUND = "";

        public string NormalSound { get; } = DEFAULT_NORMAL_SOUND;
        public string InvertedSound { get; } = DEFAULT_INVERTED_SOUND;
        public string ToggleSound { get; } = DEFAULT_TOGGLE_SOUND;
        public string LineSound { get; } = GravityLine.DEFAULT_SOUND;
        public string InversionBlockSound { get; } = InversionBlock.DEFAULT_SOUND;
        public string MusicParam { get; } = string.Empty;

        // ReSharper disable once UnusedMember.Global
        public SoundGravityController()
        {
            // ephemeral controller
        }

        // ReSharper disable once UnusedMember.Global
        public SoundGravityController(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            NormalSound = data.Attr("normalSound", NormalSound);
            InvertedSound = data.Attr("invertedSound", InvertedSound);
            ToggleSound = data.Attr("toggleSound", ToggleSound);
            LineSound = data.Attr("lineSound", LineSound);
            InversionBlockSound = data.Attr("inversionBlockSound", InversionBlockSound);
            MusicParam = data.Attr("musicParam", MusicParam);

            if (Persistent)
            {
                Add(new PlayerGravityListener
                {
                    GravityChanged = (_, args) =>
                    {
                        var active = ActiveController;
                        setParam(active.MusicParam);
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

        private static void setParam(string param)
        {
            if (!string.IsNullOrEmpty(param))
                Audio.SetMusicParam(param, GravityHelperModule.ShouldInvertPlayer ? 1 : 0);
        }
    }
}
