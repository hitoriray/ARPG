using Arch.Core;
using Arch.Core.Extensions;
using Battle.ECS.Component;
using FixMath;
using UnityEngine;

namespace Battle.ECS.Core.Helper
{
    /// <summary>
    /// GO 层调用的伤害发射帮助类
    /// 将 AttackData 转化为 ECS DamageRequest 挂载到目标实体
    /// </summary>
    public static class DamageHelper
    {
        /// <summary>
        /// 向目标实体发射一次伤害请求
        /// </summary>
        /// <param name="targetEntity">被攻击的 ECS 实体</param>
        /// <param name="attackData">GO 层的攻击数据</param>
        /// <param name="targetPosition">目标世界坐标（用于计算方向）</param>
        public static void EmitDamage(Entity targetEntity, AttackData attackData, Vector3 targetPosition)
        {
            if (!targetEntity.IsAlive())
            {
                RayDebug.Warn("[DamageHelper] 目标实体已失效，跳过伤害发射");
                return;
            }

            // 已经有未处理的 DamageRequest 时替换（同一帧多次命中取最后一次）
            var hitDir = attackData.hitPoint - targetPosition;
            hitDir.y = 0f;
            if (hitDir.sqrMagnitude < 0.0001f)
            {
                // hitPoint 和目标重合时，使用攻击者朝向作为方向
                if (attackData.source is UnityEngine.Component comp)
                    hitDir = comp.transform.forward;
                else
                    hitDir = Vector3.forward;
            }

            var request = new DamageRequest
            {
                RawDamage     = (FP)attackData.attackValue,
                DefenseIgnore = FP.Zero,
                HitDirection  = (TSVector3)hitDir.normalized,
                HitPoint      = attackData.hitPoint    // 受击点，用于飘字位置
            };

            if (targetEntity.Has<DamageRequest>())
                targetEntity.Set(request);
            else
                targetEntity.Add(request);
        }
    }
}
