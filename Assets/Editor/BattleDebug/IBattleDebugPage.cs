namespace Editor.BattleDebug
{
    /// <summary>
    /// 调试页面接口
    /// </summary>
    public interface IBattleDebugPage
    {
        /// <summary>
        /// 页面标题
        /// </summary>
        string Title { get; }
        
        /// <summary>
        /// 页面图标（Emoji）
        /// </summary>
        string Icon { get; }
        
        /// <summary>
        /// 绘制页面内容
        /// </summary>
        void OnGUI();
        
        /// <summary>
        /// 页面启用时调用
        /// </summary>
        void OnEnable() { }
        
        /// <summary>
        /// 页面禁用时调用
        /// </summary>
        void OnDisable() { }
    }
}
