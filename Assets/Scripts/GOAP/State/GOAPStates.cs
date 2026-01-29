using System.Collections.Generic;
using GOAP.Action;
using Sirenix.OdinInspector;

namespace GOAP
{
    public class GOAPStates
    {
        public Dictionary<GOAPStateType, GOAPStateBase> StateDict = new();

        public bool TryAddState(GOAPStateType type, GOAPStateBase state)
        {
            return StateDict.TryAdd(type, state);
        }

        public bool TryRemove(GOAPStateType type)
        {
            return StateDict.Remove(type);
        }

        public T GetStateBase<T>(GOAPStateType type) where T : GOAPStateBase
        {
            return (T)StateDict[type];
        }

        public bool TryGetState<T>(GOAPStateType type, out T state) where T : GOAPStateBase
        {
            if (StateDict.TryGetValue(type, out GOAPStateBase stateBase))
            {
                state = (T)stateBase;
                return true;
            }
            state = default;
            return false;
        }

        public bool CheckState(GOAPStateType type, GOAPStateComparer comparer)
        {
            if (TryGetState(type, out GOAPStateBase state))
            {
                return state.Compare(comparer);
            }
            return false;
        }
        
        public void ApplyEffect(GOAPTypeAndComparer effect)
        {
            if (StateDict.TryGetValue(effect.stateType, out GOAPStateBase state))
            {
                state.ApplyEffect(effect.stateComparer);
            }
        }
        
#if UNITY_EDITOR
        [Button]
        private void CheckStates()
        {
            List<GOAPStateType> createTypeList = new();
            foreach (var item in StateDict)
            {
                if (item.Value == null ||
                    GOAPGlobalConfig.GetStateValueType(item.Key) != item.Value.GetType())
                {
                    createTypeList.Add(item.Key);
                }
            }

            foreach (var state in createTypeList)
            {
                StateDict[state] = GOAPGlobalConfig.CopyState(state);
            }
        }
#endif
    }
}