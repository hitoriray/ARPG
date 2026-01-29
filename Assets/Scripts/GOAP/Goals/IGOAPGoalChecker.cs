namespace GOAP.Goals
{
    public interface IGOAPGoalChecker
    {
        /// <summary>
        /// 更新goalItem的优先级
        /// </summary>
        /// <param name="goalItem"></param>
        public void Update(GOAPGoals.GoalItem goalItem, GOAPAgent agent, IGOAPOwner owner);
    }
}