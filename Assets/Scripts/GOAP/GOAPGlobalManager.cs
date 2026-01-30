using JKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GOAP
{
    public class GOAPGlobalManager : SerializedMonoBehaviour
    {
        public static GOAPGlobalManager Instance { get; private set; }
        [SerializeField] private GOAPStates globalStates;
        public GOAPStates GlobalStates => globalStates;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        public bool TryGetGlobalState(string targetState, out GOAPStateBase state)
        {
            state = default;
            if (globalStates == null || globalStates.StateDict == null)
                return false;
            return globalStates.TryGetState(targetState, out state);
        }
    }
}