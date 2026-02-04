using Config;
using Manager;
using RayAnimation;
using Skill;
using UnityEngine;

namespace Enemy
{
    public class EnemyController : MonoBehaviour, ICharacter
    {
        public void OnHit(AttackData attackData)
        {
            RayDebug.Log($"敌人被命中: {attackData.attackValue}");
        }

        public float GetAttackValue(SkillAttackDetectionEvent detectionEvent)
        {
            return 0;
        }

        public void OnSkillRotate()
        {
            
        }

        public void AddBuff(BuffConfig buffConfig, int stack)
        {
            
        }
        
        public void CreateWeapon(int slotIndex, GameObject weaponPrefab)
        {
        }

        public void DestroyWeapon(int slotIndex)
        {
        }

        public void Change2IdleState()
        {
            
        }

        public void OnSkillMove(Vector3 deltaPos)
        {
            
        }

        public void OnSkillRotate(Quaternion deltaRot)
        {
            
        }

        public AnimationController AnimationController { get; }
        public Transform ModelTransform { get; }
    }
}