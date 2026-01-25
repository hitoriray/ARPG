using JKFrame;
using UnityEngine;

namespace Skill.Buff
{
    public enum BuffEffectType
    {
        Hp,
    }

    [CreateAssetMenu(menuName = "Config/BuffConfig")]
    public class BuffConfig : ConfigBase
    {
        public string buffName;
        [Multiline] public string description;
        public Sprite icon;
        public int maxStack = 1;
        public bool canStack => maxStack > 1;
        public float duration;
        public float tickTime;
        public BuffEffectDataBase startEffect;  // 开始效果
        public BuffEffectDataBase tickEffect;   // 驱动效果
        public BuffEffectDataBase endEffect;    // 结束效果
    }

    public abstract class BuffEffectDataBase
    {
    }

    public class SimpleBuffEffectData : BuffEffectDataBase
    {
        public BuffEffectType type;
        public float value;
    }
}