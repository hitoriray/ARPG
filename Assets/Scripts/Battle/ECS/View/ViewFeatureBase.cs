using System.Collections.Generic;
using Arch.Extend.System;
using Battle.ECS.Core;
using Battle.ECS.View;

namespace Battle.ECS.Features
{
    public abstract class ViewFeatureBase : BattleFeature, ILoadViewSystem, IUnloadViewSystem
    {
        private readonly List<ILoadViewSystem> _loadViewSystems = new();
        private readonly List<IUnloadViewSystem> _unloadViewSystems = new();

        private bool _loaded;
        protected ViewFeatureBase(BattleContext context) : base(context)
        {
        }

        public sealed override Systems Add(ISystem system)
        {
            if (system is ILoadViewSystem loadViewSystem) _loadViewSystems.Add(loadViewSystem);
            if (system is IUnloadViewSystem unloadViewSystem) _unloadViewSystems.Add(unloadViewSystem);
            return base.Add(system);
        }

        public override void Update()
        {
            if (_loaded)
                base.Update();
        }

        public override void Cleanup()
        {
            if (_loaded)
                base.Cleanup();
        }

        public void LoadView(BattleViewReference viewReference)
        {
            _loaded = true;
            for (int i = 0; i < _loadViewSystems.Count; i++)
            {
                _loadViewSystems[i].LoadView(viewReference);
            }
        }

        public void UnloadView()
        {
            for (int i = 0; i < _unloadViewSystems.Count; i++)
            {
                _unloadViewSystems[i].UnloadView();
            }
            _loaded = false;
        }
    }
}