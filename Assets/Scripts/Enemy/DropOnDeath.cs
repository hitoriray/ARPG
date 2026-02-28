using Config;
using Manager;
using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// 敌人死亡掉落组件。
    /// 由 SpawnPoint 在生成敌人后动态 AddComponent，传入 SpawnGroupConfig 引用。
    /// 当 EnemyDeathListener 通知死亡时，调用 OnDied() 完成：
    ///   1. 按 LootConfig 掷骰子掉落道具（直接入背包，类艾尔登法环）
    ///   2. 给玩家加金币
    ///   3. 给玩家加经验
    /// </summary>
    public class DropOnDeath : MonoBehaviour
    {
        private SpawnGroupConfig _config;

        public void Init(SpawnGroupConfig config)
        {
            _config = config;
            // 订阅本 GameObject 上的 EnemyDeathListener
            var listener = GetComponent<EnemyDeathListener>();
            if (listener != null)
                listener.OnDied += OnDied;
        }

        private void OnDied()
        {
            if (_config == null) return;
            var loot = _config.LootConfig;

            // ── 经验 ──────────────────────────────────────────────
            if (_config.ExpReward > 0)
            {
                var playerConfig = PlayerManager.Instance?.CharacterConfig;
                if (playerConfig?.LevelGrowthConfig != null)
                {
                    DataManager.AddExperience(
                        DataManager.GameData.SelectedCharacterId,
                        _config.ExpReward,
                        playerConfig.LevelGrowthConfig);
                }
            }

            if (loot == null) return;

            // ── 金币（直接入账，不生成掉落物） ───────────────────
            int gold = loot.RollGold();
            if (gold > 0)
                DataManager.AddGold(gold);

            // ── 道具：根据配置决定直接入背包 or 生成世界掉落物 ────
            var drops = loot.Roll();
            Vector3 deathPos = transform.position + Vector3.up * 0.5f;
            foreach (var (item, count) in drops)
            {
                if (item.SpawnAsWorldDrop && LootDropManager.Instance != null)
                    LootDropManager.Instance.SpawnWorldDrop(item, count, deathPos);
                else
                    InventoryManager.AddItem(item, count);
            }
        }

        private void OnDestroy()
        {
            var listener = GetComponent<EnemyDeathListener>();
            if (listener != null)
                listener.OnDied -= OnDied;
        }
    }
}
