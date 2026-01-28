using System;
using UnityEngine;

namespace Skill
{
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private Collider detectionCollider;
        private LayerMask detectionLayerMask;
        private Action<IHitTarget, AttackData> onDetection;
        private AttackData attackData;

        public void Init(LayerMask detectionLayerMask, Action<IHitTarget, AttackData> onDetection)
        {
            this.detectionLayerMask = detectionLayerMask;
            this.onDetection = onDetection;
            detectionCollider.enabled = false;
        }

        public void StartDetection(AttackData attackData)
        {
            detectionCollider.enabled = true;
            this.attackData = attackData;
        }

        public void StopDetection()
        {
            detectionCollider.enabled = false;
        }

        private void OnTriggerStay(Collider other)
        {
            // 判断是否在LayerMask里
            if ((detectionLayerMask & (1 << other.gameObject.layer)) != 0)
            {
                IHitTarget hitTarget = other.GetComponentInChildren<IHitTarget>();
                if (hitTarget != null)
                {
                    attackData.hitPoint = other.ClosestPoint(transform.position);
                    onDetection?.Invoke(hitTarget, attackData);
                }
            }
        }
    }
}