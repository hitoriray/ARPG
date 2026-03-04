using System.Collections.Generic;
using Config;
using Data;

namespace Manager
{
    /// <summary>
    /// 玩家管理接口 — UI 层等底层模块依赖此接口与玩家交互
    /// 实体层（PlayerManager）实现此接口并进行注册
    /// </summary>
    public interface IPlayerManager
    {
        ICharacter GetCharacterController();
        CharacterConfig GetCharacterConfig();
        /// <summary>获取当前角色的所有技能配置</summary>
        List<SkillConfig> GetAllSkillConfig();

        /// <summary>开启或关闭角色输入控制</summary>
        void SetCharacterControl(bool canControl);

        /// <summary>为角色添加/升级技能</summary>
        void AddSkill(int skillIndex, SkillLearnedData skillLearnedData);

        void PushUICursor();
        void PopUICursor();
    }

    /// <summary>
    /// 用于向底层模块（如 UI）注入或提供高层 IPlayerManager 实现的静态类
    /// </summary>
    public static class PlayerService
    {
        public static IPlayerManager Instance { get; set; }
    }
}
