using System.Collections.Generic;
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
            GOAPGlobalManager globalManager = GOAP.Editor.GOAPEditorUtility.GlobalManager;
            if (globalManager != null && globalManager.GlobalStates != null && globalManager.GlobalStates.StateDict != null)
            {
                foreach (KeyValuePair<string, GOAPStateBase> item in globalManager.GlobalStates.StateDict)
                {
                    res.Add(item.Key);
                }
            }
            if (GOAP.Editor.GOAPEditorUtility.agent != null && GOAP.Editor.GOAPEditorUtility.agent.states != null && GOAP.Editor.GOAPEditorUtility.agent.states.StateDict != null)
            {
                foreach (KeyValuePair<string, GOAPStateBase> item in GOAP.Editor.GOAPEditorUtility.agent.states.StateDict)
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
