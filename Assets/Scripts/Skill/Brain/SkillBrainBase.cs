using System;
using System.Collections.Generic;
using Config;
using JKFrame;
using Player;
using Sirenix.OdinInspector;
using Skill.Behaviour;
using UnityEngine;

namespace Skill
{
    public abstract class SkillBrainBase : MonoBehaviour
    {
        [SerializeField] protected SkillPlayer skillPlayer;
        [SerializeField] protected List<SkillConfig> skillConfigs = new();  // 技能
        [ShowInInspector] protected List<SkillBehaviourBase> skillBehaviours;

        public virtual int LastReleaseBehaviourIndex { get; protected set; } = -1;
        public virtual bool CanRelease { get; protected set; }
        public int SkillConfigCount => skillConfigs.Count;

        public virtual void SetCanReleaseFlag(bool newValue)
        {
            CanRelease = newValue;
        }

        public virtual void Init(PlayerController player)
        {
            CanRelease = true;
            skillPlayer.Init(player.AnimationController, player.ModelTransform);
            skillBehaviours = new(skillConfigs.Count);
            foreach (var skillConfig in skillConfigs)
            {
                var skillBehaviour = skillConfig.Behaviour.DeepClone();
                skillBehaviour.Init(player, skillConfig, this, skillPlayer);
                skillBehaviours.Add(skillBehaviour);
            }
        }

        protected virtual void Update()
        {
            foreach (var skillBehaviour in skillBehaviours)
            {
                skillBehaviour.OnUpdate();
            }
        }

        public virtual void ReleaseSkill(int index)
        {
            if (LastReleaseBehaviourIndex != -1 && LastReleaseBehaviourIndex != index)
            {
                skillBehaviours[index].OnReleaseNewSkill();
            }
            skillBehaviours[index].Release();
            LastReleaseBehaviourIndex = index;
        }
        
        public virtual bool CheckCost(SkillCostType costType, float costValue)
        {
            // TODO：和上一层对接，（如PlayerController）
            return true;
        }
        
        public virtual bool CheckReleaseSkill(int index)
        {
            return CanRelease && skillBehaviours[index].CheckRelease();
        }
        
        /// <summary>
        /// 应用技能的消耗
        /// </summary>
        public virtual void ApplyCost(SkillCostType costType, float costValue)
        {
            Debug.Log($"释放技能的代价：类型:{costType}，需求量:{costValue}");
            // TODO：和上一层对接，（如PlayerController）
        }
        
        #region 共享数据

        protected interface ISkillShareData { }

        protected class SkillShareData<T> : ISkillShareData
        {
            public T value;
        }
        
        private readonly Dictionary<string, ISkillShareData> shareDataDict = new();

        protected SkillShareData<T> GetSkillShareData<T>()
        {
            return ResSystem.GetOrNew<SkillShareData<T>>();
        }

        protected void DestroySkillShareData(ISkillShareData value)
        {
            value.ObjectPushPool();
        }

        public void AddShareData<T>(string key, T value)
        {
            var skillShareData = GetSkillShareData<T>();
            skillShareData.value = value;
            shareDataDict.Add(key, skillShareData);
            if (sharedDataEventDict.TryGetValue(key, out var sharedDataEventData))
            {
                ((SharedDataEventData<T>)sharedDataEventData).TriggerOnCreate(value);
                ((SharedDataEventData<T>)sharedDataEventData).TriggerOnChanged(value);
            }
        }

        public void AddOrUpdateShareData<T>(string key, T value)
        {
            if (shareDataDict.TryGetValue(key, out var data))
            {
                ((SkillShareData<T>)data).value = value;
                if (sharedDataEventDict.TryGetValue(key, out var sharedDataEventData))
                {
                    ((SharedDataEventData<T>)sharedDataEventData).TriggerOnChanged(value);
                }
            }
            else
                AddShareData(key, value);
        }

        public bool ContainsShareData(string key)
        {
            return shareDataDict.ContainsKey(key);
        }

