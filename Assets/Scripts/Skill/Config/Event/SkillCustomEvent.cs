namespace Config
{
    public class SkillCustomEvent
    {
        public SkillEventType EventType;
        public string CustomEventName;
        public int IntArg;
        public float FloatArg;
        public string StringArg;
        public UnityEngine.Object ObjectArg;
    }

    public enum SkillEventType
    {
        Custom,             // 自定义事件类型
        CanSkillRelease,    // 取消后摇
        CanRotate,          // 可以旋转
        CannotRotate,       // 不可以旋转
        AddBuff,            // 添加Buff
        RemoveBuff,         // 移除Buff
    }
}