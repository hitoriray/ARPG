using Config;
using Skill;
using UnityEngine;

namespace Enemy
{
    public class EnemyController : MonoBehaviour, ICharacter
    {
        public void OnHit(AttackData attackData)
        {
            Debug.Log($"敌人被命中: {attackData.attackValue}");
        }

        public float GetAttackValue(SkillAttackDetectionEvent detectionEvent)
        {
            return 0;
        }
    }
}