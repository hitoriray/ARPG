using System.Collections.Generic;

namespace Arch.Extend.System
{
#if UNITY_EDITOR
    public class DebugSystems : ProfilingSystems, IDrawGizmosSystem, IOnGUISystem
    {
        private readonly List<IDrawGizmosSystem> _drawGizmosSystems = new();
        private readonly List<IOnGUISystem> _onGUISystems = new();

        public override Systems Add(ISystem system)
        {
            if (system is IDrawGizmosSystem drawGizmosSystem) _drawGizmosSystems.Add(drawGizmosSystem);
            if (system is IOnGUISystem onGUISystem) _onGUISystems.Add(onGUISystem);
            return base.Add(system);
        }

        public void OnDrawGizmos()
        {
            foreach (var drawGizmosSystem in _drawGizmosSystems) drawGizmosSystem.OnDrawGizmos();
        }

        public void OnGUI()
        {
            foreach (var onGUISystem in _onGUISystems) onGUISystem.OnGUI();
        }
    }
#endif
}
