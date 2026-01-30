using System.Collections.Generic;
using GOAP.Action;
using Sirenix.OdinInspector;

namespace GOAP
{
    public class GOAPStates
    {
        public Dictionary<string, GOAPStateBase> StateDict = new();

        public bool TryAddState(GOAPStateType type, GOAPStateBase state)
        {
            return StateDict.TryAdd(type, state);
        }

        public bool TryRemove(GOAPStateType type)
        {
            return StateDict.Remove(type);
        }

        public T GetState<T>(GOAPStateType type) where T : GOAPStateBase
        {
            return (T)StateDict[type];
        }

        public bool TryGetState(GOAPStateType type, out GOAPStateBase state)
        {
            state = default;
            if (StateDict == null || type.name == null)
                return false;
            return StateDict.TryGetValue(type, out state);
        }

        public bool TryGetState<T>(GOAPStateType type, out T state) where T : GOAPStateBase
        {
            state = default;
            if (StateDict == null)
                return false;
            if (StateDict.TryGetValue(type, out GOAPStateBase stateBase))
            {
                state = (T)stateBase;
                return true;
            }
            return false;
        }

        public bool CheckStateForPrecondition(GOAPStateType type, GOAPStateComparer comparer)
        {
            if (TryGetState(type, out GOAPStateBase state))
            {
                return state.CompareForPrecondition(comparer);
            }
            return false;
        }
        public bool CheckStateForEffect(GOAPStateType type, GOAPStateComparer comparer)
        {
            if (TryGetState(type, out GOAPStateBase state))
            {
                return state.CompareForEffect(comparer);
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
    }
}