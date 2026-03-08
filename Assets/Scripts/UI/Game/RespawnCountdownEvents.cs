namespace UI
{
    /// <summary>
    /// Respawn countdown event keys shared by gameplay logic and UI.
    /// </summary>
    public static class RespawnCountdownEvents
    {
        /// <summary>
        /// Optional: if this window key exists in JKFrame settings, gameplay will auto show/close it.
        /// </summary>
        public const string WindowTypeKey = "UI.UI_RespawnCountdownWindow";

        /// <summary>
        /// Payload: float totalSeconds.
        /// </summary>
        public const string CountdownStartEvent = "PlayerRespawnCountdownStart";

        /// <summary>
        /// Payload: float remainSeconds.
        /// </summary>
        public const string CountdownTickEvent = "PlayerRespawnCountdownTick";

        /// <summary>
        /// Payload: none.
        /// </summary>
        public const string CountdownEndEvent = "PlayerRespawnCountdownEnd";
    }
}
