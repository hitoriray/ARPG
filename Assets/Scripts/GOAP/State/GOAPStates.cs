using System.Collections.Generic;

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
    }
}