using FixMath;

namespace Battle.ECS.Component
{
    /// <summary>
    /// 伤害请求组件 — 一次性消费组件
    /// 由 GO 层 OnHit 时挂载到目标实体上，DamageSystem 处理后移除
    /// </summary>
    public struct DamageRequest
    {
        /// <summary>原始伤害值（攻击力 × 技能倍率，已在 GO 层计算好）</summary>
        public FP RawDamage;

        /// <summary>无视防御比例 0~1（预留）</summary>
        public FP DefenseIgnore;

        /// <summary>受击方向（世界空间，从攻击者指向被攻击者或 hitPoint - 被攻击者位置）</summary>
        public FixMath.TSVector3 HitDirection;

        /// <summary>受击点世界坐标（用于飘字显示位置）</summary>
        public UnityEngine.Vector3 HitPoint;
    }
}
