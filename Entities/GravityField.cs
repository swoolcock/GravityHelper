// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.GravityHelper.Entities
{
    [CustomEntity("GravityHelper/GravityField")]
    [Tracked]
    public class GravityField : Trigger, IConnectableField
    {
        public static readonly Color GRAVITY_NORMAL_COLOR = Color.Blue;
        public static readonly Color GRAVITY_INVERT_COLOR = Color.Red;
        public static readonly Color GRAVITY_TOGGLE_COLOR = Color.Purple;

        public const float DEFAULT_ARROW_OPACITY = 0.5f;
        public const float DEFAULT_FIELD_OPACITY = 0.15f;
        public const float DEFAULT_PARTICLE_OPACITY = 0.5f;

        #region Entity Properties

        public GravityType GravityType { get; }
        public VisualType ArrowType { get; }
        public VisualType FieldType { get; }
        public bool AttachToSolids { get; }

        private float? _arrowOpacity;
        public float ArrowOpacity => _arrowOpacity ?? DEFAULT_ARROW_OPACITY;

        private float? _fieldOpacity;
        public float FieldOpacity => _fieldOpacity ?? DEFAULT_FIELD_OPACITY;

        private float? _particleOpacity;
        public float ParticleOpacity => _particleOpacity ?? DEFAULT_PARTICLE_OPACITY;

        #endregion

        public Color FieldColor => colorForGravityType(fieldGravityType) * FieldOpacity;

        private GravityType arrowGravityType => ArrowType == VisualType.Default ? GravityType : (GravityType) ArrowType;
        private GravityType fieldGravityType => FieldType == VisualType.Default ? GravityType : (GravityType) FieldType;

        private static Color colorForGravityType(GravityType type) => type switch
        {
            GravityType.Normal => GRAVITY_NORMAL_COLOR,
            GravityType.Inverted => GRAVITY_INVERT_COLOR,
            GravityType.Toggle => GRAVITY_TOGGLE_COLOR,
            _ => Color.White
        };

        private readonly bool shouldDrawArrows;
        private readonly bool shouldDrawField;
        private readonly MTexture arrowTexture;
        private readonly MTexture arrowSmallTexture;
        private readonly SpriteEffects arrowSpriteEffects;
        private readonly Vector2 arrowOrigin;
        private readonly Vector2 arrowSmallOrigin;
        private Vector2 arrowShakeOffset;

        private readonly Hitbox normalHitbox;
        private readonly Hitbox staticMoverHitbox;

        private readonly List<Vector2> particles = new List<Vector2>();
        private readonly float[] speeds = {12f, 20f, 40f};
        private GravityFieldGroup fieldGroup;

        public GravityField(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            GravityType = (GravityType)data.Int("gravityType");
            AttachToSolids = data.Bool("attachToSolids");
            ArrowType = (VisualType)data.Int("arrowType", (int) VisualType.Default);
            FieldType = (VisualType)data.Int("fieldType", (int) VisualType.Default);

            if (float.TryParse(data.Attr("arrowOpacity"), out var arrowOpacity))
                _arrowOpacity = Calc.Clamp(arrowOpacity, 0f, 1f);

            if (float.TryParse(data.Attr("particleOpacity"), out var particleOpacity))
                _particleOpacity = Calc.Clamp(particleOpacity, 0f, 1f);

            if (float.TryParse(data.Attr("fieldOpacity"), out var fieldOpacity))
                _fieldOpacity = Calc.Clamp(fieldOpacity, 0f, 1f);

            shouldDrawArrows = !(ArrowType == VisualType.None || ArrowType == VisualType.Default && GravityType == GravityType.None);
            shouldDrawField = !(FieldType == VisualType.None || FieldType == VisualType.Default && GravityType == GravityType.None);

            Visible = shouldDrawArrows || shouldDrawField;
            Collider = normalHitbox = new Hitbox(data.Width, data.Height);

            if (shouldDrawArrows && !shouldDrawField)
                Depth = Depths.FGDecals - 1;
            else
                Depth = Depths.Player + 1;

            staticMoverHitbox = new Hitbox(data.Width + 2, data.Height + 2, -1, -1);

            if (AttachToSolids)
            {
                Add(new StaticMover
                {
                    OnAttach = p => Depth = p.Depth - 1,
                    SolidChecker = staticMoverCollideCheck,
                    OnShake = amount => arrowShakeOffset += amount,
                });
            }

            if (shouldDrawArrows)
            {
                switch (arrowGravityType)
                {
                    case GravityType.Normal:
                        arrowTexture = GFX.Game["objects/GravityHelper/gravityField/arrow"];
                        arrowSmallTexture = GFX.Game["objects/GravityHelper/gravityField/arrowSmall"];
                        arrowSpriteEffects = SpriteEffects.FlipVertically;
                        break;

                    case GravityType.Inverted:
                        arrowTexture = GFX.Game["objects/GravityHelper/gravityField/arrow"];
                        arrowSmallTexture = GFX.Game["objects/GravityHelper/gravityField/arrowSmall"];
                        arrowSpriteEffects = SpriteEffects.None;
                        break;

                    case GravityType.Toggle:
                        arrowTexture = GFX.Game["objects/GravityHelper/gravityField/doubleArrow"];
                        arrowSmallTexture = GFX.Game["objects/GravityHelper/gravityField/doubleArrowSmall"];
                        arrowSpriteEffects = SpriteEffects.None;
                        break;
                }

                arrowOrigin = new Vector2(arrowTexture.Width / 2f, arrowTexture.Height / 2f);
                arrowSmallOrigin = new Vector2(arrowSmallTexture.Width / 2f, arrowSmallTexture.Height / 2f);
            }

            if (shouldDrawField)
            {
                for (int index = 0; index < Width * (double) Height / 16.0; ++index)
                    particles.Add(new Vector2(Calc.Random.NextFloat(Width - 1f), Calc.Random.NextFloat(Height - 1f)));
            }
        }

        public override void OnEnter(Player player)
        {
            base.OnEnter(player);

            if (GravityType == GravityType.None || fieldGroup == null) return;

            fieldGroup.Semaphore++;

            if (fieldGroup.Semaphore == 1)
                GravityHelperModule.Instance.SetGravity(GravityType);
        }

        public override void OnStay(Player player)
        {
            base.OnStay(player);

            if (GravityType == GravityType.None || GravityType == GravityType.Toggle)
                return;

            if (GravityType != GravityHelperModule.Instance.Gravity)
                GravityHelperModule.Instance.SetGravity(GravityType);
        }

        public override void OnLeave(Player player)
        {
            base.OnLeave(player);

            if (fieldGroup == null) return;
            fieldGroup.Semaphore--;
        }

        private GravityController getController(Scene scene)
        {
            if (scene == null) return null;
            var controller = scene.Entities.OfType<GravityController>().FirstOrDefault() ??
                             scene.Entities.ToAdd.OfType<GravityController>().FirstOrDefault();
            return controller;
        }

        private GravityFieldRenderer getRenderer(Scene scene, bool create = false)
        {
            if (scene == null) return null;
            var renderer = scene.Entities.OfType<GravityFieldRenderer>().FirstOrDefault() ??
                           scene.Entities.ToAdd.OfType<GravityFieldRenderer>().FirstOrDefault();
            if (create && renderer == null)
                scene.Add(renderer = new GravityFieldRenderer());
            return renderer;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            if (getController(scene) is { } controller)
            {
                _arrowOpacity ??= controller.ArrowOpacity;
                _fieldOpacity ??= controller.FieldOpacity;
                _particleOpacity ??= controller.ParticleOpacity;
            }

            if (shouldDrawField)
            {
                var renderer = getRenderer(scene, true);
                renderer?.Track(this);
            }

            arrowShakeOffset = Vector2.Zero;
            fieldGroup = null;
        }

        public override void Removed(Scene scene)
        {
            base.Removed(scene);

            if (shouldDrawField)
            {
                var renderer = getRenderer(scene);
                renderer?.Untrack(this, true);
            }

            fieldGroup = null;
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);
            buildFieldGroup();
        }

        public override void Update()
        {
            int length = speeds.Length;
            float height = Height;
            int index = 0;

            for (int count = particles.Count; index < count; ++index)
            {
                bool flip = GravityType == GravityType.Inverted || GravityType == GravityType.Toggle && index % 2 == 1;
                Vector2 target = particles[index] + Vector2.UnitY * speeds[index % length] * Engine.DeltaTime * (flip ? -1 : 1);

                if (target.Y < 0)
                    target.Y += height;
                else if (target.Y >= height)
                    target.Y -= height;

                particles[index] = target;
            }

            base.Update();
        }

        public override void Render()
        {
            base.Render();

            if (shouldDrawField)
            {
                Color color = Color.White * ParticleOpacity;
                foreach (Vector2 particle in particles)
                    Draw.Pixel.Draw(Position + particle, Vector2.Zero, color);
            }

            if (shouldDrawArrows)
            {
                int widthInTiles = (int) (Width / 8);
                int heightInTiles = (int) (Height / 8);

                // one arrow every 2 tiles, rounded down, but at least one
                int arrowsX = Math.Max(widthInTiles / 2, 1);
                int arrowsY = Math.Max(heightInTiles / 2, 1);

                // if width or height is 1, scale down the arrows
                var texture = widthInTiles == 1 || heightInTiles == 1 ? arrowSmallTexture : arrowTexture;
                var origin = widthInTiles == 1 || heightInTiles == 1 ? arrowSmallOrigin : arrowOrigin;
                var color = shouldDrawField ? Color.White * ArrowOpacity : Color.White;

                // arrows should be centre aligned in each 2x2 box
                // offset by half a tile if the width or height is odd
                for (int y = 0; y < arrowsY; y++)
                {
                    int offsetY = y * 16 + 8 + heightInTiles % 2 * 4;
                    if (heightInTiles == 1) offsetY = 4;
                    for (int x = 0; x < arrowsX; x++)
                    {
                        int offsetX = x * 16 + 8 + widthInTiles % 2 * 4;
                        if (widthInTiles == 1) offsetX = 4;
                        texture.Draw(Position + arrowShakeOffset + new Vector2(offsetX, offsetY), origin, color, 1f, 0f, arrowSpriteEffects);
                    }
                }
            }
        }

        private bool staticMoverCollideCheck(Solid solid)
        {
            if (solid is SolidTiles) return false;
            // TODO: allow the mapper to specify the static mover collision hitbox
            // Collider = staticMoverHitbox;
            var collides = CollideCheck(solid);
            // Collider = normalHitbox;
            return collides;
        }

        private bool canConnectTo(GravityField other) =>
            other.GravityType == GravityType &&
            other.AttachToSolids == AttachToSolids;

        private IEnumerable<GravityField> getAdjacent()
        {
            if (!Scene.Tracker.Entities.ContainsKey(typeof(GravityField)))
                return Enumerable.Empty<GravityField>();

            var adjacent = new List<GravityField>();
            Scene.CollideInto(new Rectangle((int)X, (int)Y - 2, (int)Width, (int)Height + 4), adjacent);
            Scene.CollideInto(new Rectangle((int)X - 2, (int)Y, (int)Width + 4, (int)Height), adjacent);
            adjacent.Remove(this);
            adjacent.RemoveAll(f => !canConnectTo(f));
            return adjacent;
        }

        private void buildFieldGroup(GravityFieldGroup existing = null)
        {
            if (fieldGroup != null) return;

            fieldGroup = existing ?? new GravityFieldGroup();

            var adjacent = getAdjacent();
            foreach (var field in adjacent)
                field.buildFieldGroup(fieldGroup);
        }

        [Tracked]
        internal class GravityFieldRenderer : ConnectedFieldRenderer<GravityField>
        {
        }

        private class GravityFieldGroup
        {
            public int Semaphore;
        }

        public enum VisualType
        {
            Default = -2,
            None = -1,
            Normal = 0,
            Inverted,
            Toggle,
        }
    }
}
