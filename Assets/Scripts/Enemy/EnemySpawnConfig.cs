using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// 单条生成规则：描述某一类敌人的生成参数。
    /// 一个 EnemySpawnConfig 可持有多条规则，SpawnPoint 按权重随机选取。
    /// </summary>
    [Serializable]
    public class SpawnGroupConfig
    {
        [LabelText("敌人ID")]
        [Tooltip("对应 CharacterTable 中的 CharacterId")]
        public int EnemyCharacterId;

        [LabelText("等级")]
        [MinValue(1)]
        public int Level = 1;

        [LabelText("最大存活数")]
        [MinValue(1)]
        public int MaxAliveCount = 1;

        [LabelText("刷新延迟(秒)")]
        [MinValue(0f)]
        public float RespawnDelay = 30f;

        [LabelText("权重")]
        [Range(1, 100)]
        public int Weight = 10;
    }

    /// <summary>
    /// 敌人生成配置资源（ScriptableObject）。
    /// 每个 SpawnRegion 挂一个此配置来描述该区域会生成什么敌人。
    /// 创建路径：右键 → Create → Config/Enemy/SpawnConfig
    /// </summary>
    [CreateAssetMenu(fileName = "EnemySpawnConfig", menuName = "Config/Enemy/SpawnConfig")]
    public class EnemySpawnConfig : ScriptableObject
    {
        [LabelText("生成规则列表")]
        [ListDrawerSettings(ShowFoldout = false, NumberOfItemsPerPage = 10)]
        public List<SpawnGroupConfig> SpawnGroups = new();

        [LabelText("区域同时存活上限")]
        [MinValue(1)]
        [Tooltip("整个区域内所有种类的敌人总存活数不超过此值（-1 = 不限）")]
        public int RegionMaxAliveCount = 10;

        [LabelText("区域激活半径")]
        [MinValue(1f)]
        [Tooltip("玩家进入此范围内，区域自动激活开始生成")]
        public float ActivateRadius = 40f;

        [LabelText("区域停用半径")]
        [MinValue(1f)]
        [Tooltip("玩家离开此范围，区域停止生成（应大于激活半径）")]
        public float DeactivateRadius = 60f;

        [LabelText("区域清空后不再刷新")]
        [Tooltip("若勾选，区域内所有敌人被玩家消灭后，该区域永久停止生成（可存档）")]
        public bool NeverRespawnAfterCleared = false;

        // ── 工具方法 ──────────────────────────────────────────────

        /// <summary>
        /// 按权重随机返回一条生成规则。
        /// </summary>
        public SpawnGroupConfig GetRandomGroup()
        {
            if (SpawnGroups == null || SpawnGroups.Count == 0)
                return null;

            if (SpawnGroups.Count == 1)
                return SpawnGroups[0];

            int totalWeight = 0;
            foreach (var g in SpawnGroups)
                totalWeight += Mathf.Max(1, g.Weight);

            int roll = UnityEngine.Random.Range(0, totalWeight);
            int accumulated = 0;
            foreach (var g in SpawnGroups)
            {
                accumulated += Mathf.Max(1, g.Weight);
                if (roll < accumulated)
                    return g;
            }

            return SpawnGroups[SpawnGroups.Count - 1];
        }
    }
}
