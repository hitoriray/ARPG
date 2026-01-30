using System.Collections.Generic;
using GOAP.Editor;
using GOAP.Plan;
using Sirenix.OdinInspector;

namespace GOAP.Action
{
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
                if (!agent.CheckStateForPrecondition(condition.stateType, condition.stateComparer))
                {
                    return false;
                }
            }
            return true;
        }

        public virtual bool CheckEffect()
        {
            foreach (var effect in effects)
            {
                if (!agent.CheckStateForEffect(effect.stateType, effect.stateComparer))
                {
                    return false;
                }
            }
            return true;
        }

        public virtual GOAPRunState StartRun()
        {
            if (CheckEffect())
                return GOAPRunState.Succeed;
            if (CheckPreconditions())
            {
                OnStart();
                return GOAPRunState.Running;
            }
            return GOAPRunState.Failed;
        }

        public virtual void OnStart() { }
        public virtual GOAPRunState OnUpdate() { return default; }
        public virtual void OnStop() { }
        
        /// <summary>
        /// 如果正常Stop，则不需要Destroy，如果在Update中销毁则调用OnDestroy
        /// </summary>
        public virtual void OnDestroy() { }

        public void ApplyEffect()
        {
            foreach (var effect in effects)
            {
                if (GOAPGlobalManager.Instance.TryGetGlobalState(effect.stateType, out GOAPStateBase state))
                {
                    state.ApplyEffect(effect.stateComparer);
                }
                else
                {
                    agent.ApplyEffect(effect);
                }
            }
        }

        public virtual void UpdatePriority() { }
    }
    
    public class GOAPTypeAndComparer
    {
        [OnValueChanged("CheckState")] public GOAPStateType stateType;
        public GOAPStateComparer stateComparer;
#if UNITY_EDITOR
        public void CheckState()
        {
            if (GOAPEditorUtility.GlobalManager != null
                && GOAPEditorUtility.GlobalManager.TryGetGlobalState(stateType, out GOAPStateBase state)
                && (stateComparer == null || stateComparer.GetType() != state.GetComparerType()))
            {
                stateComparer = state.GetComparer();
            }
            else if (GOAPEditorUtility.agent != null 
                     && GOAPEditorUtility.agent.states.TryGetState(stateType, out state)
                     && (stateComparer == null || stateComparer.GetType() != state.GetComparerType()))
            {
                stateComparer = state.GetComparer();
            }
        }
#endif
    }
}