namespace Battle.ECS.Component
{
    public readonly struct DebugInfo
    {
        public readonly string Info;
        public DebugInfo(string info)
        {
            Info = info;
        }
    }
}