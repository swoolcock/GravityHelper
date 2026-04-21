// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Xml;
using Microsoft.Xna.Framework;
using Monocle;

// ReSharper disable UnusedMember.Global

namespace Celeste.Mod.GravityHelper.Extensions;

internal static class BasicExtensions
{
    public static int ClampLower(this int self, int min) => Math.Max(self, min);
    public static int ClampUpper(this int self, int max) => Math.Min(self, max);
    public static int Clamp(this int self, int min, int max) => Calc.Clamp(self, min, max);
    public static int Mod(this int self, int divisor) => ((self % divisor) + divisor) % divisor;

    public static float ClampLower(this float self, float min) => Math.Max(self, min);
    public static float ClampUpper(this float self, float max) => Math.Min(self, max);
    public static float Clamp(this float self, float min, float max) => Calc.Clamp(self, min, max);
    public static float Clamp01(this float self) => Calc.Clamp(self, 0f, 1f);
    public static float Mod(this float self, float divisor) => ((self % divisor) + divisor) % divisor;

    public static TItem AddWithDescription<TItem>(this TextMenu self, TItem item, string description)
        where TItem : TextMenu.Item
    {
        self.Add(item);
        item.AddDescription(self, description);
        return item;
    }

    public static void AddSubHeader(this TextMenu self, string subHeader)
        => self.Add(new TextMenu.SubHeader(subHeader.DialogCleanOrNull() ?? subHeader, false));

    public static void PlayIfAvailable(this Sprite self, string id, bool restart = false, bool randomizeFrame = false)
    {
        if (self.Has(id)) self.Play(id, restart, randomizeFrame);
    }

    public static Sprite CreateWithPath(this SpriteBank self, string id, string overridePath)
    {
        return self.SpriteData.ContainsKey(id) ? self.SpriteData[id].CreateWithPath(overridePath) : throw new Exception($"Missing animation name in SpriteData: '{id}'!");
    }

    public static Sprite CreateOnWithPath(this SpriteBank self, Sprite sprite, string id, string overridePath)
    {
        if (self.SpriteData.ContainsKey(id))
            return self.SpriteData[id].CreateOnWithPath(sprite, overridePath);
        throw new Exception($"Missing animation name in SpriteData: '{id}'!");
    }

    public static Sprite CreateWithPath(this SpriteData self, string overridePath) =>
        self.CreateOnWithPath(new Sprite(), overridePath);

    public static Sprite CreateOnWithPath(this SpriteData self, Sprite sprite, string overridePath)
    {
        if (self.Sources.FirstOrDefault() is not { } spriteDataSource)
        {
            Logger.Log(nameof(GravityHelperModule), "CreateOnWithPath: Couldn't get sprite source.");
            return null;
        }

        sprite.Reset(self.Atlas, overridePath);
        float defaultDelay = spriteDataSource.XML.AttrFloat("delay", 0f);

        foreach (XmlElement animXml in spriteDataSource.XML.GetElementsByTagName("Anim"))
        {
            Chooser<string> into = !animXml.HasAttr("goto") ? null : Chooser<string>.FromString<string>(animXml.Attr("goto"));
            string id = animXml.Attr("id");
            string pathAttr = animXml.Attr("path", "");
            int[] frames = Calc.ReadCSVIntWithTricks(animXml.Attr("frames", ""));
            sprite.Add(id, pathAttr, animXml.AttrFloat("delay", defaultDelay), into, frames);
        }

        foreach (XmlElement loopXml in spriteDataSource.XML.GetElementsByTagName("Loop"))
        {
            string id = loopXml.Attr("id");
            string pathAttr = loopXml.Attr("path", "");
            int[] frames = Calc.ReadCSVIntWithTricks(loopXml.Attr("frames", ""));
            sprite.AddLoop(id, pathAttr, loopXml.AttrFloat("delay", defaultDelay), frames);
        }

        if (spriteDataSource.XML.HasChild("Center"))
        {
            sprite.CenterOrigin();
            sprite.Justify = new Vector2(0.5f, 0.5f);
        }
        else if (spriteDataSource.XML.HasChild("Justify"))
        {
            sprite.JustifyOrigin(spriteDataSource.XML.ChildPosition("Justify"));
            sprite.Justify = spriteDataSource.XML.ChildPosition("Justify");
        }
        else if (spriteDataSource.XML.HasChild("Origin"))
            sprite.Origin = spriteDataSource.XML.ChildPosition("Origin");
        if (spriteDataSource.XML.HasChild("Position"))
            sprite.Position = spriteDataSource.XML.ChildPosition("Position");
        if (spriteDataSource.XML.HasAttr("start"))
            sprite.Play(spriteDataSource.XML.Attr("start"));
        return sprite;
    }
}
