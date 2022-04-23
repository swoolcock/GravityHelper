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

        public string NormalSound { get; }
        public string InvertedSound { get; }
        public string ToggleSound { get; }
        public string LineSound { get; }
        public string MusicParam { get; }

        public SoundGravityController(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            NormalSound = data.Attr("normalSound", DEFAULT_NORMAL_SOUND);
            InvertedSound = data.Attr("invertedSound", DEFAULT_INVERTED_SOUND);
            ToggleSound = data.Attr("toggleSound", DEFAULT_TOGGLE_SOUND);
            LineSound = data.Attr("lineSound", string.Empty);
            MusicParam = data.Attr("musicParam", string.Empty);

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
