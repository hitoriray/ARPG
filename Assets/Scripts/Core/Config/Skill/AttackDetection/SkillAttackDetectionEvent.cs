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
        [LabelText("击退力度")] public Vector3 RepelStrength;  // 击退力度向量（技能自身坐标系下，x=左右 y=上下 z=前）
        [LabelText("击退时间")] public float RepelTime;         // 击退持续时间
        [LabelText("击退方向")] public KnockbackDirection KnockbackDirection = KnockbackDirection.PlayerOpposite;
        [LabelText("命中特效预制体")] public GameObject HitEffectPrefab;
        [LabelText("命中音效")] public AudioClip HitAudioClip;

        [LabelText("镜头震动力度"), Min(0f)]
        public float CameraShakeForce = 0f;     // 0 = 不震动
        [LabelText("时停时长(秒)"), Min(0f)]
        public float HitStopDuration = 0f;      // 0 = 不时停
        [LabelText("时停时间缩放"), Range(0.01f, 0.5f)]
        public float HitStopTimeScale = 0.05f;  // 时停期间的时间缩放比例
    }

    /// <summary>
    /// 击退方向枚举
    /// </summary>
    public enum KnockbackDirection
    {
        [LabelText("攻击者反方向（默认）")] PlayerOpposite = 0,  // 从攻击者指向被击者方向
        [LabelText("世界坐标方向")] WorldSpace = 1,              // RepelStrength 使用世界坐标
        [LabelText("技能自身前向")] SkillForward = 2,            // 施法者前向
    }
    
    #endregion
}