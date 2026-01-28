using Sirenix.OdinInspector;

namespace Config
{
    public class SkillCustomEvent
    {
        [LabelText("事件类型")] public SkillEventType EventType;
        [LabelText("事件名称")] public string CustomEventName;
        [LabelText("Int参数")] public int IntArg;
        [LabelText("Float参数")] public float FloatArg;
        [LabelText("String参数")] public string StringArg;
        [LabelText("Object参数")] public UnityEngine.Object ObjectArg;
    }

    public enum SkillEventType
    {
        Custom,             // 自定义事件类型
        CanSkillRelease,    // 取消后摇
        CanRotate,          // 可以旋转
        CannotRotate,       // 不可以旋转
        AddBuff,            // 添加Buff
    }
}