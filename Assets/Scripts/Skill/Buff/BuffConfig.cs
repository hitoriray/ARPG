using JKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Config
{
    [CreateAssetMenu(menuName = "Config/BuffConfig")]
    public class BuffConfig : ConfigBase
    {
        [LabelText("名称")] public string buffName;
        [LabelText("描述")] [Multiline] public string description;
        [LabelText("图标")] public Sprite icon;
        [LabelText("最大层数")] public int maxStack = 1;
        public bool canStack => maxStack > 1;
        [LabelText("每层持续时间")] public float duration;
        [LabelText("周期时间")] public float periodicTime;                  // 周期时间
        [LabelText("开始效果")] public BuffEffectDataBase startEffect;      // 开始效果
        [LabelText("周期效果")] public BuffEffectDataBase periodicEffect;   // 周期效果
        [LabelText("结束效果")] public BuffEffectDataBase endEffect;        // 结束效果
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