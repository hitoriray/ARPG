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
        CancelRecovery,     // 取消后摇
    }
}