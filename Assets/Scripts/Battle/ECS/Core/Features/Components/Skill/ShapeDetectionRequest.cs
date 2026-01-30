using Config;
using Skill.Behaviour;
using UnityEngine;

namespace Battle.ECS.Component
{
    /// <summary>
    /// Shape攻击检测请求（由外部技能系统发起）
    /// </summary>
    public struct ShapeDetectionRequest
    {
        public SkillBehaviourBase Behaviour;
        public SkillAttackDetectionEvent DetectionEvent;
        public Transform ModelTransform;
        public ICharacter Source;
        public LayerMask DetectionLayer;
    }
}
