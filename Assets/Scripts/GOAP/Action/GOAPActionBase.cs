using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace GOAP.Action
{
    public class GOAPTypeAndComparer
    {
        public GOAPStateType stateType;
        public GOAPStateComparer stateComparer;
    }
    
    public abstract class GOAPActionBase
    {
        [LabelText("前提")] public List<GOAPTypeAndComparer> preconditions = new();
        [LabelText("效果")] public List<GOAPTypeAndComparer> effects = new();
        [LabelText("代价值")] public float costValue;
        [LabelText("效果值")] public float effectValue;
        [LabelText("优先级"), ReadOnly] public virtual float Priority => effectValue - costValue;
        protected GOAPAgent agent;

        public virtual void Init(GOAPAgent agent, IGOAPOwner owner)
        {
            this.agent = agent;
        }

        public virtual bool CheckPreconditions()
        {
            foreach (var condition in preconditions)
            {
                if (!agent.CheckState(condition.stateType, condition.stateComparer))
                {
                    return false;
                }
            }
            return true;
        }

        public virtual void OnStart()
        {
        }

        public virtual void OnUpdate()
        {
        }

        public virtual void OnStop()
        {
        }
        
        /// <summary>
        /// 如果正常Stop，则不需要Destroy，如果在Update中销毁则调用OnDestroy
        /// </summary>
        public virtual void OnDestroy()
        {
        }

        public virtual void ApplyEffect()
        {
            for (int i = 0; i < effects.Count; i++)
            {
                GOAPTypeAndComparer effect = effects[i];
                if (GOAPGlobalConfig.IsGlobalState(effect.stateType))
                {
                    GOAPGlobalManager.Instance.GlobalStates.ApplyEffect(effect);
                }
                else
                {
                    agent.ApplyEffect(effect);
                }
            }
        }

        public virtual void UpdatePriority()
        {
        }
    }
}