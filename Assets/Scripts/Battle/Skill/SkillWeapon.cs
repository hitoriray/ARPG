using System;
using UnityEngine;

namespace Skill
{
    public class SkillWeapon : MonoBehaviour
    {
        [SerializeField] private Collider detectionCollider;
        private LayerMask detectionLayerMask;
        private Action<Collider> onDetection;

        public void Init(LayerMask detectionLayerMask, Action<Collider> onDetection)
        {
            this.detectionLayerMask = detectionLayerMask;
            this.onDetection = onDetection;
            detectionCollider.enabled = false;
            
        }

        public void StartDetection()
        {
            detectionCollider.enabled = true;
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
                onDetection?.Invoke(other);
            }
        }
    }
}