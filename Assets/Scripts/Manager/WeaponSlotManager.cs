using System.Collections.Generic;
using Sirenix.OdinInspector;
using Skill;
using UnityEngine;

namespace Manager
{
    /// <summary>
    /// 武器槽位管理器，挂在角色根节点上
    /// 自动收集所有子物体的 WeaponController 并按 slotIndex 缓存
    /// </summary>
    public class WeaponSlotManager : MonoBehaviour
    {
        private Dictionary<int, WeaponController> slotDict = new();
        private WeaponController[] allSlots;
        
        [ShowInInspector, ReadOnly]
        public int SlotCount => slotDict.Count;
        
        /// <summary>
        /// 刷新槽位缓存（在运行时动态添加槽位后调用）
        /// </summary>
        public void RefreshSlots()
        {
            slotDict.Clear();
            allSlots = GetComponentsInChildren<WeaponController>(true);
            foreach (var slot in allSlots)
            {
                if (!slotDict.TryAdd(slot.SlotIndex, slot))
                {
                    RayDebug.Warn($"[WeaponSlotManager] 发现重复的槽位索引: {slot.SlotIndex}，物体: {slot.gameObject.name}");
                }
            }
        }
        /// <summary>
        /// 根据槽位索引获取 WeaponController
        /// </summary>
        public WeaponController GetSlot(int slotIndex)
        {
            slotDict.TryGetValue(slotIndex, out var slot);
            return slot;
        }
        /// <summary>
        /// 获取所有槽位
        /// </summary>
        public WeaponController[] GetAllSlots()
        {
            return allSlots;
        }
        /// <summary>
        /// 在指定槽位创建武器
        /// </summary>
        /// <param name="slotIndex">槽位索引</param>
        /// <param name="prefabOverride">可选的武器预制体覆盖</param>
        public void CreateWeapon(int slotIndex, GameObject prefabOverride = null)
        {
            var slot = GetSlot(slotIndex);
            if (slot != null)
            {
                slot.CreateWeapon(prefabOverride);
            }
            else
            {
                RayDebug.Warn($"[WeaponSlotManager] 未找到槽位索引: {slotIndex}");
            }
        }
        /// <summary>
        /// 销毁指定槽位的武器
        /// </summary>
        /// <param name="slotIndex">槽位索引，-1 表示销毁所有槽位的武器</param>
        public void DestroyWeapon(int slotIndex)
        {
            if (slotIndex == -1)
            {
                // 销毁所有
                foreach (var slot in allSlots)
                {
                    slot.DestroyWeapon();
                }
            }
            else
            {
                var slot = GetSlot(slotIndex);
                if (slot != null)
                {
                    slot.DestroyWeapon();
                }
                else
                {
                    RayDebug.Warn($"[WeaponSlotManager] 未找到槽位索引: {slotIndex}");
                }
            }
        }
    }
}