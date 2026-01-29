using System.Collections.Generic;

namespace GOAP.Action
{
    public class GOAPActions
    {
        public List<GOAPActionBase> actions = new();
        // Value: 可以满足GOAPStateType的行为列表
        public Dictionary<GOAPStateType, List<GOAPActionBase>> ActionEffectDict { get; set; }

        public void Init(GOAPAgent agent, IGOAPOwner owner)
        {
            ActionEffectDict = new Dictionary<GOAPStateType, List<GOAPActionBase>>();
            foreach (GOAPActionBase action in actions)
            {
                action.Init(agent, owner);
                foreach (GOAPTypeAndComparer effect in action.effects)
                {
                    AddActionEffect(effect.stateType, action);
                }
            }
        }

        private void AddActionEffect(GOAPStateType stateType, GOAPActionBase action)
        {
            if (!ActionEffectDict.TryGetValue(stateType, out List<GOAPActionBase> actions))
            {
                actions = new List<GOAPActionBase>();
                ActionEffectDict.Add(stateType, actions);
            }
            actions.Add(action);
        }
    }
}