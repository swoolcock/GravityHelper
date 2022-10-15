// Copyright (c) Shane Woolcock. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using Monocle;

namespace Celeste.Mod.GravityHelper.Components
{
    [Tracked]
    public class CassetteComponent : Component
    {
        public bool Enabled { get; set; } = true;
        public int CassetteIndex { get; }

        public Action<CassetteStates> OnStateChange;

        private CassetteStates _cassetteState = CassetteStates.Off;
        public CassetteStates CassetteState
        {
            get => _cassetteState;
            set
            {
                _cassetteState = value;
                OnStateChange?.Invoke(value);
            }
        }

        private EntityID? _entityId;

        public CassetteComponent(int cassetteIndex, EntityID? id = null) : base(true, false)
        {
            CassetteIndex = cassetteIndex;
            _entityId = id;
        }

        public CassetteComponent(int cassetteIndex, EntityData data)
            : this(cassetteIndex, new EntityID(data.Level.Name, data.ID))
        {
        }

        public void TrySetActivatedSilently(int currentIndex)
        {
            if (_entityId != null && _entityId.Value.Level == SceneAs<Level>().Session.Level)
                CassetteState = CassetteIndex == currentIndex ? CassetteStates.On : CassetteStates.Off;
        }

        public override void EntityAdded(Scene scene)
        {
            base.EntityAdded(scene);
            if (scene is not Level level) return;

            level.HasCassetteBlocks = true;

            var cbm = scene.Entities.Concat(scene.Entities.ToAdd).OfType<CassetteBlockManager>().FirstOrDefault();
            if (cbm == null)
            {
                scene.Add(cbm = new CassetteBlockManager());
            }
        }
    }

    public enum CassetteStates
    {
        Off,
        Appearing,
        On,
        Disappearing,
    }
}
