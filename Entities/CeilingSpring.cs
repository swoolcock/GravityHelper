using System;
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace GravityHelper.Entities
{
    [CustomEntity("GravityHelper/CeilingSpring")]
    public class CeilingSpring : Entity
    {
        public Color DisabledColor = Color.White;
        public bool VisibleWhenDisabled;

        private bool playerCanUse;

        private Sprite sprite;
        private Wiggler wiggler;
        private StaticMover staticMover;

        public CeilingSpring(EntityData data, Vector2 offset)
            : base(data.Position + offset)
        {
            playerCanUse = data.Bool(nameof(playerCanUse), true);

            Add(new PlayerCollider(OnCollide));

            Add(sprite = new Sprite(GFX.Game, "objects/spring/"));
            sprite.Add("idle", "", 0.0f, new int[1]);
            sprite.Add("bounce", "", 0.07f, "idle", 0, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 4, 5);
            sprite.Add("disabled", "white", 0.07f);
            sprite.Play("idle");

            sprite.Origin.X = sprite.Width / 2f;
            sprite.Origin.Y = sprite.Height;
            sprite.Rotation = (float) Math.PI;

            Depth = -8501;

            Add(staticMover = new StaticMover
            {
                OnAttach = p => Depth = p.Depth + 1,
                SolidChecker = s => CollideCheck(s, Position - Vector2.UnitY),
                JumpThruChecker = jt => CollideCheck(jt, Position - Vector2.UnitY),
                OnEnable = OnEnable,
                OnDisable = OnDisable,
            });

            Add(wiggler = Wiggler.Create(1f, 4f, v => sprite.Scale.Y = 1 + v * 0.2f));

            Collider = new Hitbox(16f, 6f, -8f, 0f);
        }

        private void OnEnable()
        {
            Visible = Collidable = true;
            sprite.Color = Color.White;
            sprite.Play("idle");
        }

        private void OnDisable()
        {
            Collidable = false;
            if (VisibleWhenDisabled)
            {
                sprite.Play("disabled");
                sprite.Color = DisabledColor;
            }
            else
                Visible = false;
        }

        private void OnCollide(Player player)
        {
            if (player.StateMachine.State == Player.StDreamDash || !playerCanUse)
                return;

            if (GravityHelperModule.Session.Gravity == GravityType.Normal)
                GravityHelperModule.Session.Gravity = GravityType.Inverted;

            if (player.Speed.Y < 0)
                return;

            BounceAnimate();
            player.SuperBounce(Bottom);
        }

        private void BounceAnimate()
        {
            Audio.Play("event:/game/general/spring", BottomCenter);
            staticMover.TriggerPlatform();
            sprite.Play("bounce", true);
            wiggler.Start();
        }

        public override void Render()
        {
            if (Collidable)
                sprite.DrawOutline();
            base.Render();
        }
    }
}