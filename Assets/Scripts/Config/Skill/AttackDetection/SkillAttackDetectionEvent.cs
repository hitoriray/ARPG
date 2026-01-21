using System;
using UnityEngine;

namespace Config
{
    public class SkillAttackDetectionEvent
    {
#if UNITY_EDITOR
        public string TrackName = "伤害检测轨道";
#endif
        public int FrameIndex = 0;
        public int DurationFrame = 10;
        public AttackDetectionDataBase AttackDetectionData;

#if UNITY_EDITOR
        public AttackDetectionType AttackDetectionType
        {
            get
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
}