using DamageNumbersPro;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// DamageNumberPro 接入层 — 场景单例，持有 3 种飘字预制体
/// 挂在场景常驻 GameObject 上，在 Inspector 中拖入预制体引用
/// </summary>
public class DamageNumberManager : MonoBehaviour
{
    public static DamageNumberManager Instance { get; private set; }

    [Title("飘字预制体")]
    [LabelText("普通伤害"), Required]
    [SerializeField] private DamageNumber normalPrefab;

    [LabelText("暴击伤害（金黄/放大）")]
    [SerializeField] private DamageNumber critPrefab;

    [LabelText("治疗（绿色）")]
    [SerializeField] private DamageNumber healPrefab;

    [Title("显示参数")]
    [LabelText("随机 X 偏移范围"), Range(0f, 1f)]
    [SerializeField] private float randomXOffset = 0.3f;

    [LabelText("头顶高度额外偏移")]
    [SerializeField] private float heightOffset = 0.2f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// 在世界坐标生成一个飘字
    /// worldPos 应已包含头顶偏移（DamageSystem 中传入 hitPoint + up * 1.5）
    /// </summary>
    public void Spawn(float damage, bool isCrit, bool isHeal, Vector3 worldPos)
    {
        DamageNumber prefab = isHeal ? healPrefab : (isCrit ? critPrefab : normalPrefab);

        // 暴击时 critPrefab 可能未配，退回 normalPrefab
        if (prefab == null) prefab = normalPrefab;
        if (prefab == null)
        {
            RayDebug.Warn("[DamageNumberManager] normalPrefab 未配置！");
            return;
        }

        // X 轴随机偏移避免数字连续堆叠
        Vector3 spawnPos = worldPos + new Vector3(
            Random.Range(-randomXOffset, randomXOffset),
            heightOffset,
            0f
        );

        // DNP 自带对象池（预制体上勾选 enablePooling = true）
        prefab.Spawn(spawnPos, damage);
    }
}
