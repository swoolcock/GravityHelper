// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
// ReSharper disable RedundantArgumentDefaultValue

namespace Celeste.Mod.GravityHelper.Components;

public class SwapComponent : Component
{
    public SwapType Type;
    public Vector2 Direction;
    public Vector2 Target;
    public bool Swapping;
    public ParticleType ParticleType = SwapBlock.P_Move;

    private Vector2 start;
    private Vector2 end;
    private float lerp;
    private int target;
    private Rectangle moveRect;
    private float speed;
    private float maxForwardSpeed;
    private float maxBackwardSpeed;
    private float returnTimer;
    private MTexture[,] nineSliceTarget;
    private PathRenderer path;
    private EventInstance moveSfx;
    private EventInstance returnSfx;
    private DisplacementRenderer.Burst burst;
    private float particlesRemainder;

    public new Platform Entity => base.Entity as Platform;

    public static bool TryCreate(EntityData data, Vector2 offset, out SwapComponent component) =>
        TryCreate(data, offset, data.FirstNodeNullable(offset), out component);

    public static bool TryCreate(EntityData data, Vector2 offset, Vector2? target, out SwapComponent component)
    {
        var swapType = (SwapType)data.Int("swapType", (int)SwapType.None);

        if (swapType == SwapType.None)
        {
            component = null;
            return false;
        }

        component = new SwapComponent
        {
            Type = swapType,
            Target = target ?? Vector2.Zero,
        };

        return true;
    }

    public SwapComponent() : base(true, false)
    {
    }

    public override void Added(Entity entity)
    {
        if (entity is not Platform)
            throw new Exception("SwapComponent should only be used on Platforms!");

        base.Added(entity);

        start = entity.Position;
        end = Target;

        maxForwardSpeed = 360f / Vector2.Distance(start, end);
        maxBackwardSpeed = maxForwardSpeed * 0.4f;
        Direction.X = Math.Sign(end.X - start.X);
        Direction.Y = Math.Sign(end.Y - start.Y);

        entity.Add(new DashListener
        {
            OnDash = onDash
        });

        int x = (int)MathHelper.Min(start.X, end.X);
        int y = (int)MathHelper.Min(start.Y, end.Y);
        int num1 = (int)MathHelper.Max(start.X + entity.Width, end.X + entity.Width);
        int num2 = (int)MathHelper.Max(start.Y + entity.Height, end.Y + entity.Height);
        moveRect = new Rectangle(x, y, num1 - x, num2 - y);

        var targetTexture = GFX.Game["objects/swapblock/target"];
        nineSliceTarget = new MTexture[3, 3];
        for (int index1 = 0; index1 < 3; ++index1)
        {
            for (int index2 = 0; index2 < 3; ++index2)
            {
                nineSliceTarget[index1, index2] = targetTexture.GetSubtexture(new Rectangle(index1 * 8, index2 * 8, 8, 8));
            }
        }
    }

    private void onDash(Vector2 direction)
    {
        Swapping = lerp < 1.0;
        target = 1;
        returnTimer = 0.8f;
        burst = SceneAs<Level>().Displacement.AddBurst(Entity.Center, 0.2f, 0.0f, 16f);
        speed = lerp < 0.2f ? MathHelper.Lerp(maxForwardSpeed / 3f, maxForwardSpeed, lerp / 0.2f) : maxForwardSpeed;
        Audio.Stop(returnSfx);
        Audio.Stop(moveSfx);
        if (!Swapping)
            Audio.Play(SFX.game_05_swapblock_move_end, Entity.Center);
        else
            moveSfx = Audio.Play(SFX.game_05_swapblock_move, Entity.Center);
    }

    public override void EntityAwake()
    {
        base.EntityAwake();
        addPathRenderer();
    }

    private void addPathRenderer()
    {
        if (path != null || Entity == null) return;
        Scene.Add(path = new PathRenderer(this, Entity.Position));
    }

    public override void EntityRemoved(Scene scene)
    {
        base.EntityRemoved(scene);
        Audio.Stop(moveSfx);
        Audio.Stop(returnSfx);
    }

    public override void SceneEnd(Scene scene)
    {
        base.SceneEnd(scene);
        Audio.Stop(moveSfx);
        Audio.Stop(returnSfx);
    }

    public override void Update()
    {
        base.Update();

        addPathRenderer();

        if (returnTimer > 0f)
        {
            returnTimer -= Engine.DeltaTime;
            if (returnTimer <= 0f)
            {
                target = 0;
                speed = 0.0f;
                returnSfx = Audio.Play( SFX.game_05_swapblock_return, Entity.Center);
            }
        }

        if (burst != null)
            burst.Position = Entity.Center;

        speed = target != 1
            ? Calc.Approach(speed, maxBackwardSpeed, maxBackwardSpeed / 1.5f * Engine.DeltaTime)
            : Calc.Approach(speed, maxForwardSpeed, maxForwardSpeed / 0.2f * Engine.DeltaTime);

        float lerp = this.lerp;
        this.lerp = Calc.Approach(this.lerp, target, speed * Engine.DeltaTime);
        if (this.lerp != (double)lerp)
        {
            Vector2 liftSpeed = (end - start) * speed;
            Vector2 position1 = Entity.Position;
            if (target == 1)
                liftSpeed = (end - start) * maxForwardSpeed;
            if (this.lerp < (double)lerp)
                liftSpeed *= -1f;
            if (target == 1 && Scene.OnInterval(0.02f))
                MoveParticles(end - start);
            Entity.MoveTo(Vector2.Lerp(start, end, this.lerp), liftSpeed);
            Vector2 position2 = Entity.Position;
            if (position1 != position2)
            {
                Audio.Position(moveSfx, Entity.Center);
                Audio.Position(returnSfx, Entity.Center);
                if (Entity.Position == start && target == 0)
                {
                    Audio.SetParameter(returnSfx, "end", 1f);
                    Audio.Play(SFX.game_05_swapblock_return_end, Entity.Center);
                }
                else if (Entity.Position == end && target == 1)
                    Audio.Play(SFX.game_05_swapblock_move_end, Entity.Center);
            }
        }

        if (Swapping && this.lerp >= 1.0)
            Swapping = false;

        if (Entity is Solid solid)
            solid.StopPlayerRunIntoAnimation = this.lerp <= 0.0 || this.lerp >= 1.0;
    }

