// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;

namespace GravityHelper.Entities
{
    [CustomEntity("GravityHelper/GravityField")]
    [Tracked]
    public class GravityField : Trigger, IConnectableField
    {
        private static readonly Color gravity_normal_color = Color.Blue;
        private static readonly Color gravity_invert_color = Color.Red;
        private static readonly Color gravity_toggle_color = Color.Purple;

        public GravityType GravityType { get; }
        public VisualType ArrowType { get; }
        public VisualType FieldType { get; }
        public bool AttachToSolids { get; }

        private bool ShouldDrawArrows => !(ArrowType == VisualType.None || ArrowType == VisualType.Default && GravityType == GravityType.None); 
        private bool ShouldDrawField => !(FieldType == VisualType.None || FieldType == VisualType.Default && GravityType == GravityType.None);
        
        public Color FieldColor => colorForGravityType(FieldType == VisualType.Default ? GravityType : (GravityType) FieldType);

        private static Color colorForGravityType(GravityType type) => type switch
        {
            GravityType.Normal => gravity_normal_color,
            GravityType.Inverted => gravity_invert_color,
            GravityType.Toggle => gravity_toggle_color,
            _ => Color.White
        };

        private readonly Hitbox normalHitbox;
        private readonly Hitbox staticMoverHitbox;

        private List<Vector2> particles = new List<Vector2>();
        private float[] speeds = { 12f, 20f, 40f };
        private GravityFieldGroup fieldGroup;

        // public float Flashing => fieldGroup?.Flashing ?? 0f;

        public GravityField(EntityData data, Vector2 offset)
            : base(data, offset)
        {
            GravityType = (GravityType)data.Int("gravityType");
            AttachToSolids = data.Bool("attachToSolids");
            ArrowType = (VisualType)data.Int("arrowType", (int) VisualType.Default);
            FieldType = (VisualType)data.Int("fieldType", (int) VisualType.Default);

            Visible = ShouldDrawArrows || ShouldDrawField;
            Collider = normalHitbox = new Hitbox(data.Width, data.Height);

            staticMoverHitbox = new Hitbox(data.Width + 2, data.Height + 2, -1, -1);

            if (AttachToSolids)
                Add(new StaticMover
                {
                    OnAttach = p => Depth = p.Depth + 1,
                    SolidChecker = staticMoverCollideCheck
                });

            if (ShouldDrawArrows)
            {

            }

            if (ShouldDrawField)
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

        private GravityFieldRenderer getRenderer(Scene scene, bool create = false)
        {
            if (scene == null) return null;
            var renderer = scene.Entities.OfType<GravityFieldRenderer>().FirstOrDefault() ?? scene.Entities.ToAdd.OfType<GravityFieldRenderer>().FirstOrDefault();
            if (create && renderer == null)
                scene.Add(renderer = new GravityFieldRenderer());
            return renderer;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            if (ShouldDrawField)
            {
                var renderer = getRenderer(scene, true);
                renderer?.Track(this);
            }

            fieldGroup = null;
        }

        public override void Removed(Scene scene)
        {
            base.Removed(scene);

            if (ShouldDrawField)
            {
                var renderer = getRenderer(scene);
                renderer?.Untrack(this, true);
            }

            fieldGroup = null;
        }

        public override void Awake(Scene scene)
        {
            base.Awake(scene);

            if (ShouldDrawField)
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

            Color color = Color.White * 0.5f;
            foreach (Vector2 particle in particles)
                Draw.Pixel.Draw(Position + particle, Vector2.Zero, color);
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
