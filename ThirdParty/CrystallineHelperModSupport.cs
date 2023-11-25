// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Reflection;
using Celeste.Mod.GravityHelper.Components;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mod.GravityHelper.ThirdParty;

// [ThirdPartyMod("CrystallineHelper")]
public class CrystallineHelperModSupport : ThirdPartyModSupport
{
    // ReSharper disable InconsistentNaming
    private static IDetour hook_ForceDashCrystal_UpdateSprite;
    private static IDetour hook_ForceDashCrystal_Render;
    private static IDetour hook_TeleCrystal_Render;
    // ReSharper restore InconsistentNaming

    protected override void Load()
    {
        var chfdct = ReflectionCache.CrystallineHelperForceDashCrystalType;
        var chtct = ReflectionCache.CrystallineHelperTeleCrystalType;

        var updateSprite = chfdct?.GetMethod("UpdateSprite", BindingFlags.Instance | BindingFlags.NonPublic);
        if (updateSprite != null)
        {
            var target = GetType().GetMethod(nameof(CrystallineHelperForceDashCrystal_UpdateSprite), BindingFlags.Static | BindingFlags.NonPublic);
            hook_ForceDashCrystal_UpdateSprite = new Hook(updateSprite, target);
        }

        var render = chfdct?.GetMethod("Render", BindingFlags.Instance | BindingFlags.Public);
        if (render != null)
        {
            var target = GetType().GetMethod(nameof(CrystallineHelperForceDashCrystal_Render), BindingFlags.Static | BindingFlags.NonPublic);
            hook_ForceDashCrystal_Render = new Hook(render, target);
        }

        var telerender = chtct?.GetMethod("Render", BindingFlags.Instance | BindingFlags.Public);
        if (telerender != null)
        {
            var target = GetType().GetMethod(nameof(CrystallineHelperTeleCrystal_Render), BindingFlags.Static | BindingFlags.NonPublic);
            hook_TeleCrystal_Render = new Hook(telerender, target);
        }
    }

    protected override void Unload()
    {
        hook_ForceDashCrystal_UpdateSprite?.Dispose();
        hook_ForceDashCrystal_UpdateSprite = null;
        hook_ForceDashCrystal_Render?.Dispose();
        hook_ForceDashCrystal_Render = null;
        hook_TeleCrystal_Render?.Dispose();
        hook_TeleCrystal_Render = null;
    }

    private static void CrystallineHelperForceDashCrystal_UpdateSprite(Action<Entity> orig, Entity self)
    {
        orig(self);

        if (GravityHelperModule.ShouldInvertPlayer && self.Get<BloomPoint>() is { } bloomPoint)
        {
            bloomPoint.Y *= -1;
        }
    }

    private static void CrystallineHelperForceDashCrystal_Render(Action<Entity> orig, Entity self)
    {
        void flipSprites(Entity entity)
        {
            if (!GravityHelperModule.ShouldInvertPlayer) return;
            foreach (var comp in entity.Components)
            {
                if (comp is Sprite sprite)
                {
                    sprite.Y *= -1;
                    sprite.Scale.Y *= -1;
                }
            }
        }

        flipSprites(self);
        orig(self);
        flipSprites(self);
    }

    private static void CrystallineHelperTeleCrystal_Render(Action<Entity> orig, Entity self)
    {
        if (!GravityHelperModule.ShouldInvertPlayer)
        {
            orig(self);
            return;
        }

        var dyndata = DynamicData.For(self);
        var sprite = dyndata.Get<Sprite>("sprite");
        var flash = dyndata.Get<Sprite>("flash");
        var outline = dyndata.Get<Image>("outline");

        if (sprite != null) sprite.Scale.Y *= -1;
        if (flash != null) flash.Scale.Y *= -1;
        if (outline != null) outline.Scale.Y *= -1;

        orig(self);

        if (sprite != null) sprite.Scale.Y *= -1;
        if (flash != null) flash.Scale.Y *= -1;
        if (outline != null) outline.Scale.Y *= -1;
    }
}

