namespace Battle.ECS.Component
{
    /// <summary>
    /// 当前激活的技能
    /// </summary>
    public struct ActiveSkill
    {
        public int SkillId;
        public float Elapsed;
        public int CurrentFrame;
        
        public ActiveSkill(int skillId)
        {
            SkillId = skillId;
            Elapsed = 0;
            CurrentFrame = 0;
        }
    }
}