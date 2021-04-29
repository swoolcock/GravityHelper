using System;
using Celeste;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;

namespace GravityHelper.Entities
{
    public class UpsideDownTalkComponentUI : TalkComponent.TalkComponentUI
    {
        public UpsideDownTalkComponentUI(TalkComponent handler) : base(handler)
        {
        }

        public override void Render()
        {
            var data = new DynData<TalkComponent.TalkComponentUI>(this);
            float slide = data.Get<float>(nameof(slide));
            float timer = data.Get<float>(nameof(timer));
            float alpha = data.Get<float>(nameof(alpha));
            Wiggler wiggler = data.Get<Wiggler>(nameof(wiggler));
            Color lineColor = data.Get<Color>(nameof(lineColor));

            Level level = SceneAs<Level>();
            if (level.FrozenOrPaused || slide <= 0f || Handler.Entity == null)
                return;

            Vector2 value = level.Camera.Position.Floor();
            Vector2 vector = Handler.Entity.Position + Handler.DrawAt - value;
            if (SaveData.Instance != null && SaveData.Instance.Assists.MirrorMode)
                vector.X = 320f - vector.X;

            vector.X *= 6f;
            vector.Y *= 6f;
            vector.Y -= (float)Math.Sin(timer * 4f) * 12f + 64f * (1f - Ease.CubeOut(slide));
            float num = !Highlighted ? 1f + wiggler.Value * 0.5f : 1f - wiggler.Value * 0.5f;
            float scale = Ease.CubeInOut(slide) * alpha;
            Color value2 = lineColor * scale;

            if (!Highlighted)
                GFX.Gui["hover/idle"].DrawJustified(vector, new Vector2(0.5f, 1f), value2 * alpha, new Vector2(num, -num));
            else
            {
                Handler.HoverUI.Texture.DrawJustified(vector, new Vector2(0.5f, 1f), value2 * alpha, new Vector2(num, -num));
                Vector2 position = vector - Handler.HoverUI.InputPosition * num + Vector2.UnitY;
                if (Input.GuiInputController(Input.PrefixMode.Latest))
                    Input.GuiButton(Input.Talk, "controls/keyboard/oemquestion").DrawJustified(position, new Vector2(0.5f), Color.White * scale, num);
                else
                    ActiveFont.DrawOutline(Input.FirstKey(Input.Talk).ToString().ToUpper(), position, new Vector2(0.5f), new Vector2(num), Color.White * scale, 2f, Color.Black);
            }
        }
    }
}