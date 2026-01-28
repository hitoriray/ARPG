using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Config
{
    public class SkillAttackDetectionEvent
    {
#if UNITY_EDITOR
        [LabelText("轨道名称")] public string TrackName = "伤害检测轨道";
#endif
        [LabelText("起始帧")] public int FrameIndex = 0;
        [LabelText("持续帧数")] public int DurationFrame = 10;
        [LabelText("攻击检测数据")] public AttackDetectionDataBase AttackDetectionData;
        [LabelText("攻击命中数据")] public AttackHitConfig AttackHitConfig = new();
        
        public AttackDetectionType GetAttackDetectionType()
        {
            switch (AttackDetectionData)
            {
                case null:
                    return AttackDetectionType.None;
                case WeaponDetectionData:
                    return AttackDetectionType.Weapon;
                case BoxDetectionData:
                    return AttackDetectionType.Box;
                case SphereDetectionData:
                    return AttackDetectionType.Sphere;
                case FanDetectionData:
                    return AttackDetectionType.Fan;
                default:
                    return AttackDetectionType.None;
            }
        }

#if UNITY_EDITOR
        public AttackDetectionType AttackDetectionType
        {
            get => GetAttackDetectionType();
            set
            {
                // 如果类型发生了变化
                if (value != AttackDetectionType)
                {
                    switch (value)
                    {
                        case AttackDetectionType.None:
                            AttackDetectionData = null;
                            break;
                        case AttackDetectionType.Weapon:
                            AttackDetectionData = new WeaponDetectionData();
                            break;
                        case AttackDetectionType.Box:
                            AttackDetectionData = new BoxDetectionData();
                            break;
                        case AttackDetectionType.Sphere:
                            AttackDetectionData = new SphereDetectionData();
                            break;
                        case AttackDetectionType.Fan:
                            AttackDetectionData = new FanDetectionData();
                            break;
                        default:
                            AttackDetectionData = null;
                            break;
                    }
                }
            }
        }
#endif
    }
    
    #region 检测
    /// <summary>
    /// 攻击检测类型
    /// </summary>
    public enum AttackDetectionType
    {
        None,
        Weapon,
        Box,
        Sphere,
        Fan,
    }

    /// <summary>
    /// 攻击检测数据基类
    /// </summary>
    public abstract class AttackDetectionDataBase
    {
        
    }

    /// <summary>
    /// 武器检测
    /// </summary>
    public class WeaponDetectionData : AttackDetectionDataBase
    {
        public string WeaponName;
    }
    
    /// <summary>
    /// 形状类检测（如Box，Sphere等）
    /// </summary>
    public abstract class ShapeDetectionDataBase : AttackDetectionDataBase
    {
        public Vector3 Position;
    }

    /// <summary>
    /// 盒型检测
    /// </summary>
    public class BoxDetectionData : ShapeDetectionDataBase
    {
        public Vector3 Rotation;
        public Vector3 Scale = Vector3.one;
    }

    /// <summary>
    /// 球形检测
    /// </summary>
    public class SphereDetectionData : ShapeDetectionDataBase
    {
        public float Radius = 1.0f;
    }

    public class FanDetectionData : ShapeDetectionDataBase
    {
        public Vector3 Rotation;
        public float InsideRadius = 1f; // 内圆半径
        public float Radius = 3f;       // 外圆半径
        public float Height = 0.5f;     // 厚度
        public float Angle = 90f;       // 角度
    }
    #endregion
        
    #region 命中

    /// <summary>
    /// 攻击命中配置
    /// </summary>
    public class AttackHitConfig
    {
        [LabelText("攻击系数")] public float AttackMultiply;
        [LabelText("击退程度（矢量）")] public Vector3 RepelStrength; // 击退程度，矢量
        [LabelText("击退时间")] public float RepelTime;     // 击退时间
        [LabelText("命中特效预制体")] public GameObject HitEffectPrefab;
        [LabelText("命中音效")] public AudioClip HitAudioClip;
        // TODO: 加特效位移，声音音量
    }
    
    #endregion
}