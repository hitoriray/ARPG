using UnityEngine;

namespace Skill
{
    /// <summary>
    /// 挂在武器实例本身，将 Trigger 事件代理回 WeaponController
    /// 解决：当角色根节点有 Rigidbody 时，子物体碰撞事件不会传递到 WeaponController 的问题
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WeaponHitDetector : MonoBehaviour
    {
        private WeaponController owner;

        private void Awake()
        {
            // Unity 要求触发器双方至少有一个有 Rigidbody 才能产生 OnTrigger 事件
            // 给武器实例自身加一个 Kinematic Rigidbody，不参与物理模拟
            if (GetComponent<Rigidbody>() == null)
            {
                var rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        public void Init(WeaponController owner)
        {
            this.owner = owner;
        }

        private void OnTriggerEnter(Collider other)
        {
            owner?.OnWeaponTriggerEnter(other);
        }

        private void OnTriggerStay(Collider other)
        {
            owner?.OnWeaponTriggerStay(other);
        }

        private void OnTriggerExit(Collider other)
        {
            owner?.OnWeaponTriggerExit(other);
        }
    }
}
