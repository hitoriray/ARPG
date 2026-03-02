using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Config;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Skill
{
    public class WeaponController : MonoBehaviour
    {
        [LabelText("槽位索引")]
        [SerializeField]
        private int slotIndex;
        public int SlotIndex => slotIndex;

        [ValueDropdown("GetWeaponNameList")]
        [SerializeField]
        [LabelText("当前选择武器")]
        private string weaponName;
        public string WeaponName => weaponName;

        [LabelText("武器预制体")]
        [SerializeField]
        private GameObject weaponPrefab;
        public GameObject WeaponPrefab => weaponPrefab;

        // 从武器身上去获取碰撞体
        private Collider detectionCollider;
        private LayerMask detectionLayerMask;
        private Action<IHitTarget, AttackData> onDetection;
        private AttackData attackData;
        private bool isDetecting;
        private readonly HashSet<Collider> hitCache = new(32);

        // 当前生成的武器实例
        private GameObject currentWeaponInstance;
        public GameObject CurrentWeaponInstance => currentWeaponInstance;

        public void Init(LayerMask detectionLayerMask, Action<IHitTarget, AttackData> onDetection)
        {
            this.detectionLayerMask = detectionLayerMask;
            this.onDetection = onDetection;
            // 先尝试从自身获取武器,有的话直接使用
            detectionCollider = transform.GetComponentInChildren<BoxCollider>();
            if (detectionCollider != null)
            {
                detectionCollider.enabled = false;
            }
        }

        public void StartDetection(AttackData attackData)
        {
            if (detectionCollider != null)
            {
                detectionCollider.isTrigger = true;
                detectionCollider.enabled = true;
            }
            this.attackData = attackData;
            isDetecting = true;
            hitCache.Clear();
        }

        public void StopDetection()
        {
            if (detectionCollider != null)
                detectionCollider.enabled = false;
            isDetecting = false;
            hitCache.Clear();
        }

        // 由 WeaponHitDetector 代理调用，武器实例本身接收物理事件后转发过来
        public void OnWeaponTriggerEnter(Collider other)
        {
            if (!isDetecting) return;
            TryHit(other);
        }

        public void OnWeaponTriggerStay(Collider other)
        {
            if (!isDetecting || hitCache.Contains(other)) return;
            TryHit(other);
        }

        public void OnWeaponTriggerExit(Collider other)
        {
            hitCache.Remove(other);
        }

        private void TryHit(Collider other)
        {
            // RayDebug.Info($"[WeaponController.TryHit] 碰撞物体:{other.gameObject.name}, Layer:{LayerMask.LayerToName(other.gameObject.layer)}({other.gameObject.layer}), detectionLayerMask:{detectionLayerMask.value}, 匹配:{((detectionLayerMask & (1 << other.gameObject.layer)) != 0)}");
            if ((detectionLayerMask & (1 << other.gameObject.layer)) == 0)
                return;
            if (hitCache.Contains(other))
                return;
            IHitTarget hitTarget = other.GetComponentInChildren<IHitTarget>();
            // RayDebug.Info($"[WeaponController.TryHit] 目标:{other.gameObject.name}, IHitTarget:{(hitTarget != null ? hitTarget.GetType().Name : "NULL")}");
            if (hitTarget != null)
            {
                hitCache.Add(other);
                attackData.hitPoint = other.ClosestPoint(transform.position);
                onDetection?.Invoke(hitTarget, attackData);
            }
        }

        public void CreateWeapon(GameObject customPrefab = null)
        {
            DestroyWeapon();

            GameObject prefab = customPrefab != null ? customPrefab : weaponPrefab;
            if (prefab == null)
            {
                RayDebug.Warn($"SlotIndex {slotIndex}: [{weaponName}] 没有武器预制体可以生成");
                return;
            }
            
            currentWeaponInstance = Instantiate(prefab, transform);
            currentWeaponInstance.transform.localPosition = Vector3.zero;
            // currentWeaponInstance.transform.localRotation = Quaternion.identity;
            currentWeaponInstance.transform.localScale = Vector3.one;
            detectionCollider = currentWeaponInstance.GetComponent<BoxCollider>();
            
            // 确保武器碰撞体是Trigger模式
            if (detectionCollider != null)
            {
                detectionCollider.isTrigger = true;
                detectionCollider.enabled = false;
            }
            
            // 挂上代理脚本，让武器实例本身接收物理事件并转发给 WeaponController
            // 这样无论角色身上有没有 Rigidbody 都能正确触发
            var detector = currentWeaponInstance.GetComponent<WeaponHitDetector>();
            if (detector == null)
                detector = currentWeaponInstance.AddComponent<WeaponHitDetector>();
            detector.Init(this);
        }

        public void DestroyWeapon()
        {
            if (currentWeaponInstance != null)
            {
                if (Application.isPlaying)
                    Destroy(currentWeaponInstance);
                else
                    DestroyImmediate(currentWeaponInstance);
                currentWeaponInstance = null;
            }
        }

        private void EnsureRigidbody()
        {
            // Unity中子物体的Trigger事件只有在父物体有Rigidbody时，才能通过父物体身上的脚本（WeaponController）接收到
            var rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
        
        private IEnumerable GetWeaponNameList()
        {
            // 仅在编辑器下运行，防止打包报错
#if UNITY_EDITOR
            string configPath = "Assets/Config/Weapon/WeaponConfig.asset";
            var config = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponConfig>(configPath);
            if (config != null)
            {
                return config.GetAllWeaponNames();
            }
            else
            {
                // 如果没找到，尝试在整个工程搜寻一次，防止路径移动导致失效
                RayDebug.Warn($"未在指定路径 {configPath} 找到配置表，正在全工程搜索...");
                var guids = UnityEditor.AssetDatabase.FindAssets("t:WeaponConfig");
                if (guids.Length > 0)
                {
                    string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                    return UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponConfig>(path).GetAllWeaponNames();
                }
            }
#endif
            return new List<string> { "未找到配置表" };
        }
    }
}