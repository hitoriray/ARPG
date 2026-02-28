using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// 敌人生成区域。
    /// 场景中放置此组件，用 SphereTrigger 或半径检测感知玩家进入/离开，
    /// 统一管理下属所有 SpawnPoint 的激活/暂停/清空状态。
    /// </summary>
    public class SpawnRegion : MonoBehaviour
    {
        [Header("生成配置")]
        [Required]
        [Tooltip("此区域使用的生成配置")]
        [SerializeField] private EnemySpawnConfig spawnConfig;

        [Header("运行时信息（只读）")]
        [ShowInInspector, ReadOnly] private bool _isActive;
        [ShowInInspector, ReadOnly] private bool _isCleared;
        [ShowInInspector, ReadOnly] private int _aliveCount;

        private List<SpawnPoint> _spawnPoints = new();
        private HashSet<SpawnPoint> _subscribedPoints = new();
        private Transform _player;

        // ── 标识 ─────────────────────────────────────────────────
        /// <summary>此区域的唯一标识（用于存档，取 GameObject 路径 hash）</summary>
        public int RegionId => GetInstanceID();

        public bool IsCleared => _isCleared;

        /// <summary>供 EnemySpawnManager 读取激活半径</summary>
        public float ActivateRadius => spawnConfig != null ? spawnConfig.ActivateRadius : 0f;

        // ── 生命周期 ──────────────────────────────────────────────
        private void Awake()
        {
            CollectSpawnPoints();
        }

        private void Start()
        {
            // 向全局管理器注册自己
            EnemySpawnManager.Register(this);

            // 检查存档：开局即判断该区域是否已被玩家清空
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string key = $"{sceneName}_{RegionId}";
            if (EnemySpawnManager.IsRegionKeyCleared(key))
            {
                _isCleared = true; // 直接设为清空，不再初始化生成
                RayDebug.Log($"[SpawnRegion] {gameObject.name} 已在存档中被清空，跳过初始化");
            }
        }

        private void OnDestroy()
        {
            EnemySpawnManager.Unregister(this);
        }

        private void Update()
        {
            if (!_isActive || _isCleared || _player == null || spawnConfig == null)
                return;

            // 检查玩家是否离开了停用半径
            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist > spawnConfig.DeactivateRadius)
            {
                Deactivate();
            }
        }

        // ── 区域激活 / 停用 ───────────────────────────────────────
        /// <summary>玩家进入激活范围，由 EnemySpawnManager 调用</summary>
        public void Activate(Transform player)
        {
            if (_isCleared || _isActive) return;

            _player = player;
            _isActive = true;

            RefreshSpawnPoints();

            foreach (var sp in _spawnPoints)
                sp.Activate();

            RayDebug.Log($"{gameObject.name} 激活，生成点数量: {_spawnPoints.Count}");
        }

        /// <summary>玩家离开停用范围，暂停生成（不销毁已存活的敌人）</summary>
        public void Deactivate()
        {
            _isActive = false;
            foreach (var sp in _spawnPoints)
                sp.Pause();

            RayDebug.Log($"{gameObject.name} 停用");
        }

        /// <summary>外力强制清空（剧情触发、调试等）</summary>
        public void ForceCleared()
        {
            MarkCleared();
        }

        // ── 内部逻辑 ──────────────────────────────────────────────
        private void CollectSpawnPoints()
        {
            _spawnPoints.Clear();
            GetComponentsInChildren(true, _spawnPoints);
        }

        /// <summary>
        /// 确保每个 SpawnPoint 都被分配了配置。
        /// 按权重为每个点独立随机选取一条 SpawnGroupConfig。
        /// </summary>
        private void RefreshSpawnPoints()
        {
            if (spawnConfig == null || spawnConfig.SpawnGroups == null) return;

            foreach (var sp in _spawnPoints)
            {
                if (sp.State == SpawnPointState.Idle)
                {
                    var group = spawnConfig.GetRandomGroup();
                    sp.Init(group, this);

                    // 防止重复订阅
                    if (_subscribedPoints.Add(sp))
                        sp.OnEnemyDied += OnSpawnPointEnemyDied;
                }
            }
        }

        private void OnSpawnPointEnemyDied(SpawnPoint sp)
        {
            _aliveCount = Mathf.Max(0, _aliveCount - 1);
            CheckCleared();
        }

        private void CheckCleared()
        {
            if (!spawnConfig.NeverRespawnAfterCleared) return;

            // 判断是否所有敌人都死亡且没有正在等待刷新的点
            bool allGone = true;
            foreach (var sp in _spawnPoints)
            {
                if (sp.State == SpawnPointState.Alive ||
                    sp.State == SpawnPointState.Spawning ||
                    sp.State == SpawnPointState.Waiting)
                {
                    allGone = false;
                    break;
                }
            }

            if (allGone && _aliveCount <= 0)
            {
                MarkCleared();
            }
        }

        private void MarkCleared()
        {
            _isCleared = true;
            _isActive = false;

            foreach (var sp in _spawnPoints)
                sp.MarkCleared();

            // 通知全局管理器此区域已清空（可写存档）
            EnemySpawnManager.OnRegionCleared(this);
            RayDebug.Log($"{gameObject.name} 已永久清空！");
        }

        // ── Gizmo ─────────────────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (spawnConfig == null) return;

            // 激活半径（绿色）
            Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
            Gizmos.DrawSphere(transform.position, spawnConfig.ActivateRadius);
            Gizmos.color = new Color(0f, 1f, 0f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, spawnConfig.ActivateRadius);

            // 停用半径（黄色）
            Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
            Gizmos.DrawSphere(transform.position, spawnConfig.DeactivateRadius);
            Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, spawnConfig.DeactivateRadius);
        }
#endif
    }
}
