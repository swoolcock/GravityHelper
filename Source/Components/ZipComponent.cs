// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Monocle;

// ReSharper disable RedundantArgumentDefaultValue

namespace Celeste.Mod.GravityHelper.Components;

public class ZipComponent : Component
{
    public ZipType Type;
    public Vector2 Target;
    public ParticleType ScrapeParticles = ZipMover.P_Scrape;
    public ParticleType SparksParticles = ZipMover.P_Sparks;

    public Color RopeColor;
    public Color RopeLightColor;

    private SoundSource sfx = new SoundSource();
    private Coroutine coroutine;
    private Vector2 start;
    private float percent;
    private PathRenderer pathRenderer;
    private bool drawBlackBorder;

    public new Solid Entity => base.Entity as Solid;

    public static bool TryCreate(EntityData data, Vector2 offset, out ZipComponent component) =>
        TryCreate(data, offset, data.FirstNodeNullable(offset), out component);

    public static bool TryCreate(EntityData data, Vector2 offset, Vector2? target, out ZipComponent component)
    {
        var zipType = (ZipType)data.Int("zipType", (int)ZipType.None);

        if (zipType == ZipType.None)
        {
            component = null;
            return false;
        }

        component = new ZipComponent
        {
            Type = zipType,
            Target = target ?? Vector2.Zero,
            RopeColor = data.HexColor("zipRopeColor", ZipMover.ropeColor),
            RopeLightColor = data.HexColor("zipRopeLightColor", ZipMover.ropeLightColor),
        };

        return true;
    }

    public ZipComponent() : base(true, false)
    {
    }

    private void addPathRenderer()
    {
        if (pathRenderer != null || Entity == null) return;
        Scene.Add(pathRenderer = new PathRenderer(this));
    }

    public override void Added(Entity entity)
    {
        if (entity is not Solid)
            throw new Exception("ZipComponent should only be used on Solids!");

        base.Added(entity);

        start = Entity.Position;

        sfx.Position = new Vector2(Entity.Width, Entity.Height) / 2f;
        Entity.Add(sfx);
        Entity.Add(coroutine = new Coroutine(zipSequence()));
    }

    public override void Removed(Entity entity)
    {
        pathRenderer?.RemoveSelf();
        pathRenderer = null;
        coroutine?.RemoveSelf();
        coroutine = null;
        sfx?.RemoveSelf();
        sfx = null;
        base.Removed(entity);
    }

    public override void EntityAdded(Scene scene)
    {
        base.EntityAdded(scene);
        addPathRenderer();
    }

    public override void EntityRemoved(Scene scene)
    {
        base.EntityRemoved(scene);
        pathRenderer?.RemoveSelf();
        pathRenderer = null;
    }

