using Arch.Core;
using Config;
using UnityEngine;

namespace Item
{
    /// <summary>
    /// 世界掉落物 MonoBehaviour。
    /// 挂在场景中的掉落物 GameObject 上，由 LootDropManager 管理。
    /// 不负责运动逻辑，只存数据 + 持有对应 ECS Entity 引用。
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class WorldDropItem : MonoBehaviour
    {
        public ItemConfig Config    { get; private set; }
        public int        Count    { get; private set; }
        public Entity     Entity   { get; private set; }

        /// <summary>手动拾取时：玩家是否处于拾取范围内</summary>
        public bool PlayerNearby  { get; set; }

        private Rigidbody _rb;

        public void Init(ItemConfig config, int count, Entity entity)
        {
            Config = config;
            Count = count;
            Entity = entity;
            PlayerNearby = false;

            _rb = GetComponent<Rigidbody>();
            var sphereCollider = GetComponentInChildren<SphereCollider>();
            if (sphereCollider == null)
            {
                RayDebug.Error($"{gameObject.name} 没有 SphereCollider，无法Trigger");
                return;
            }
            sphereCollider.isTrigger = true;
            var maxScale = Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            if (maxScale < 0.0001f) maxScale = 1f;
            sphereCollider.radius = Mathf.Max(0.1f, config.PickupRadius / maxScale);
        }

        /// <summary>
        /// 生成后调用：施加随机弹射冲力，模拟掉落弹出效果。
        /// delay 后锁定物理，防止道具滚太远。
        /// </summary>
        public async void ApplyBounceForce(float lockDelay = -1f)
        {
            if (_rb == null) return;
            _rb.isKinematic = false;

            // 随机斜上方冲力
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), 1.6f, Mathf.Sin(angle)).normalized;
            float force = UnityEngine.Random.Range(1f, 2f);
            _rb.AddForce(dir * force, ForceMode.Impulse);

            if (lockDelay < 0f)
            {
                return;
            }
            
            await Cysharp.Threading.Tasks.UniTask.WaitForSeconds(lockDelay);
            
            if (this != null && _rb != null)
                _rb.isKinematic = true;
        }

        // ── 手动拾取 Trigger（仅 AutoPickup=false 物品有 SphereCollider）─────

        private void OnTriggerEnter(Collider other)
        {
            if (Config == null || Config.AutoPickup) return;
            if (!other.CompareTag("Player")) return;
            
            Debug.Log($"[WorldDropItem] 玩家进入拾取范围: {Config.ItemName}");
            PlayerNearby = true;
            InteractManager.Instance?.RegisterDropItem(this);
        }

        private void OnTriggerExit(Collider other)
        {
            if (Config == null || Config.AutoPickup) return;
            if (!other.CompareTag("Player")) return;
            
            Debug.Log($"[WorldDropItem] 玩家离开拾取范围: {Config.ItemName}");
            PlayerNearby = false;
            InteractManager.Instance?.UnregisterDropItem(this);
        }

        /// <summary>
        /// 重置状态（回池前调用）。
        /// </summary>
        public void Reset()
        {
            Config       = null;
            Count        = 0;
            Entity       = Entity.Null;
            PlayerNearby = false;
            if (_rb != null) _rb.isKinematic = true;
        }

        public void SetCount(int newCount)
        {
            Count = Mathf.Max(0, newCount);
        }
    }
}
