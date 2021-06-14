using System;
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace GravityHelper.Entities
{
    [CustomEntity(
        "GravityHelper/GravitySpringFloor = LoadFloor",
        "GravityHelper/GravitySpringCeiling = LoadCeiling",
        "GravityHelper/GravitySpringWallLeft = LoadWallLeft",
        "GravityHelper/GravitySpringWallRight = LoadWallRight")]
    public class GravitySpring : Entity
    {
        public Color DisabledColor = Color.White;
        public bool VisibleWhenDisabled;

        public bool PlayerCanUse { get; }
        public Orientations Orientation { get; }
        public GravityType GravityType { get; }
        public float Cooldown { get; }

        private string getAnimId(string id) => GravityType switch
        {
            GravityType.None => $"none_{id}",
            GravityType.Normal => $"normal_{id}",
            GravityType.Inverted => $"invert_{id}",
            GravityType.Toggle => $"toggle_{id}",
            _ => id
        };

        private Sprite sprite;
        private Wiggler wiggler;
        private StaticMover staticMover;
        private float cooldownRemaining;

        public static Entity LoadFloor(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
            new GravitySpring(entityData, offset, Orientations.Floor);

        public static Entity LoadCeiling(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
            new GravitySpring(entityData, offset, Orientations.Ceiling);

        public static Entity LoadWallLeft(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
            new GravitySpring(entityData, offset, Orientations.WallLeft);

        public static Entity LoadWallRight(Level level, LevelData levelData, Vector2 offset, EntityData entityData) =>
            new GravitySpring(entityData, offset, Orientations.WallRight);

        public GravitySpring(EntityData data, Vector2 offset, Orientations orientation)
            : base(data.Position + offset)
        {
            PlayerCanUse = data.Bool("playerCanUse", true);
            GravityType = data.Enum<GravityType>("gravityType");
            Cooldown = data.Float("cooldown", 1f);

            Orientation = orientation;

            Add(new PlayerCollider(OnCollide));
            Add(sprite = GFX.SpriteBank.Create("gravitySpring"));
            sprite.Play(getAnimId("idle"));

            switch (Orientation)
            {
                case Orientations.Floor:
                    sprite.Rotation = 0;
                    Collider = new Hitbox(16f, 6f, -8f, -6f);
                    break;

                case Orientations.WallLeft:
                    sprite.Rotation = (float) Math.PI / 2f;
                    Collider = new Hitbox(6, 16f, 0f, -8f);
                    break;

                case Orientations.WallRight:
                    sprite.Rotation = (float) -Math.PI / 2f;
                    Collider = new Hitbox(6, 16f, -6f, -8f);
                    break;

                case Orientations.Ceiling:
                    sprite.Rotation = (float) Math.PI;
                    Collider = new Hitbox(16f, 6f, -8f, 0f);
                    break;
            }

            Depth = -8501;

            Add(staticMover = new StaticMover
            {
                OnAttach = p => Depth = p.Depth + 1,
                SolidChecker = Orientation switch
                {
                    Orientations.WallLeft => s => CollideCheck(s, Position - Vector2.UnitX),
                    Orientations.WallRight => s => CollideCheck(s, Position + Vector2.UnitX),
                    Orientations.Ceiling => s => CollideCheck(s, Position - Vector2.UnitY),
                    _ => s => CollideCheck(s, Position + Vector2.UnitY)
                },
                JumpThruChecker = Orientation switch
                {
                    Orientations.WallLeft => jt => CollideCheck(jt, Position - Vector2.UnitX),
                    Orientations.WallRight => jt => CollideCheck(jt, Position + Vector2.UnitX),
                    Orientations.Ceiling => jt => CollideCheck(jt, Position - Vector2.UnitY),
                    _ => jt => CollideCheck(jt, Position + Vector2.UnitY)
                },
                OnShake = amount => Position += amount,
                OnEnable = OnEnable,
                OnDisable = OnDisable,
            });

            Add(wiggler = Wiggler.Create(1f, 4f, v => sprite.Scale.Y = 1 + v * 0.2f));
        }

        private void OnEnable()
        {
            Visible = Collidable = true;
            sprite.Color = Color.White;
            sprite.Play(getAnimId("idle"));
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

        public override void Update()
        {
            base.Update();

            if (cooldownRemaining > 0)
            {
                cooldownRemaining = Math.Max(0, cooldownRemaining - Engine.DeltaTime);
                // TODO: update sprite to show cooldown
            }
        }

        private void OnCollide(Player player)
        {
            // ignore spring if dream dashing, if we're not allowed to use it, or if we're on cooldown
            if (player.StateMachine.State == Player.StDreamDash || !PlayerCanUse)
                return;

            // ignore spring if moving away
            var realY = GravityHelperModule.ShouldInvert ? -player.Speed.Y : player.Speed.Y;
            switch (Orientation)
            {
                case Orientations.Floor when realY < 0:
                case Orientations.Ceiling when realY > 0:
                case Orientations.WallLeft when player.Speed.X > 240:
                case Orientations.WallRight when player.Speed.X < -240:
                    return;
            }

            // set gravity and cooldown if not on cooldown
            if (GravityType != GravityType.None && cooldownRemaining == 0f)
            {
                GravityHelperModule.Instance.SetGravity(GravityType);
                cooldownRemaining = Cooldown;
                // TODO: update sprite to show cooldown
            }

            // boing!
            BounceAnimate();

            // bounce player away
            switch (Orientation)
            {
                case Orientations.Floor:
                    if (GravityHelperModule.ShouldInvert)
                        InvertedSuperBounce(player, Top);
                    else
                        player.SuperBounce(Top);
                    break;

                case Orientations.Ceiling:
                    if (!GravityHelperModule.ShouldInvert)
                        InvertedSuperBounce(player, Bottom);
                    else
                        player.SuperBounce(Bottom);
                    break;

                case Orientations.WallLeft:
                    player.SideBounce(1, CenterRight.X, CenterRight.Y);
                    break;

                case Orientations.WallRight:
                    player.SideBounce(-1, CenterLeft.X, CenterLeft.Y);
                    break;
            }
        }

        private void BounceAnimate()
        {
            Audio.Play("event:/game/general/spring", BottomCenter);
            staticMover.TriggerPlatform();
            sprite.Play(getAnimId("bounce"), true);
            wiggler.Start();
        }

        public override void Render()
        {
            if (Collidable)
                sprite.DrawOutline();
            base.Render();
        }

        public enum Orientations
        {
            Floor,
            WallLeft,
            WallRight,
            Ceiling,
        }

        public static void InvertedSuperBounce(Player self, float fromY)
        {
            if (self.StateMachine.State == Player.StBoost && self.CurrentBooster != null)
            {
                self.CurrentBooster.PlayerReleased();
                self.CurrentBooster = null;
            }

            Collider collider = self.Collider;
            self.Collider = self.GetNormalHitbox();
            self.MoveV(GravityHelperModule.ShouldInvert ? self.Bottom - fromY : fromY - self.Top);
            if (!self.Inventory.NoRefills)
                self.RefillDash();
            self.RefillStamina();

            using (var data = new DynData<Player>(self))
            {
                data["jumpGraceTimer"] = 0f;
                data["varJumpTimer"] = 0f;
                data["dashAttackTimer"] = 0.0f;
                data["gliderBoostTimer"] = 0.0f;
                data["wallSlideTimer"] = 1.2f;
                data["wallBoostTimer"] = 0.0f;
                data["varJumpSpeed"] = 0f;
                data["launched"] = false;
            }

            self.StateMachine.State = Player.StNormal;
            self.AutoJump = false;
            self.AutoJumpTimer = 0.0f;
            self.Speed.X = 0.0f;
            self.Speed.Y = 185f;

            var level = self.SceneAs<Level>();
            level?.DirectionalShake(GravityHelperModule.ShouldInvert ? -Vector2.UnitY : Vector2.UnitY, 0.1f);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            self.Sprite.Scale = new Vector2(0.5f, 1.5f);
            self.Collider = collider;
        }
    }
}
