// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using Celeste.Mod.GravityHelper.Entities;
using Celeste.Mod.GravityHelper.Entities.Controllers;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

// ReSharper disable InconsistentNaming

namespace Celeste.Mod.GravityHelper.Hooks;

internal static class LevelHooks
{
    private static IDetour hook_Level_orig_TransitionRoutine;

    public static void Load()
    {
        Logger.Log(nameof(GravityHelperModule), $"Loading {nameof(Level)} hooks...");

        On.Celeste.Level.LoadLevel += Level_LoadLevel;
        On.Celeste.Level.EnforceBounds += Level_EnforceBounds;
        On.Celeste.Level.Update += Level_Update;
        On.Celeste.Level.End += Level_End;

        IL.Celeste.Level.EnforceBounds += Level_EnforceBounds;

        hook_Level_orig_TransitionRoutine = new ILHook(ReflectionCache.Level_OrigTransitionRoutine.GetStateMachineTarget(), Level_orig_TransitionRoutine);
    }

    public static void Unload()
    {
        Logger.Log(nameof(GravityHelperModule), $"Unloading {nameof(Level)} hooks...");

        On.Celeste.Level.LoadLevel -= Level_LoadLevel;
        On.Celeste.Level.EnforceBounds -= Level_EnforceBounds;
        On.Celeste.Level.Update -= Level_Update;
        On.Celeste.Level.End -= Level_End;

        IL.Celeste.Level.EnforceBounds -= Level_EnforceBounds;

        hook_Level_orig_TransitionRoutine?.Dispose();
        hook_Level_orig_TransitionRoutine = null;
    }

    private static void Level_orig_TransitionRoutine(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);

        //// if (direction == Vector2.UnitY)
        cursor.GotoNext(instr =>
            instr.MatchCall<Vector2>("get_UnitY") && instr.Next.MatchCall<Vector2>("op_Equality"));
        cursor.EmitInvertVectorDelegate();

        //// --playerTo.Y;
        cursor.GotoNext(MoveType.After, instr => instr.MatchLdcR4(1) && instr.Next.MatchSub());
        cursor.EmitInvertFloatDelegate();
        cursor.GotoPrev(instr => instr.MatchLdarg(0));

