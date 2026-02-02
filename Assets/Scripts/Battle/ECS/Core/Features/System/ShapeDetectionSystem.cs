using Arch.Core;
using Arch.Core.Extensions;
using Arch.Extend.System;
using Battle.ECS.Component;
using Battle.ECS.Core;
using Config;
using Skill;
using UnityEngine;

namespace Battle.ECS.System
{
    /// <summary>
    /// 攻击检测系统 - 处理技能检测请求
    /// </summary>
    public sealed class ShapeDetectionSystem : IUpdateLevelSystem<GameLogic>
    {
        private readonly BattleContext _context;
        private readonly QueryDescription _query = new QueryDescription().WithAll<ShapeDetectionRequest>().WithNone<Death, Destroy>();

        public ShapeDetectionSystem(BattleContext context)
        {
            _context = context;
        }

        public void Update()
        {
            var processor = new AttackDetectionProcessor();
            _context.World.InlineEntityQuery<AttackDetectionProcessor, ShapeDetectionRequest>(in _query, ref processor);
        }

        private struct AttackDetectionProcessor : IForEachWithEntity<ShapeDetectionRequest>
        {
            public void Update(Entity entity, ref ShapeDetectionRequest req)
            {
                if (req.DetectionEvent == null || req.ModelTransform == null || req.Behaviour == null || req.Source == null)
                    return;

                var detectionType = req.DetectionEvent.GetAttackDetectionType();
                if (detectionType == AttackDetectionType.None || detectionType == AttackDetectionType.Weapon)
                    return;

                var colliders = SkillAttackDetectionHelper.ShapeDetection(req.ModelTransform,
                    req.DetectionEvent.AttackDetectionData, detectionType, req.DetectionLayer);
                if (colliders == null)
                    return;

                Vector3 hitPoint = Vector3.zero;
                if (req.DetectionEvent.AttackDetectionData is ShapeDetectionDataBase shapeData)
                    hitPoint = shapeData.Position;

                float attackValue = req.Source.GetAttackValue(req.DetectionEvent);
                foreach (var col in colliders)
                {
                    if (col == null)
                        continue;
                    IHitTarget hitTarget = col.GetComponentInChildren<IHitTarget>();
                    if (hitTarget == null)
                        continue;

                    var attackData = new AttackData
                    {
                        detectionEvent = req.DetectionEvent,
                        source = req.Source,
                        attackValue = attackValue,
                        hitPoint = hitPoint
                    };
                    RayDebug.Log($"由Ecs触发Shape命中检测:{col.name}");
                    req.Behaviour.OnAttackDetection(hitTarget, attackData);
                }
                
                entity.TryAdd(new Death());
            }
        }
    }
}
