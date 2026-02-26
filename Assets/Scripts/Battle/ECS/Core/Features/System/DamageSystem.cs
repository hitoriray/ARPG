using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.Extend.System;
using Battle.ECS.Component;
using Battle.ECS.Core;
using FixMath;
using UnityEngine;

namespace Battle.ECS.System
{
    /// <summary>
    /// 伤害系统 — 处理 DamageRequest：防御减伤 → 暴击判定 → 扣血 → 死亡判定
    /// 每帧消费所有 DamageRequest 后移除该组件
    /// </summary>
    public class DamageSystem : IUpdateLevelSystem<GameFree>
    {
        private readonly BattleContext _context;

        private readonly QueryDescription _query =
            new QueryDescription().WithAll<DamageRequest, Health, Component.Attribute>().WithNone<Death>();

        private readonly List<Entity> _entities = new(16);

        public DamageSystem(BattleContext context)
        {
            _context = context;
        }

        public void Update()
        {
            _entities.Clear();
            _context.World.CollectEntities(in _query, _entities);

            foreach (var entity in _entities)
            {
                ProcessDamage(entity);
            }
        }

        private void ProcessDamage(Entity entity)
        {
            ref var request = ref entity.Get<DamageRequest>();
            ref var health = ref entity.Get<Health>();
            ref var attr = ref entity.Get<Component.Attribute>();

            // 1. 防御减伤
            FP defense = attr.Defense * (FP.One - request.DefenseIgnore);
            FP finalDamage = TSMath.Max(FP.One, request.RawDamage - defense);

            // 2. 暴击判定（使用攻击者属性 — 预留，当前不区分攻击者/被攻击者）
            // 暴击在GO层或攻击者侧计算更合理，此处仅做被击端防御计算

            // 3. 扣血
            health.Current -= finalDamage;
            if (health.Current < FP.Zero)
                health.Current = FP.Zero;

            entity.Set(health);

            RayDebug.Log($"[DamageSystem] 实体{entity.Id} 受到伤害: raw={request.RawDamage} def={defense} final={finalDamage} → HP={health.Current}/{health.Max}");

            // 4. 死亡判定
            if (health.IsDead && !entity.Has<Death>())
            {
                entity.Add(new Death());
            }

            // 5. 移除伤害请求（一次性消费）
            entity.Remove<DamageRequest>();
        }
    }
}