        //// while ((double) direction.X != 0.0 && (double) playerTo.Y >= (double) level.Bounds.Bottom)
        // to avoid changing the >= comparison, this converts to -playerTo.Y >= -level.Bounds.Top
        cursor.GotoNext(MoveType.After, instr => instr.MatchLdfld<Vector2>(nameof(Vector2.Y)));
        cursor.EmitInvertFloatDelegate();
        cursor.GotoNext(instr => instr.MatchCall<Rectangle>("get_Bottom"));
        cursor.EmitLoadShouldInvert();
        cursor.Emit(OpCodes.Brfalse_S, cursor.Next);
        cursor.Emit(OpCodes.Call, typeof(Rectangle).GetMethod("get_Top"));
        cursor.Emit(OpCodes.Neg);
        cursor.Emit(OpCodes.Br_S, cursor.Next.Next);
    });

    private static void Level_EnforceBounds(On.Celeste.Level.orig_EnforceBounds orig, Level self, Player player)
    {
        if (!GravityHelperModule.ShouldInvertPlayer)
        {
            orig(self, player);
            return;
        }

        // horizontal code copied from vanilla
        Rectangle bounds = self.Bounds;
        Rectangle rectangle = new Rectangle((int) self.Camera.Left, (int) self.Camera.Top, 320, 180);
        if (self.Transitioning)
            return;
        if (self.CameraLockMode == Level.CameraLockModes.FinalBoss && player.Left < (double) rectangle.Left)
        {
            player.Left = rectangle.Left;
            player.OnBoundsH();
        }
        else if (player.Left < (double) bounds.Left)
        {
            if (player.Top >= (double) bounds.Top && player.Bottom < (double) bounds.Bottom &&
                self.Session.MapData.CanTransitionTo(self, player.Center + Vector2.UnitX * -8f))
            {
                player.BeforeSideTransition();
                self.NextLevel(player.Center + Vector2.UnitX * -8f, -Vector2.UnitX);
                return;
            }

            player.Left = bounds.Left;
            player.OnBoundsH();
        }

        TheoCrystal entity = self.Tracker.GetEntity<TheoCrystal>();
        if (self.CameraLockMode == Level.CameraLockModes.FinalBoss && player.Right > (double) rectangle.Right && rectangle.Right < bounds.Right - 4)
        {
            player.Right = rectangle.Right;
            player.OnBoundsH();
        }
        else if (entity != null && (player.Holding == null || !player.Holding.IsHeld) && player.Right > (double) (bounds.Right - 1))
            player.Right = bounds.Right - 1;
        else if (player.Right > (double) bounds.Right)
        {
            if (player.Top >= (double) bounds.Top && player.Bottom < (double) bounds.Bottom &&
                self.Session.MapData.CanTransitionTo(self, player.Center + Vector2.UnitX * 8f))
            {
                player.BeforeSideTransition();
                self.NextLevel(player.Center + Vector2.UnitX * 8f, Vector2.UnitX);
                return;
            }

            player.Right = bounds.Right;
            player.OnBoundsH();
        }

        // custom vertical code
        void tryToDie(int bounceAtPoint)
        {
            if (SaveData.Instance.Assists.Invincible)
            {
                player.Play("event:/game/general/assist_screenbottom");
                player.Bounce(bounceAtPoint);
            }
            else
                player.Die(Vector2.Zero);
        }

        // transition down if required
        if (self.CameraLockMode != Level.CameraLockModes.None && player.Bottom > rectangle.Bottom)
        {
            player.Bottom = rectangle.Bottom;
            player.OnBoundsV();
        }
        else if (player.CenterY > bounds.Bottom)
        {
            if (self.Session.MapData.CanTransitionTo(self, player.Center + Vector2.UnitY * 12f) &&
                !self.Session.LevelData.DisableDownTransition &&
                !player.CollideCheck<Solid>(player.Position + Vector2.UnitY * 4f))
            {
                player.BeforeDownTransition();
                self.NextLevel(player.Center + Vector2.UnitY * 12f, Vector2.UnitY);
            }
            else if (player.Bottom > bounds.Bottom + 24)
            {
                player.Bottom = bounds.Bottom + 24;
                player.OnBoundsV();
            }
        }

        // die or transition up if required
        var disableUpTransition = self.Tracker.GetEntity<DisableUpTransitionController>() != null;
        if (self.CameraLockMode != Level.CameraLockModes.None && rectangle.Top > bounds.Top + 4 && player.Bottom < rectangle.Top)
            tryToDie(rectangle.Top);
        else if (player.Top < bounds.Top &&
                 self.Session.MapData.CanTransitionTo(self, player.Center - Vector2.UnitY * 12f) &&
                 !disableUpTransition)
        {
            player.BeforeUpTransition();
            self.NextLevel(player.Center - Vector2.UnitY * 12f, -Vector2.UnitY);
        }
        else if (player.Bottom < bounds.Top && SaveData.Instance.Assists.Invincible)
            tryToDie(bounds.Top);
        else if (player.Bottom < bounds.Top - 4)
            player.Die(Vector2.Zero);
    }

    private static void Level_EnforceBounds(ILContext il) => HookUtils.SafeHook(() =>
    {
        var cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.Before, instr => instr.MatchCallvirt<Player>(nameof(Player.BeforeUpTransition))))
            throw new HookException("Couldn't find Player.BeforeUpTransition.");
        if (!cursor.TryGotoPrev(MoveType.After, instr => instr.MatchCallvirt<MapData>(nameof(MapData.CanTransitionTo))))
            throw new HookException("Couldn't find MapData.CanTransitionTo.");

        cursor.Emit(OpCodes.Ldarg_0);
        cursor.EmitDelegate<Func<bool, Level, bool>>((b, self) =>
        {
            if (!b) return false;
            var disableUpTransition = self.Tracker.GetEntity<DisableUpTransitionController>() != null;
            return !disableUpTransition;
        });
    });

    private static void Level_Update(On.Celeste.Level.orig_Update orig, Level self)
    {
        if (GravityHelperModule.Settings.ToggleInvertGravity.Pressed && !self.FrozenOrPaused && self.Tracker.GetEntity<Player>() != null)
        {
            GravityHelperModule.Settings.ToggleInvertGravity.ConsumePress();
            GravityHelperModule.PlayerComponent?.SetGravity(GravityType.Toggle);
        }

        orig(self);
    }

    /// <summary>
    /// This is hooked to ensure that LoadLevel will create and add all controllers on the entire map,
    /// and only once when the map is first loaded, regardless of the current room.
    /// </summary>
    private static void Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerintro, bool isfromloader)
    {
        if (!isfromloader)
        {
            orig(self, playerintro, false);
            replaceRefills(self);
            return;
        }

        GravityHelperModule.ClearStatics();

        // find all gravity controllers
        var entities = self.Session.MapData?.Levels?.SelectMany(l => l.Entities) ?? Enumerable.Empty<EntityData>();
        var controllers = entities.Where(d => d.Name.StartsWith("GravityHelper") && d.Name.Contains("Controller")).ToArray();

        // log controller warnings if required
        foreach (var grouping in controllers.GroupBy(d => d.Name))
        {
            var persistentCount = grouping.Count(c => c.Bool("persistent"));
            if (persistentCount > 1)
                Logger.Log(LogLevel.Warn, nameof(GravityHelperModule), $"Warning: Found {persistentCount} persistent controllers of type {grouping.Key}");

            foreach (var roomGroup in grouping.GroupBy(d => d.Level.Name))
            {
                var controllerCount = roomGroup.Count();
                if (controllerCount > 1)
                    Logger.Log(LogLevel.Warn, nameof(GravityHelperModule), $"Warning: Found {controllerCount} controllers of type {grouping.Key} in room {roomGroup.Key}");
            }
        }

        foreach (var data in controllers)
        {
            // if the "persistent" flag does not exist (regardless of whether it's set), don't skip loading
            if (!data.Has("persistent")) continue;
            self.Session.DoNotLoad.Add(new EntityID(data.Level.Name, data.ID));
            Level.LoadCustomEntity(data, self);
        }

        orig(self, playerintro, true);

        replaceRefills(self);

        var triggers = self.Session.MapData?.Levels?.SelectMany(l => l.Triggers) ?? Enumerable.Empty<EntityData>();
        var hasVvvvvvTriggers = triggers.Any(e => e.Name == "GravityHelper/VvvvvvTrigger");
        var hasCassetteControllers = controllers.Any(e => e.Name == "GravityHelper/CassetteGravityController");

        // apply each controller type (this should probably be automatic)
        self.GetPersistentController<BehaviorGravityController>()?.Transitioned();
        self.GetPersistentController<SoundGravityController>()?.Transitioned();
        self.GetPersistentController<VisualGravityController>()?.Transitioned();
        self.GetPersistentController<CassetteGravityController>(hasCassetteControllers)?.Transitioned();

        // vvvvvv requires extra logic when triggers exist
        var vvvvvv = self.GetPersistentController<VvvvvvGravityController>(hasVvvvvvTriggers || GravityHelperModule.Settings.VvvvvvMode != GravityHelperModuleSettings.VvvvvvSetting.Default);
        if (vvvvvv != null && vvvvvv.Ephemeral && hasVvvvvvTriggers)
            vvvvvv.Mode = VvvvvvMode.TriggerBased;
        vvvvvv?.Transitioned();
    }

    private static void replaceRefills(Level self)
    {
        // replace refills
        if (GravityHelperModule.Settings.ReplaceRefills)
        {
            var refills = self.Entities.Where(e => e.GetType() == typeof(Refill)).ToList();
            foreach (Refill refill in refills)
            {
                var gravRefill = new GravityRefill(refill.Position, refill.twoDashes, refill.oneUse);
                self.Remove(refill);
                self.Add(gravRefill);
            }
        }
    }

    private static void Level_End(On.Celeste.Level.orig_End orig, Level self)
    {
        orig(self);
        GravityHelperModule.ClearStatics();
    }
}
