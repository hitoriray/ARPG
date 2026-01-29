using JKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GOAP
{
    public class GOAPGlobalManager : SerializedMonoBehaviour
    {
        public static GOAPGlobalManager Instance { get; private set; }
        [SerializeField] private GOAPGlobalConfig config;
        public GOAPGlobalConfig Config => config;
        [SerializeField] private GOAPStates globalStates;
        public GOAPStates GlobalStates => globalStates;

        private void OnEnable()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }
    }
}