using System.Collections.Generic;
using Config;
using JKFrame;
using UnityEngine;

namespace Skill
{
    public abstract class SkillBrainBase : MonoBehaviour
    {
        public SkillConfig basicAttackConfig;           // 普攻
        public List<SkillConfig> skillConfigs = new();  // 技能

        /// <summary>
        /// 应用技能的消耗
        /// </summary>
        /// <param name="costType">消耗类型</param>
        /// <param name="value">消耗的值</param>
        public virtual void ApplyCost(SkillCostType costType, float value)
        {
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