using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities
{
    [CustomEntity("GravityHelper/UpsideDownWatchTower")]
    public class UpsideDownWatchTower : Lookout
    {
        private static readonly string[] prefixes = {"", "badeline_", "nobackpack_"};

        private static readonly string[][] pairs =
        {
            new[] {"lookingUp", "lookingDown"},
            new[] {"lookingUpRight", "lookingDownRight"},
            new[] {"lookingUpLeft", "lookingDownLeft"},
        };

        private bool addedUI;

        public UpsideDownWatchTower(EntityData data, Vector2 offset)
            : base(data, offset)
        {
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
            foreach (var prefix in prefixes)
            {
                foreach (var pair in pairs)
                {
                    var upAnim = animations[prefix + pair[0]];
                    var downAnim = animations[prefix + pair[1]];
                    animations[prefix + pair[0]] = downAnim;
                    animations[prefix + pair[1]] = upAnim;
                }
            }
        }

        public override void Update()
        {
            if (!addedUI)
            {
                var talkComponent = Get<TalkComponent>();
                if (talkComponent.UI == null)
                {
                    addedUI = true;
                    Scene.Add(talkComponent.UI = new UpsideDownTalkComponentUI(talkComponent));
                }
            }

            base.Update();
        }
    }
}