// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Celeste.Mod.CelesteNet;
using Celeste.Mod.CelesteNet.Client;
using Celeste.Mod.CelesteNet.Client.Entities;
using Celeste.Mod.GravityHelper.Extensions;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.GravityHelper.ThirdParty.CelesteNet
{
    internal class CelesteNetGravityComponent : GameComponent
    {
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly CelesteNetClientModule _clientModule;
        private Delegate _initHook;
        private Delegate _disposeHook;

        private ConcurrentQueue<Action> _updateQueue = new ConcurrentQueue<Action>();

        public CelesteNetGravityComponent(Game game) : base(game)
        {
            _clientModule = (CelesteNetClientModule)Everest.Modules.FirstOrDefault(m => m is CelesteNetClientModule);
            if (_clientModule == null) throw new Exception("CelesteNet not loaded???");

            EventInfo initEvent = typeof(CelesteNetClientContext).GetEvent("OnInit");
            if (initEvent.EventHandlerType.GenericTypeArguments.FirstOrDefault() == typeof(CelesteNetClientContext))
                initEvent.AddEventHandler(null, _initHook = (Action<CelesteNetClientContext>)(_ => clientInit(_clientModule.Context.Client)));
            else
                initEvent.AddEventHandler(null, _initHook = (Action<object>)(_ => clientInit(_clientModule.Context.Client)));

            EventInfo disposeEvent = typeof(CelesteNetClientContext).GetEvent("OnDispose");
            if (disposeEvent.EventHandlerType.GenericTypeArguments.FirstOrDefault() == typeof(CelesteNetClientContext))
                disposeEvent.AddEventHandler(null, _disposeHook = (Action<CelesteNetClientContext>)(_ => clientDisposed()));
            else
                disposeEvent.AddEventHandler(null, _disposeHook = (Action<object>)(_ => clientDisposed()));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (_initHook != null)
                typeof(CelesteNetClientContext).GetEvent("OnInit").RemoveEventHandler(null, _initHook);
            _initHook = null;

            if (_disposeHook != null)
                typeof(CelesteNetClientContext).GetEvent("OnDispose").RemoveEventHandler(null, _disposeHook);
            _disposeHook = null;
        }

        private void clientInit(CelesteNetClient client)
        {
            client.Data.RegisterHandlersIn(this);
        }

        private void clientDisposed()
        {
        }

        public override void Update(GameTime gameTime)
        {
            var queue = _updateQueue;
            _updateQueue = new ConcurrentQueue<Action>();
            foreach (var action in queue) action();

            base.Update(gameTime);
        }

        public void Handle(CelesteNetConnection connection, DataPlayerGravity data) => _updateQueue.Enqueue(() =>
        {
            var ghost = Engine.Scene?.Tracker
                .GetEntities<Ghost>()
                .FirstOrDefault(e => (e as Ghost).PlayerInfo.ID == data.Player.ID);
            if (ghost == null) return;

            ghost.SetShouldInvert(data.GravityType == GravityType.Inverted);

            var hitbox = ghost.Collider;
            if (data.GravityType == GravityType.Inverted && hitbox.Top < 0 ||
                data.GravityType == GravityType.Normal && hitbox.Bottom > 0)
                hitbox.Bottom = -hitbox.Top;
        });

        public void SendPlayerGravity(GravityType gravityType)
        {
            var client = _clientModule.Context?.Client;
            if (client == null) return;

            client.SendAndHandle(new DataPlayerGravity
            {
                GravityType = gravityType,
                Player = client.PlayerInfo,
            });
        }
    }
}
