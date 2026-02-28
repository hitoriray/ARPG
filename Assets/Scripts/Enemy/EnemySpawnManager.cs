using System.Collections.Generic;
using JKFrame;
using Manager;
using RayPlayer;
using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// 全局敌人生成管理器（场景单例）。
    /// 每帧检测玩家与各 SpawnRegion 的距离，在激活/停用半径之间切换区域状态。
    /// SpawnRegion 在 Start 时自动注册，无需手动配置。
    /// </summary>
    public class EnemySpawnManager : SingletonMono<EnemySpawnManager>
    {
        [Header("检测配置")]
        [Tooltip("玩家距离检测频率（秒），降低频率节省性能")]
        [SerializeField] private float detectInterval = 1f;

        // 全局静态注册表（供 SpawnRegion 调用）
        private static readonly List<SpawnRegion> s_regions = new();

        // 已清空的区域 ID 集合（用于写存档）
        private static readonly HashSet<int> s_clearedRegionIds = new();

        private float _detectTimer;
        private Transform _playerTransform;

        // ── 静态注册接口（供 SpawnRegion 调用）───────────────────
        public static void Register(SpawnRegion region)
        {
            if (!s_regions.Contains(region))
                s_regions.Add(region);
        }

        public static void Unregister(SpawnRegion region)
        {
            s_regions.Remove(region);
        }

        public static void OnRegionCleared(SpawnRegion region)
        {
            s_clearedRegionIds.Add(region.RegionId);

            // 持久化已清空区域到存档
            if (Manager.DataManager.GameData != null)
            {
                var keys = Manager.DataManager.GameData.ClearedRegionKeys;
                if (keys == null)
                {
                    Manager.DataManager.GameData.ClearedRegionKeys = new Data.Serialized_List<string>();
                    keys = Manager.DataManager.GameData.ClearedRegionKeys;
                }
                string key = GetRegionKey(region);
                if (!keys.List.Contains(key))
                {
                    keys.List.Add(key);
                    Manager.DataManager.SaveGameData();
                }
            }
        }

        /// <summary>生成区域的存档 Key（场景名 + 实例 ID）。</summary>
        private static string GetRegionKey(SpawnRegion region)
            => $"{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}_{region.RegionId}";

        // ── MonoBehaviour ─────────────────────────────────────────
        private void Start()
        {
            // 获取玩家 Transform
            RefreshPlayerRef();

            // 从存档恢复已清空区域 ID（对应 SpawnRegion.IsCleared 标记，
            // 由 SpawnRegion.Start 在注册后立即读取）
            RestoreClearedRegionsFromSave();
        }

        private void Update()
        {
            _detectTimer += Time.deltaTime;
            if (_detectTimer < detectInterval) return;
            _detectTimer = 0f;

            if (_playerTransform == null)
            {
                RefreshPlayerRef();
                if (_playerTransform == null) return;
            }

            TickRegions();
        }

        // ── 核心检测逻辑 ──────────────────────────────────────────
        private void TickRegions()
        {
            Vector3 playerPos = _playerTransform.position;

            for (int i = s_regions.Count - 1; i >= 0; i--)
            {
                var region = s_regions[i];
                if (region == null)
                {
                    s_regions.RemoveAt(i);
                    continue;
                }

                if (region.IsCleared) continue;

                float dist = Vector3.Distance(region.transform.position, playerPos);

                // 取 Config 中的激活半径（SpawnRegion 公开一个属性）
                float activateR = region.ActivateRadius;

                if (dist <= activateR)
                {
                    region.Activate(_playerTransform);
                }
                // 停用由 SpawnRegion.Update() 内部自行检测（避免每帧两次距离计算）
            }
        }

        // ── 工具 ──────────────────────────────────────────────────

        /// <summary>
        /// 从存档读取已清空区域列表，通知对应 SpawnRegion 标记为永久清空状态。
        /// Start 时调用一次即可。
        /// </summary>
        private void RestoreClearedRegionsFromSave()
        {
            var keys = Manager.DataManager.GameData?.ClearedRegionKeys;
            if (keys == null || keys.List == null || keys.List.Count == 0) return;

            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            foreach (var region in s_regions)
            {
                if (region == null) continue;
                string key = $"{sceneName}_{region.RegionId}";
                if (keys.List.Contains(key))
                {
                    region.ForceCleared();
                    s_clearedRegionIds.Add(region.RegionId);
                }
            }
        }

        /// <summary>
        /// 查询某个区域 key 是否已在存档中被标记为清空。
        /// 供 SpawnRegion.Start 调用以实现"开局即清空"。
        /// </summary>
        public static bool IsRegionKeyCleared(string regionKey)
        {
            var keys = Manager.DataManager.GameData?.ClearedRegionKeys;
            return keys != null && keys.List != null && keys.List.Contains(regionKey);
        }

        private void RefreshPlayerRef()
        {
            if (PlayerManager.Instance != null && PlayerManager.Instance.player != null)
                _playerTransform = PlayerManager.Instance.player.transform;
        }

        /// <summary>全局暂停所有区域生成（过场动画时调用）</summary>
        public void PauseAll()
        {
            foreach (var r in s_regions)
                r?.Deactivate();
        }

        /// <summary>全局恢复所有区域感知（过场结束后调用）</summary>
        public void ResumeAll()
        {
            if (_playerTransform == null) RefreshPlayerRef();
            TickRegions();
        }

        private void OnDestroy()
        {
            s_regions.Clear();
            s_clearedRegionIds.Clear();
        }
    }
}
