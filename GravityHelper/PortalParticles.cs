using Celeste;
using Microsoft.Xna.Framework;
using Monocle;

namespace GravityHelper
{
    internal class PortalParticles : Entity
    {
        public readonly Color Color;

        public PortalParticles(Vector2 pos, Color color)
            : base(pos)
        {
            Color = color;
        }

        public override void Update()
        {
            base.Update();
            SceneAs<Level>().Particles.Emit(Refill.P_Regen, Position, Color);
        }
    }

    internal class SpringParticles : Component
    {
        public readonly Color Color;

        public SpringParticles(Color color)
            : base(true, true)
        {
            Color = color;
        }

        public override void Update()
        {
            base.Update();
            SceneAs<Level>().Particles.Emit(Refill.P_Regen, Entity.Position, Color);
        }
    }
}
