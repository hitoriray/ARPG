using Attribute;
using Battle.ECS.Component;
using JKFrame;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Config
{
    [CreateAssetMenu(menuName = "Config/BuffConfig")]
    public class BuffConfig : ConfigBase
    {
        [LabelText("Buff ID")] public int buffId;
        [LabelText("名称")] public string buffName;
        [LabelText("描述")] [Multiline] public string description;
        [LabelText("图标")] public Sprite icon;

        [TitleGroup("叠加配置")] [LabelText("最大层数")]
        public int maxStack = 1;

        [LabelText("叠加模式")] public BattleBuffStackMode stackMode = BattleBuffStackMode.RefreshDuration;
        [LabelText("溢出策略")] public BattleBuffOverflowPolicy overflowPolicy = BattleBuffOverflowPolicy.ReplaceOldest;

        [TitleGroup("时间配置")] [LabelText("每层持续时间（秒）")]
        public float duration = 5f;

        [LabelText("Tick间隔（秒）")] public float tickInterval = 1f;
        [LabelText("Tick次数（-1无限）")] public int tickCount = -1;

        [TitleGroup("属性配置")] 
        [LabelText("属性修正")] public BuffAttrModifier[] AttrModifiers;
        [LabelText("速度修正百分比")] public int speedPctModifier = 0;
        
        [TitleGroup("效果配置")]
        [LabelText("开始效果")] public BuffEffectDataBase startEffect; // 开始效果
        [LabelText("周期效果")] public BuffEffectDataBase periodicEffect; // 周期效果
        [LabelText("结束效果")] public BuffEffectDataBase endEffect; // 结束效果
        
        [TitleGroup("特效配置")]
        [LabelText("特效预制体")] public GameObject vfxPrefab;
        [LabelText("挂点类型")] public VfxMountType vfxMountType = VfxMountType.Root;
        
        [TitleGroup("标签配置")]
        [LabelText("Buff标签")] public BuffTag tags = BuffTag.None;
        
        public bool canStack => maxStack > 1;
    }

    /// <summary>
    /// Buff属性修正器
    /// </summary>
    public class BuffAttrModifier
    {
        [LabelText("属性类型")] public AttributeType type;
        [LabelText("修正值")] public float value;
        [LabelText("修正模式")] public AttrModifyMode mode;
    }

    public enum AttrModifyMode
    {
        Fixed,
        Percent,
    }

    /// <summary>
    /// Buff标签
    /// </summary>
    public enum BuffTag
    {
        None = 0,
        Positive = 1 << 0,   // 增益
        Negative = 1 << 1,   // 减益
        Control = 1 << 2,    // 控制
        Dispellable = 1 << 3,// 可驱散
        Permanent = 1 << 4,  // 永久
        Mergeable = 1 << 5,  // 可合并
    }
    
    // 特效挂点类型
    public enum VfxMountType
    {
        Root = 0,       // 根节点
        Head = 1,       // 头顶
        Chest = 2,      // 胸部
        Foot = 3,       // 脚底
        Overhead = 4,   // 头顶上方
    }
    
    public abstract class BuffEffectDataBase
    {
    }

    public class SimpleBuffEffectData : BuffEffectDataBase
    {
        [LabelText("类型")] public BuffEffectType type;
        [LabelText("值")] public float value;
    }
}