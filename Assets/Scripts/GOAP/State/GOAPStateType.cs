using System.Collections.Generic;
using GOAP.Editor;
using Sirenix.OdinInspector;

namespace GOAP
{
    [HideLabel]
    public struct GOAPStateType
    {
        [HideLabel, ValueDropdown("GetAllState")]
        public string name;

        public static implicit operator GOAPStateType(string stateName)
        {
            return new GOAPStateType { name = stateName };
        }

        public static implicit operator string(GOAPStateType stateType)
        {
            return stateType.name;
        }

        #region Editor

#if UNITY_EDITOR
        private List<string> GetAllState()
        {
            List<string> res = new();
            GOAPGlobalManager globalManager = GOAPEditorUtility.GlobalManager;
            if (globalManager != null && globalManager.GlobalStates != null && globalManager.GlobalStates.StateDict != null)
            {
                foreach (KeyValuePair<string, GOAPStateBase> item in globalManager.GlobalStates.StateDict)
                {
                    res.Add(item.Key);
                }
            }
            if (GOAPEditorUtility.agent != null && GOAPEditorUtility.agent.states != null && GOAPEditorUtility.agent.states.StateDict != null)
            {
                foreach (KeyValuePair<string, GOAPStateBase> item in GOAPEditorUtility.agent.states.StateDict)
                {
                    res.Add(item.Key);
                }
            }
            return res;
        }
#endif
        #endregion
    }
}