        public bool TryGetShareData<T>(string key, out T value)
        {
            bool res = shareDataDict.TryGetValue(key, out var data);
            value = res ? ((SkillShareData<T>)data).value : default;
            return res;
        }

        public void RemoveShareData(string key)
        {
            if (shareDataDict.Remove(key, out var data))
            {
                if (sharedDataEventDict.TryGetValue(key, out var sharedDataEventData))
                {
                    sharedDataEventData.TriggerOnRemove();
                }
                DestroySkillShareData(data);
            }
        }

        public void ClearShareData()
        {
            foreach (var (key, value) in shareDataDict)
            {
                DestroySkillShareData(value);
                if (sharedDataEventDict.TryGetValue(key, out var sharedDataEventData))
                {
                    sharedDataEventData.TriggerOnRemove();
                }
            }
            shareDataDict.Clear();
        }

        #endregion
        
        #region 共享数据相关事件

        private interface ISharedDataEventData
        {
            public void TriggerOnRemove();
        }

        private class SharedDataEventData<T> : ISharedDataEventData
        {
            public Action<T> onCreate;
            public Action<T> onChanged;
            public Action onRemove;

            public void TriggerOnCreate(T value) => onCreate?.Invoke(value);
            public void TriggerOnChanged(T value) => onChanged?.Invoke(value);
            public void TriggerOnRemove() => onRemove?.Invoke();
        }
        
        private Dictionary<string, ISharedDataEventData> sharedDataEventDict = new();

        public void AddSharedDataCreateEventListener<T>(string key, Action<T> action)
        {
            if (sharedDataEventDict.TryGetValue(key, out var sharedDataEventData) == false)
            {
                SharedDataEventData<T> eventData = new();
                eventData.onCreate += action;
                sharedDataEventDict.Add(key, eventData);
            }
            else
            {
                SharedDataEventData<T> eventData = (SharedDataEventData<T>)sharedDataEventData;
                eventData.onCreate += action;
            }
        }

        public void RemoveSharedDataCreateEventListener<T>(string key, Action<T> action)
        {
            if (sharedDataEventDict.TryGetValue(key, out var sharedDataEventData))
            {
                SharedDataEventData<T> eventData = (SharedDataEventData<T>)sharedDataEventData;
                eventData.onCreate -= action;
            }
        }
        
        public void AddSharedDataChangedEventListener<T>(string key, Action<T> action)
        {
            if (sharedDataEventDict.TryGetValue(key, out var sharedDataEventData) == false)
            {
                SharedDataEventData<T> eventData = new();
                eventData.onChanged += action;
                sharedDataEventDict.Add(key, eventData);
            }
            else
            {
                SharedDataEventData<T> eventData = (SharedDataEventData<T>)sharedDataEventData;
                eventData.onChanged += action;
            }
        }

        public void RemoveSharedDataChangedEventListener<T>(string key, Action<T> action)
        {
            if (sharedDataEventDict.TryGetValue(key, out var sharedDataEventData))
            {
                SharedDataEventData<T> eventData = (SharedDataEventData<T>)sharedDataEventData;
                eventData.onChanged -= action;
            }
        }
        
        public void AddSharedDataRemoveEventListener<T>(string key, Action action)
        {
            if (sharedDataEventDict.TryGetValue(key, out var sharedDataEventData) == false)
            {
                SharedDataEventData<T> eventData = new();
                eventData.onRemove += action;
                sharedDataEventDict.Add(key, eventData);
            }
            else
            {
                SharedDataEventData<T> eventData = (SharedDataEventData<T>)sharedDataEventData;
                eventData.onRemove += action;
            }
        }

        public void RemoveSharedDataRemoveEventListener<T>(string key, Action action)
        {
            if (sharedDataEventDict.TryGetValue(key, out var sharedDataEventData))
            {
                SharedDataEventData<T> eventData = (SharedDataEventData<T>)sharedDataEventData;
                eventData.onRemove -= action;
            }
        }

        #endregion
    }
}