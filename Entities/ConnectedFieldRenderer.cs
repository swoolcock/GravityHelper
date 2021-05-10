// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;

namespace GravityHelper.Entities
{
    public class ConnectedFieldRenderer<TEntity> : Entity
        where TEntity : Entity, IConnectableField
    {
        public Color Color { get; }
        public float Alpha { get; }

        private List<TEntity> list = new List<TEntity>();
        private List<Edge> edges = new List<Edge>();
        private VirtualMap<bool> tiles;
        private Rectangle levelTileBounds;
        private bool dirty;

        public ConnectedFieldRenderer(Color color, float alpha = 0.15f)
        {
            Color = color;
            Alpha = alpha;
            Tag = Tags.TransitionUpdate;
            Depth = 1;
            Add(new CustomBloom(onRenderBloom));
        }

        public int TrackedCount => list.Count;

        public void Track(TEntity entity)
        {
            list.Add(entity);

            if (Scene != null && tiles != null)
            {
                for (int x = (int)entity.X / 8; x < entity.Right / 8.0; ++x)
                for (int y = (int)entity.Y / 8; y < entity.Bottom / 8.0; ++y)
                    tiles[x - levelTileBounds.X, y - levelTileBounds.Y] = true;
            }

            dirty = true;
        }

        public void Untrack(TEntity entity)
        {
            list.Remove(entity);

            if (list.Count == 0)
                tiles = null;
            else
            {
                for (int x = (int)entity.X / 8; x < entity.Right / 8.0; ++x)
                for (int y = (int)entity.Y / 8; y < entity.Bottom / 8.0; ++y)
                    tiles[x - levelTileBounds.X, y - levelTileBounds.Y] = false;
            }

            dirty = true;
        }

        public override void Added(Scene scene)
        {
            base.Added(scene);

            if (tiles == null)
            {
                levelTileBounds = SceneAs<Level>().TileBounds;
                tiles = new VirtualMap<bool>(levelTileBounds.Width, levelTileBounds.Height);

                foreach (var entity in list)
                {
                    for (int x = (int)entity.X / 8; x < entity.Right / 8.0; ++x)
                    for (int y = (int)entity.Y / 8; y < entity.Bottom / 8.0; ++y)
                        tiles[x - levelTileBounds.X, y - levelTileBounds.Y] = true;
                }

                dirty = true;
            }
        }

        public override void Update()
        {
            if (dirty)
                rebuildEdges();
            UpdateEdges();
        }

        public void UpdateEdges()
        {
            Camera camera = SceneAs<Level>().Camera;
            Rectangle view = new Rectangle((int)camera.Left - 4, (int)camera.Top - 4,
                (int)(camera.Right - (double)camera.Left) + 8,
                (int)(camera.Bottom - (double)camera.Top) + 8);
            for (int index = 0; index < edges.Count; ++index)
            {
                if (edges[index].Visible)
                {
                    if (Scene.OnInterval(0.25f, index * 0.01f) && !edges[index].InView(ref view))
                        edges[index].Visible = false;
                }
                else if (Scene.OnInterval(0.05f, index * 0.01f) && edges[index].InView(ref view))
                    edges[index].Visible = true;

                if (edges[index].Visible &&
                    (Scene.OnInterval(0.05f, index * 0.01f) || edges[index].Wave == null))
                    edges[index].UpdateWave(Scene.TimeActive * 3f);
            }
        }

        private void rebuildEdges()
        {
            dirty = false;
            edges.Clear();
            if (list.Count == 0)
                return;

            // Level level = SceneAs<Level>();
            // int left = level.TileBounds.Left;
            // Rectangle tileBounds = level.TileBounds;
            // int top = tileBounds.Top;
            // tileBounds = level.TileBounds;
            // int right = tileBounds.Right;
            // tileBounds = level.TileBounds;
            // int bottom = tileBounds.Bottom;
            Point[] pointArray =
            {
                new Point(0, -1),
                new Point(0, 1),
                new Point(-1, 0),
                new Point(1, 0)
            };

            foreach (var parent in list)
            {
                for (int x = (int)parent.X / 8; x < parent.Right / 8.0; ++x)
                {
                    for (int y = (int)parent.Y / 8; y < parent.Bottom / 8.0; ++y)
                    {
                        foreach (Point point1 in pointArray)
                        {
                            Point point2 = new Point(-point1.Y, point1.X);
                            if (!inside(x + point1.X, y + point1.Y) &&
                                (!inside(x - point2.X, y - point2.Y) || inside(x + point1.X - point2.X, y + point1.Y - point2.Y)))
                            {
                                Point point3 = new Point(x, y);
                                Point point4 = new Point(x + point2.X, y + point2.Y);
                                Vector2 vector2 = new Vector2(4f) + new Vector2(point1.X - point2.X,
                                    point1.Y - point2.Y) * 4f;
                                for (;
                                    inside(point4.X, point4.Y) && !inside(point4.X + point1.X, point4.Y + point1.Y);
                                    point4.Y += point2.Y)
                                    point4.X += point2.X;
                                Vector2 a = new Vector2(point3.X, point3.Y) * 8f + vector2 - parent.Position;
                                Vector2 b = new Vector2(point4.X, point4.Y) * 8f + vector2 - parent.Position;
                                edges.Add(new Edge(parent, a, b));
                            }
                        }
                    }
                }
            }
        }

        private bool inside(int tx, int ty) => tiles[tx - levelTileBounds.X, ty - levelTileBounds.Y];

        private void onRenderBloom()
        {
            // Camera camera = SceneAs<Level>().Camera;
            // Rectangle rectangle = new Rectangle((int)camera.Left, (int)camera.Top,
            //     (int)(camera.Right - (double)camera.Left), (int)(camera.Bottom - (double)camera.Top));

            foreach (var entity in list)
            {
                if (entity.Visible)
                    Draw.Rect(entity.X, entity.Y, entity.Width, entity.Height, Color.White);
            }

            foreach (var edge in edges)
            {
                if (edge.Visible)
                {
                    Vector2 vector2_1 = edge.Parent.Position + edge.A;
                    // Vector2 vector2_2 = edge.Parent.Position + edge.B;
                    for (int index = 0; index <= (double)edge.Length; ++index)
                    {
                        Vector2 start = vector2_1 + edge.Normal * index;
                        Draw.Line(start, start + edge.Perpendicular * edge.Wave[index], Color.White);
                    }
                }
            }
        }

        public override void Render()
        {
            if (list.Count <= 0)
                return;
            Color color1 = Color * Alpha;
            // Color color2 = Color.White * 0.25f;
            foreach (var entity in list)
            {
                if (entity.Visible)
                    Draw.Rect(entity.Collider, color1);
            }

            if (edges.Count == 0)
                return;

            foreach (var edge in edges)
            {
                if (edge.Visible)
                {
                    Vector2 vector2_1 = edge.Parent.Position + edge.A;
                    // Vector2 vector2_2 = edge.Parent.Position + edge.B;
                    // Color.Lerp(color2, Color.White, edge.Parent.Flash);
                    for (int index = 0; index <= (double)edge.Length; ++index)
                    {
                        Vector2 start = vector2_1 + edge.Normal * index;
                        Draw.Line(start, start + edge.Perpendicular * edge.Wave[index], color1);
                    }
                }
            }
        }

        private class Edge
        {
            public TEntity Parent;
            public bool Visible;
            public Vector2 A;
            public Vector2 B;
            public Vector2 Min;
            public Vector2 Max;
            public Vector2 Normal;
            public Vector2 Perpendicular;
            public float[] Wave;
            public float Length;

            public Edge(TEntity parent, Vector2 a, Vector2 b)
            {
                Parent = parent;
                Visible = true;
                A = a;
                B = b;
                Min = new Vector2(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
                Max = new Vector2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
                Normal = (b - a).SafeNormalize();
                Perpendicular = -Normal.Perpendicular();
                Length = (a - b).Length();
            }

            public void UpdateWave(float time)
            {
                if (Wave == null || Wave.Length <= (double)Length)
                    Wave = new float[(int)Length + 2];
                for (int index = 0; index <= (double)Length; ++index)
                    Wave[index] = GetWaveAt(time, index, Length);
            }

            private float GetWaveAt(float offset, float along, float length)
            {
                if (along <= 1.0 || along >= length - 1.0)// || (double)Parent.Solidify >= 1.0)
                    return 0.0f;
                float num = offset + along * 0.25f;
                return (float)((1.0 + (Math.Sin(num) * 2.0 + Math.Sin(num * 0.25)) *
                    Ease.SineInOut(Calc.YoYo(along / length))));// * (1.0 - (double)Parent.Solidify));
            }

            public bool InView(ref Rectangle view) => view.Left < Parent.X + (double)Max.X &&
                                                      view.Right > Parent.X + (double)Min.X &&
                                                      view.Top < Parent.Y + (double)Max.Y &&
                                                      view.Bottom > Parent.Y + (double)Min.Y;
        }
    }
}
