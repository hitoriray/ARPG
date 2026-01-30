using Config;
using Skill.Behaviour;
using UnityEngine;

namespace Battle.ECS.Component
{
    /// <summary>
    /// 攻击检测请求（由外部技能系统发起）
    /// </summary>
    public struct AttackDetectionRequest
    {
        public SkillBehaviourBase Behaviour;
        public SkillAttackDetectionEvent DetectionEvent;
        public Transform ModelTransform;
        public ICharacter Source;
        public LayerMask DetectionLayer;
    }
}
