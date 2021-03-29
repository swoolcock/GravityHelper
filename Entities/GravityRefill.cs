using System;
using System.Collections;
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace GravityHelper.Entities
{
    [CustomEntity("GravityHelper/GravityRefill")]
    public class GravityRefill : Entity
    {
        // properties
        public bool OneUse { get; }
        public int Charges { get; }
        public bool RefillsDash { get; }
        public bool RefillsStamina { get; }
        public float RespawnTime { get; }

        // components
        private Sprite sprite;
        private Sprite flash;
        private Image outline;
        private Wiggler wiggler;
        private BloomPoint bloom;
        private VertexLight light;
        private SineWave sine;

        // particles
        private readonly ParticleType p_shatter = Refill.P_Shatter;
        private readonly ParticleType p_regen = Refill.P_Regen;
        private readonly ParticleType p_glow = Refill.P_Glow;

        private Level level;
        private float respawnTimeRemaining;

        public GravityRefill(Vector2 position, int charges, bool oneUse, bool refillsDash, bool refillsStamina, float respawnTime)
            : base(position)
        {
            Charges = charges;
            OneUse = oneUse;
            RefillsDash = refillsDash;
            RefillsStamina = refillsStamina;
            RespawnTime = respawnTime;

            Collider = new Hitbox(16f, 16f, -8f, -8f);
            Depth = -100;

            var path = "objects/refill";

            // add components
            Add(new PlayerCollider(OnPlayer),
                outline = new Image(GFX.Game[$"{path}/outline"]) {Visible = false},
                sprite = new Sprite(GFX.Game, $"{path}/idle"),
                flash = new Sprite(GFX.Game, $"{path}/flash") {OnFinish = _ => flash.Visible = false},
                wiggler = Wiggler.Create(1f, 4f, v => sprite.Scale = flash.Scale = Vector2.One * (float) (1.0 + (double) v * 0.2)),
                new MirrorReflection(),
                bloom = new BloomPoint(0.8f, 16f),
                light = new VertexLight(Color.White, 1f, 16, 48),
                sine = new SineWave(0.6f, 0.0f));

            // configure components
            outline.CenterOrigin();
            sprite.AddLoop("idle", "", 0.1f);
            sprite.Play("idle");
            sprite.CenterOrigin();
            flash.Add("flash", "", 0.05f);
            flash.CenterOrigin();
            sine.Randomize();

            UpdateY();
        }

        public GravityRefill(EntityData data, Vector2 offset)
            : this(data.Position + offset,
                data.Int("charges", 1),
                data.Bool("oneUse"),
                data.Bool("refillsDash", true),
                data.Bool("refillsStamina", true),
                data.Float("respawnTime", 2.5f))
        {
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);
            level = SceneAs<Level>();
        }

        public override void Update()
        {
            base.Update();

            if (respawnTimeRemaining > 0.0)
            {
                respawnTimeRemaining -= Engine.DeltaTime;
                if (respawnTimeRemaining <= 0.0)
                    Respawn();
            }
            else if (Scene.OnInterval(0.1f))
                level.ParticlesFG.Emit(p_glow, 1, Position, Vector2.One * 5f);

            UpdateY();

            light.Alpha = Calc.Approach(light.Alpha, sprite.Visible ? 1f : 0.0f, 4f * Engine.DeltaTime);
            bloom.Alpha = light.Alpha * 0.8f;

            if (!Scene.OnInterval(2f) || !sprite.Visible) return;

            flash.Play("flash", true);
            flash.Visible = true;
        }

        private void Respawn()
        {
            if (Collidable) return;
            Collidable = true;

            sprite.Visible = true;
            outline.Visible = false;
            Depth = -100;
            wiggler.Start();
            Audio.Play("event:/game/general/diamond_return", Position);
            level.ParticlesFG.Emit(p_regen, 16, Position, Vector2.One * 2f);
        }

        private void UpdateY() => flash.Y = sprite.Y = bloom.Y = sine.Value * 2f;

        public override void Render()
        {
            if (sprite.Visible)
                sprite.DrawOutline();
            base.Render();
        }

        private void OnPlayer(Player player)
        {
            bool canUse = RefillsDash && player.Dashes < player.MaxDashes ||
                          RefillsStamina && player.Stamina < 20 ||
                          GravityHelperModule.Session.GravityRefillCharges < Charges;

            if (!canUse) return;

            if (RefillsDash) player.RefillDash();
            if (RefillsStamina) player.RefillStamina();
            GravityHelperModule.Session.GravityRefillCharges = Charges;

            Audio.Play("event:/game/general/diamond_touch", Position);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Medium);
            Collidable = false;
            Add(new Coroutine(RefillRoutine(player)));
            respawnTimeRemaining = RespawnTime;
        }

        private IEnumerator RefillRoutine(Player player)
        {
            GravityRefill refill = this;
            Celeste.Celeste.Freeze(0.05f);
            yield return null;

            refill.level.Shake();
            refill.sprite.Visible = refill.flash.Visible = false;
            if (!refill.OneUse)
                refill.outline.Visible = true;
            refill.Depth = 8999;
            yield return 0.05f;

            float direction = player.Speed.Angle();
            refill.level.ParticlesFG.Emit(refill.p_shatter, 5, refill.Position, Vector2.One * 4f, direction - (float)Math.PI / 2f);
            refill.level.ParticlesFG.Emit(refill.p_shatter, 5, refill.Position, Vector2.One * 4f, direction + (float)Math.PI / 2f);
            SlashFx.Burst(refill.Position, direction);

            if (refill.OneUse)
                refill.RemoveSelf();
        }
    }
}