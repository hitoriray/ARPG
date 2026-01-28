using Skill;
using UnityEngine;

namespace Enemy
{
    public class EnemyController : MonoBehaviour, IHitTarget
    {
        public void OnHit()
        {
            Debug.Log("敌人被命中");
        }
    }
}