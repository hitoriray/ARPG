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

        public virtual void Init(PlayerController player)
        {
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
            skillBehaviours[index].Release();
        }
        
        public virtual bool CheckCost(SkillCostType costType, float costValue)
        {
            // TODO：和上一层对接，（如PlayerController）
            return true;
        }
        
        public virtual bool CheckReleaseSkill(int index)
        {
            return skillBehaviours[index].CheckRelease();
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
        
        private Dictionary<string, ISkillShareData> shareDataDict = new();

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
        }

        public void AddOrUpdateShareData<T>(string key, T value)
        {
            if (shareDataDict.TryGetValue(key, out var skillShareData))
            {
                ((SkillShareData<T>)skillShareData).value = value;
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
            bool res = shareDataDict.TryGetValue(key, out ISkillShareData skillShareData);
            value = res ? ((SkillShareData<T>)skillShareData).value : default;
            return res;
        }

        public void RemoveShareData(string key)
        {
            if (shareDataDict.TryGetValue(key, out ISkillShareData skillShareData))
            {
                DestroySkillShareData(skillShareData);
            }
        }

        public void ClearShareData()
        {
            foreach (var item in shareDataDict)
            {
                DestroySkillShareData(item.Value);
            }
            shareDataDict.Clear();
        }

        #endregion
    }
}