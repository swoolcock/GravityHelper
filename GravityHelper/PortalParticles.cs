using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper
{
    class PortalParticles : Entity
    {
        Color Color;

        public PortalParticles(Vector2 pos, Color color) : base(pos)
        {
            Color = color;
        }

        public override void Update()
        {
            base.Update();
            SceneAs<Level>().Particles.Emit(Refill.P_Regen, Position, Color);
        }
    }

    class SpringParticles : Component
    {
        Color Color;

        public SpringParticles(Color color) : base(true, true)
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
