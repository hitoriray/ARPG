using Battle.ECS;
using Battle.ECS.Component;
using Config;
using FixMath;
using Skill.Behaviour;
using UnityEngine;

namespace Battle.ECS.Core.Helper
{
    public static class AttackDetectionEmitterHelper
    {
        public static bool Emit(Transform modelTransform, SkillAttackDetectionEvent detectionEvent, SkillBehaviourBase behaviour, ICharacter source, LayerMask detectionLayer)
        {
            if (modelTransform == null || detectionEvent == null || behaviour == null || source == null)
                return false;

            RayDebug.Log($"[{nameof(AttackDetectionEmitterHelper)}]: 由Ecs发射{nameof(ShapeDetectionRequest)}");
            BattleEcsRunner.Instance.Context.World.Create(
                new ShapeDetectionRequest
                {
                    Behaviour = behaviour,
                    DetectionEvent = detectionEvent,
                    ModelTransform = modelTransform,
                    Source = source,
                    DetectionLayer = detectionLayer
                }
            );
            return true;
        }
    }
}
