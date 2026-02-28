using System;
using Cysharp.Threading.Tasks;
using Manager;
using UnityEngine;

namespace Enemy
{
    public enum SpawnPointState
    {
        Idle,        // 未激活（区域未唤醒）
        Spawning,    // 正在异步加载/实例化
        Alive,       // 敌人存活中
        Waiting,     // 等待刷新（死亡倒计时）
        Cleared,     // 已永久清空（不再刷新）
    }

    /// <summary>
    /// 单个敌人生成点。
    /// 挂在空 GameObject 上，作为 SpawnRegion 的子节点。
    /// 负责：实例化敌人 → 监听死亡 → 倒计时刷新。
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        [Header("偏移配置")]
        [Tooltip("在此点周围半径内随机偏移生成位置（0 = 精确在此点生成）")]
        [SerializeField] private float spawnRadius = 1.5f;

        // ── 运行时状态 ────────────────────────────────────────────
        public SpawnPointState State { get; private set; } = SpawnPointState.Idle;

        /// <summary>当前此生成点管理的敌人实例（可能为 null）</summary>
        public GameObject SpawnedEnemy { get; private set; }

        private SpawnGroupConfig _config;
        private SpawnRegion _owner;
        private float _respawnTimer;
        private bool _isPaused;

        // ── 外部回调 ──────────────────────────────────────────────
        /// <summary>敌人死亡时通知 SpawnRegion</summary>
        public event Action<SpawnPoint> OnEnemyDied;

        // ── 初始化 ────────────────────────────────────────────────
        public void Init(SpawnGroupConfig config, SpawnRegion owner)
        {
            _config = config;
            _owner = owner;
            State = SpawnPointState.Idle;
            SpawnedEnemy = null;
        }

        // ── 激活 / 暂停 / 清空 ───────────────────────────────────
        public void Activate()
        {
            if (State == SpawnPointState.Cleared) return;
            _isPaused = false;

            if (State == SpawnPointState.Idle)
                SpawnAsync().Forget();
        }

        public void Pause()
        {
            _isPaused = true;
        }

        public void MarkCleared()
        {
            State = SpawnPointState.Cleared;
            if (SpawnedEnemy != null)
            {
                Destroy(SpawnedEnemy);
                SpawnedEnemy = null;
            }
        }

        // ── 每帧倒计时 ───────────────────────────────────────────
        private void Update()
        {
            if (State != SpawnPointState.Waiting || _isPaused) return;

            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer <= 0f)
            {
                State = SpawnPointState.Idle;
                SpawnAsync().Forget();
            }
        }

        // ── 核心生成逻辑 ─────────────────────────────────────────
        private async UniTaskVoid SpawnAsync()
        {
            if (_config == null || State == SpawnPointState.Cleared) return;
            if (State == SpawnPointState.Spawning || State == SpawnPointState.Alive) return;

            State = SpawnPointState.Spawning;

            // 通过 CharacterModelManager 异步加载预制体（复用现有基础设施）
            var modelManager = CharacterModelManager.Instance;
            if (modelManager == null)
            {
                RayDebug.Error("[SpawnPoint] CharacterModelManager 未初始化，无法生成敌人");
                State = SpawnPointState.Idle;
                return;
            }

            GameObject prefab = await modelManager.LoadCharacterModelPrefabAsync(_config.EnemyCharacterId);
            if (prefab == null)
            {
                RayDebug.Error($"[SpawnPoint] 加载敌人预制体失败，ID: {_config.EnemyCharacterId}");
                State = SpawnPointState.Idle;
                return;
            }

            // 生成位置：在设定半径内随机偏移（Y 保持与地面一致）
            Vector3 spawnPos = GetSpawnPosition();

            SpawnedEnemy = Instantiate(prefab, spawnPos, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360f), 0), this.transform);
            SpawnedEnemy.name = $"Enemy_{_config.EnemyCharacterId}_Lv{_config.Level}";

            // 初始化敌人属性（等级缩放）
            ApplyLevelToEnemy(SpawnedEnemy, _config.Level);

            // 挂载死亡监听桥接器
            var deathListener = SpawnedEnemy.GetComponentInChildren<EnemyDeathListener>();
            if (deathListener == null)
                deathListener = SpawnedEnemy.AddComponent<EnemyDeathListener>();

            deathListener.Init();
            deathListener.OnDied += HandleEnemyDied;

            // 挂载掉落组件（根据 SpawnGroupConfig 中的 LootConfig / ExpReward 自动掉落）
            var dropComp = SpawnedEnemy.AddComponent<DropOnDeath>();
            dropComp.Init(_config);

            State = SpawnPointState.Alive;
            RayDebug.Log($"[SpawnPoint] 生成敌人：{SpawnedEnemy.name} at {spawnPos}");
        }

        private void HandleEnemyDied()
        {
            if (SpawnedEnemy != null)
            {
                // 注意：不在这里直接 Destroy，让敌人自己播放死亡动画后销毁
                SpawnedEnemy = null;
            }

            OnEnemyDied?.Invoke(this);

            if (State == SpawnPointState.Cleared) return;

            // 启动刷新倒计时
            _respawnTimer = _config != null ? _config.RespawnDelay : 30f;
            State = SpawnPointState.Waiting;
            RayDebug.Log($"[SpawnPoint] 敌人死亡，{_respawnTimer}秒后刷新");
        }

        private Vector3 GetSpawnPosition()
        {
            Vector3 basePos = transform.position;
            if (spawnRadius <= 0f) return basePos;

            // 在 XZ 平面上随机偏移，Y 做一次向下的地面检测以贴地
            Vector2 rand = UnityEngine.Random.insideUnitCircle * spawnRadius;
            Vector3 offset = new Vector3(rand.x, 2f, rand.y); // +2 避免穿进地面
            Vector3 testPos = basePos + offset;

            if (Physics.Raycast(testPos, Vector3.down, out var hit, 5f))
                return hit.point;

            return basePos;
        }

        /// <summary>
        /// 根据等级对敌人属性进行缩放。
        /// 目前做简单的属性倍率处理，后续可扩展曲线。
        /// </summary>
        private void ApplyLevelToEnemy(GameObject enemy, int level)
        {
            // TODO: 接入 CharacterAttribute 系统做等级缩放
            // 示例：var attr = enemy.GetComponent<CharacterAttribute>();
            //       if (attr != null) attr.ApplyLevelScale(level);
            // 当前先空置，等 CharacterAttribute 支持等级接口后填写
        }

        // ── Gizmo ─────────────────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // 生成半径
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.3f);
            Gizmos.DrawSphere(transform.position, spawnRadius > 0 ? spawnRadius : 0.3f);

            // 圆心标记
            Gizmos.color = State == SpawnPointState.Alive ? Color.green
                : State == SpawnPointState.Waiting ? Color.yellow
                : State == SpawnPointState.Cleared ? Color.gray
                : Color.white;
            Gizmos.DrawWireSphere(transform.position, 0.35f);
        }

        private void OnDrawGizmos()
        {
            // 未选中时也显示小圆点
            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.15f);
            Gizmos.DrawSphere(transform.position, 0.25f);
        }
#endif
    }
}
