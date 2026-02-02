using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GOAP
{
    [CreateAssetMenu(fileName = "GOAPGlobalConfig", menuName = "Config/GOAP/GOAPGlobalConfig")]
    public class GOAPGlobalConfig : SerializedScriptableObject
    {
        public Dictionary<GOAPStateType, GOAPStateConfigItem> goapStateConfigDict = new();
        public static GOAPGlobalConfig Instance { get; private set; }

        private void OnEnable()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }
        
        public static GOAPStateBase CopyState(GOAPStateType stateType)
        {
            return Instance.goapStateConfigDict[stateType].state.Copy();
        }

        public static bool IsGlobalState(GOAPStateType stateType)
        {
            return Instance.goapStateConfigDict[stateType].isGlobal;
        }

        public static Type GetStateValueType(GOAPStateType stateType)
        {
            return Instance.goapStateConfigDict[stateType].GetType();
        }
        
        public class GOAPStateConfigItem
        {
            public GOAPStateBase state;
            public bool isGlobal;
        }
    }

}