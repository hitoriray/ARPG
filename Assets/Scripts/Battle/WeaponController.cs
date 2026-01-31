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
        [ValueDropdown("GetWeaponNameList")]
        [SerializeField] 
        [LabelText("当前选择武器")]
        private string weaponName;
        public string WeaponName => weaponName;
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
                Debug.LogWarning($"未在指定路径 {configPath} 找到配置表，正在全工程搜索...");
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