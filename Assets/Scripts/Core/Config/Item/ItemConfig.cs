using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Config
{
    public enum ItemType
    {
        Consumable,  // 消耗品（回血药、经验丹）
        Material,    // 材料（升级素材）
        Equipment,   // 装备
        KeyItem,     // 关键道具（任务物品）
        Gold,        // 金币（特殊类型，走 DataManager.AddGold）
    }

    /// <summary>
    /// 物品配置（ScriptableObject）。
    /// 所有道具的静态属性定义，不存运行时数量。
    /// 创建路径：Create → Config/Item/ItemConfig
    /// </summary>
    [CreateAssetMenu(fileName = "ItemConfig", menuName = "Config/Item/ItemConfig")]
    public class ItemConfig : ScriptableObject
    {
        [LabelText("物品ID（唯一）")]
        public int ItemId;

        [LabelText("物品名称")]
        public string ItemName;

        [LabelText("物品类型")]
        public ItemType ItemType;

        [LabelText("物品图标")]
        [PreviewField(60)]
        public AssetReferenceSprite Icon;

        [LabelText("最大叠加数量")]
        [MinValue(1)]
        public int MaxStackCount = 99;

        [LabelText("描述")]
        [MultiLineProperty(3)]
        public string Description;

        // ── 消耗品效果（ItemType = Consumable 时生效） ────────────
        [FoldoutGroup("消耗品效果"), LabelText("使用后恢复 HP")]
        public float HpRestore = 0f;
        [FoldoutGroup("消耗品效果"), LabelText("使用后恢复 MP")]
        public float MpRestore = 0f;
        [FoldoutGroup("消耗品效果"), LabelText("使用后获得经验")]
        public long ExpGain = 0L;

        // ── 世界掉落物配置 ─────────────────────────────────────────
        [TitleGroup("世界掉落物")]
        [LabelText("以世界物体形式掉落")]
        [Tooltip("false = 死亡时直接入背包；true = 在场景中生成可见掉落物")]
        public bool SpawnAsWorldDrop = false;

        [TitleGroup("世界掉落物")]
        [ShowIf("SpawnAsWorldDrop")]
        [LabelText("自动拾取")]
        [Tooltip("true = 玩家靠近自动吸取；false = 需要按交互键拾取")]
        public bool AutoPickup = true;

        [TitleGroup("世界掉落物")]
        [ShowIf("SpawnAsWorldDrop")]
        [LabelText("拾取半径")]
        [MinValue(0.1f)]
        public float PickupRadius = 2f;

        [TitleGroup("世界掉落物")]
        [ShowIf("SpawnAsWorldDrop")]
        [LabelText("世界掉落物预制体")]
        [Tooltip("为空时 LootDropManager 使用默认通用预制体")]
        public GameObject WorldDropPrefab;

        [TitleGroup("世界掉落物")]
        [ShowIf("SpawnAsWorldDrop")]
        [LabelText("存在时间（秒）")]
        [MinValue(5f)]
        public float WorldDropLifetime = 60f;
    }
}
