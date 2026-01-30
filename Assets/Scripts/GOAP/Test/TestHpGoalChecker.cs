using GOAP.Goals;
using UnityEngine;

namespace GOAP
{
    public class TestHpGoalChecker : IGOAPGoalChecker
    {
        public void Update(GOAPGoals.GoalItem goalItem, GOAPAgent agent, IGOAPOwner owner)
        {
            goalItem.runtimeProirity += Time.deltaTime;
        }
    }
}