    public void ScrapeParticlesCheck(Vector2 to)
    {
        if (!Scene.OnInterval(0.03f))
            return;
        bool flag1 = to.Y != Entity.ExactPosition.Y;
        bool flag2 = to.X != Entity.ExactPosition.X;
        if (flag1 && !flag2)
        {
            int num1 = Math.Sign(to.Y - Entity.ExactPosition.Y);
            Vector2 vector2 = num1 != 1 ? Entity.TopLeft : Entity.BottomLeft;
            int num2 = 4;
            if (num1 == 1)
                num2 = Math.Min((int) Entity.Height - 12, 20);
            int num3 = (int) Entity.Height;
            if (num1 == -1)
                num3 = Math.Max(16 /*0x10*/, (int) Entity.Height - 16 /*0x10*/);
            if (Scene.CollideCheck<Solid>(vector2 + new Vector2(-2f, num1 * -2)))
            {
                for (int index = num2; index < num3; index += 8)
                    SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, Entity.TopLeft + new Vector2(0.0f, index + num1 * 2f), num1 == 1 ? -0.7853982f : 0.7853982f);
            }
            if (!Scene.CollideCheck<Solid>(vector2 + new Vector2(Entity.Width + 2f, num1 * -2)))
                return;
            for (int index = num2; index < num3; index += 8)
                SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, Entity.TopRight + new Vector2(-1f, index + num1 * 2f), num1 == 1 ? -2.3561945f : 2.3561945f);
        }
        else
        {
            if (!flag2 || flag1)
                return;
            int num4 = Math.Sign(to.X - Entity.ExactPosition.X);
            Vector2 vector2 = num4 != 1 ? Entity.TopLeft : Entity.TopRight;
            int num5 = 4;
            if (num4 == 1)
                num5 = Math.Min((int) Entity.Width - 12, 20);
            int num6 = (int) Entity.Width;
            if (num4 == -1)
                num6 = Math.Max(16 /*0x10*/, (int) Entity.Width - 16 /*0x10*/);
            if (Scene.CollideCheck<Solid>(vector2 + new Vector2(num4 * -2, -2f)))
            {
                for (int index = num5; index < num6; index += 8)
                    SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, Entity.TopLeft + new Vector2(index + num4 * 2f, -1f), num4 == 1 ? 2.3561945f : 0.7853982f);
            }
            if (!Scene.CollideCheck<Solid>(vector2 + new Vector2(num4 * -2, Entity.Height + 2f)))
                return;
            for (int index = num5; index < num6; index += 8)
                SceneAs<Level>().ParticlesFG.Emit(ZipMover.P_Scrape, Entity.BottomLeft + new Vector2(index + num4 * 2f, 0.0f), num4 == 1 ? -2.3561945f : -0.7853982f);
        }
    }

    private IEnumerator zipSequence()
    {
        while (true)
        {
            while (!Entity.HasPlayerRider())
                yield return null;

            sfx.Play(SFX.game_01_zipmover);
            Input.Rumble(RumbleStrength.Medium, RumbleLength.Short);
            Entity.StartShaking(0.1f);
            yield return 0.1f;
            // zipMover.streetlight.SetAnimationFrame(3);
            Entity.StopPlayerRunIntoAnimation = false;
            float at = 0.0f;
            while (at < 1.0f)
            {
                yield return null;
                at = Calc.Approach(at, 1f, 2f * Engine.DeltaTime);
                percent = Ease.SineIn(at);
                Vector2 vector2 = Vector2.Lerp(start, Target, percent);
                ScrapeParticlesCheck(vector2);
                if (Scene.OnInterval(0.1f))
                    pathRenderer.CreateSparks();
                Entity.MoveTo(vector2);
            }
            Entity.StartShaking(0.2f);
            Input.Rumble(RumbleStrength.Strong, RumbleLength.Medium);
            Entity.SceneAs<Level>().Shake();
            Entity.StopPlayerRunIntoAnimation = true;
            yield return 0.5f;
            Entity.StopPlayerRunIntoAnimation = false;
            // zipMover.streetlight.SetAnimationFrame(2);
            at = 0.0f;
            while (at < 1.0f)
            {
                yield return null;
                at = Calc.Approach(at, 1f, 0.5f * Engine.DeltaTime);
                percent = 1f - Ease.SineIn(at);
                Vector2 position = Vector2.Lerp(Target, start, Ease.SineIn(at));
                Entity.MoveTo(position);
            }
            Entity.StopPlayerRunIntoAnimation = true;
            Entity.StartShaking(0.2f);
            // zipMover.streetlight.SetAnimationFrame(1);
            yield return 0.5f;
        }
    }

    public enum ZipType
    {
        None,
        Return,
        Toggle,
        Stay,
    }

    private class PathRenderer : Entity
    {
        private ZipComponent zipComponent;
        private MTexture cog;
        private Vector2 from;
        private Vector2 to;
        private Vector2 sparkAdd;
        private float sparkDirFromA;
        private float sparkDirFromB;
        private float sparkDirToA;
        private float sparkDirToB;

        public PathRenderer(ZipComponent zipComponent)
        {
            Depth = 5000;
            this.zipComponent = zipComponent;
            from = zipComponent.start + new Vector2(zipComponent.Entity.Width / 2f, zipComponent.Entity.Height / 2f);
            to = zipComponent.Target + new Vector2(zipComponent.Entity.Width / 2f, zipComponent.Entity.Height / 2f);
            sparkAdd = (from - to).SafeNormalize(5f).Perpendicular();
            float num = (from - to).Angle();
            sparkDirFromA = num + 0.3926991f;
            sparkDirFromB = num - 0.3926991f;
            sparkDirToA = (float) (num + 3.1415927410125732 - 0.39269909262657166);
            sparkDirToB = (float) (num + 3.1415927410125732 + 0.39269909262657166);
            // if (zipMover.theme == ZipMover.Themes.Moon)
            //     cog = GFX.Game["objects/zipmover/moon/cog"];
            // else
            cog = GFX.Game["objects/zipmover/cog"];
        }

        public void CreateSparks()
        {
            SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, from + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromA);
            SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, from - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirFromB);
            SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, to + sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToA);
            SceneAs<Level>().ParticlesBG.Emit(ZipMover.P_Sparks, to - sparkAdd + Calc.Random.Range(-Vector2.One, Vector2.One), sparkDirToB);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Render()
        {
            DrawCogs(Vector2.UnitY, Color.Black);
            DrawCogs(Vector2.Zero);
            if (!zipComponent.drawBlackBorder)
                return;
            var entity = zipComponent.Entity;
            Draw.Rect(new Rectangle(
                (int)(entity.X + entity.Shake.X - 1f),
                (int)(entity.Y + entity.Shake.Y - 1f),
                (int)entity.Width + 2,
                (int)entity.Height + 2), Color.Black);
        }

        private void DrawCogs(Vector2 offset, Color? colorOverride = null)
        {
            Vector2 vector = (to - from).SafeNormalize();
            Vector2 vector2_1 = vector.Perpendicular() * 3f;
            Vector2 vector2_2 = -vector.Perpendicular() * 4f;
            float rotation = (float) (zipComponent.percent * 3.1415927410125732 * 2.0);
            Draw.Line(from + vector2_1 + offset, to + vector2_1 + offset, colorOverride ?? zipComponent.RopeColor);
            Draw.Line(from + vector2_2 + offset, to + vector2_2 + offset, colorOverride ?? zipComponent.RopeColor);
            for (float num = (float) (4.0 - zipComponent.percent * 3.1415927410125732 * 8.0 % 4.0); num < (double) (to - from).Length(); num += 4f)
            {
                Vector2 vector2_3 = from + vector2_1 + vector.Perpendicular() + vector * num;
                Vector2 vector2_4 = to + vector2_2 - vector * num;
                Draw.Line(vector2_3 + offset, vector2_3 + vector * 2f + offset, colorOverride ?? zipComponent.RopeLightColor);
                Draw.Line(vector2_4 + offset, vector2_4 - vector * 2f + offset, colorOverride ?? zipComponent.RopeLightColor);
            }
            cog.DrawCentered(from + offset, colorOverride ?? Color.White, 1f, rotation);
            cog.DrawCentered(to + offset, colorOverride ?? Color.White, 1f, rotation);
        }
    }
}
