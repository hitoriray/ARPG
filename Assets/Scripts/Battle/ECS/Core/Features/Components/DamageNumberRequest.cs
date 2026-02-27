using UnityEngine;

namespace Battle.ECS.Component
{
    /// <summary>
    /// 伤害飘字请求 — 由 DamageSystem 发起，DamageNumberSystem 消费后移除
    /// </summary>
    public struct DamageNumberRequest
    {
        public float   Damage;      // 实际伤害值（已计算防御后）
        public bool    IsCritical;  // 是否暴击
        public bool    IsHeal;      // 是否治疗（绿字）
        public Vector3 WorldPos;    // 显示位置
    }
}
