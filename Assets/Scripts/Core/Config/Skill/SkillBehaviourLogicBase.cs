namespace Config
{
    /// <summary>
    /// 技能运行逻辑的抽象基类标记
    /// 存放在 Core 配置层中以解除具体的 Battle 层逻辑对数据的耦合
    /// 具体实现由 Battle 层中的 SkillBehaviourBase 等去补全
    /// </summary>
    public abstract class SkillBehaviourLogicBase
    {
    }
}