    private void MoveParticles(Vector2 normal)
    {
        Vector2 position;
        Vector2 vector2;
        float direction;
        float num;
        if (normal.X > 0.0)
        {
            position = Entity.CenterLeft;
            vector2 = Vector2.UnitY * (Entity.Height - 6f);
            direction = 3.1415927f;
            num = Math.Max(2f, Entity.Height / 14f);
        }
        else if (normal.X < 0.0)
        {
            position = Entity.CenterRight;
            vector2 = Vector2.UnitY * (Entity.Height - 6f);
            direction = 0.0f;
            num = Math.Max(2f, Entity.Height / 14f);
        }
        else if (normal.Y > 0.0)
        {
            position = Entity.TopCenter;
            vector2 = Vector2.UnitX * (Entity.Width - 6f);
            direction = -1.5707964f;
            num = Math.Max(2f, Entity.Width / 14f);
        }
        else
        {
            position = Entity.BottomCenter;
            vector2 = Vector2.UnitX * (Entity.Width - 6f);
            direction = 1.5707964f;
            num = Math.Max(2f, Entity.Width / 14f);
        }
        this.particlesRemainder += num;
        int particlesRemainder = (int) this.particlesRemainder;
        this.particlesRemainder -= particlesRemainder;
        Vector2 positionRange = vector2 * 0.5f;
        SceneAs<Level>().Particles.Emit(ParticleType, particlesRemainder, position, positionRange, direction);
    }

    private void DrawBlockStyle(
        Vector2 pos,
        float width,
        float height,
        MTexture[,] ninSlice,
        Sprite middle,
        Color color)
    {
        int num1 = (int) (width / 8.0);
        int num2 = (int) (height / 8.0);
        ninSlice[0, 0].Draw(pos + new Vector2(0.0f, 0.0f), Vector2.Zero, color);
        ninSlice[2, 0].Draw(pos + new Vector2(width - 8f, 0.0f), Vector2.Zero, color);
        ninSlice[0, 2].Draw(pos + new Vector2(0.0f, height - 8f), Vector2.Zero, color);
        ninSlice[2, 2].Draw(pos + new Vector2(width - 8f, height - 8f), Vector2.Zero, color);
        for (int index = 1; index < num1 - 1; ++index)
        {
            ninSlice[1, 0].Draw(pos + new Vector2(index * 8, 0.0f), Vector2.Zero, color);
            ninSlice[1, 2].Draw(pos + new Vector2(index * 8, height - 8f), Vector2.Zero, color);
        }
        for (int index = 1; index < num2 - 1; ++index)
        {
            ninSlice[0, 1].Draw(pos + new Vector2(0.0f, index * 8), Vector2.Zero, color);
            ninSlice[2, 1].Draw(pos + new Vector2(width - 8f, index * 8), Vector2.Zero, color);
        }
        for (int x = 1; x < num1 - 1; ++x)
        {
            for (int y = 1; y < num2 - 1; ++y)
                ninSlice[1, 1].Draw(pos + new Vector2(x, y) * 8f, Vector2.Zero, color);
        }
        if (middle == null)
            return;
        middle.Color = color;
        middle.RenderPosition = pos + new Vector2(width / 2f, height / 2f);
        middle.Render();
    }

    public enum SwapType
    {
        None,
        Return,
        Toggle,
    }

    private class PathRenderer : Entity
    {
        private SwapComponent swapComponent;
        private MTexture pathTexture;
        private MTexture clipTexture = new();
        private float timer;

        public PathRenderer(SwapComponent swapComponent, Vector2 position) : base(position)
        {
            this.swapComponent = swapComponent;
            Depth = 8999;
            pathTexture =
                GFX.Game["objects/swapblock/path" + (swapComponent.start.X == swapComponent.end.X ? "V" : "H")];
            timer = Calc.Random.NextFloat();
        }

        public override void Update()
        {
            base.Update();
            timer += Engine.DeltaTime * 4f;
        }

        public override void Render()
        {
            swapComponent.DrawBlockStyle(new Vector2(swapComponent.moveRect.X, swapComponent.moveRect.Y), swapComponent.moveRect.Width, swapComponent.moveRect.Height, swapComponent.nineSliceTarget, null, Color.White * (float) (0.5 * (0.5 + (Math.Sin(timer) + 1.0) * 0.25)));
        }
    }
}
