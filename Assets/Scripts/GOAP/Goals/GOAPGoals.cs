using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace GOAP.Goals
{
    public class GOAPGoals
    {
        public partial class GoalItem
        {
            [LabelText("目标状态")] public GOAPStateType targetState;
            [LabelText("目标趋势")] public GOAPStateComparer targetValue;
            [LabelText("优先级系数"), HorizontalGroup("1")] public float proirityMultiply;
            [LabelText("实时优先级"), HorizontalGroup("1")] public float runtimeProirity;
            [LabelText("可以中断其他目标")] public bool canBreak;
            [LabelText("可以被其他目标中断")] public bool canBeBreak;
            [LabelText("最终优先级"), ShowInInspector, ReadOnly, HorizontalGroup("1")] public float Priority => proirityMultiply * runtimeProirity;
            [LabelText("目标检查器")] public IGOAPGoalChecker goalChecker;
#if UNITY_EDITOR
            public void CheckState()
            {
                if (GOAP.Editor.GOAPEditorUtility.GlobalManager != null
                    && GOAP.Editor.GOAPEditorUtility.GlobalManager.TryGetGlobalState(targetState, out GOAPStateBase state)
                    && (targetValue == null || targetValue.GetType() != state.GetComparerType()))
                {
                    targetValue = state.GetComparer();
                }
                else if (GOAP.Editor.GOAPEditorUtility.agent != null
                         && GOAP.Editor.GOAPEditorUtility.agent.states.TryGetState(targetState, out state)
                         && (targetValue == null || targetValue.GetType() != state.GetComparerType()))
                {
                    targetValue = state.GetComparer();
                }
            }
#endif
        }

        private class SortedGoalComparer : IComparer<string>
        {
            public Dictionary<string, GoalItem> dict;

            public SortedGoalComparer(Dictionary<string, GoalItem> dict)
            {
                this.dict = dict;
            }

            public int Compare(string x, string y)
            {
                if (x == y) return 0;
                int comp = dict[y].Priority.CompareTo(dict[x].Priority);
                if (comp == 0) return -1; // 避免同优先级被去重
                return comp;
            }
        }

        public Dictionary<string, GoalItem> goalItemDict = new();
        private SortedList<string, GoalItem> sortedItemList;

        private GOAPAgent agent;
        private IGOAPOwner owner;

        public void Init(GOAPAgent agent, IGOAPOwner owner)
        {
            this.agent = agent;
            this.owner = owner;
            sortedItemList = new(goalItemDict.Count, new SortedGoalComparer(goalItemDict));
        }

        public SortedList<string, GoalItem> UpdateGoals()
        {
            if (goalItemDict == null || goalItemDict.Count == 0)
                return null;
            sortedItemList.Clear();
            // 更新任务
            foreach (var item in goalItemDict)
            {
                if (item.Value.goalChecker != null)
                {
                    item.Value.goalChecker.Update(item.Value, agent, owner);
                }
                sortedItemList.Add(item.Key, item.Value);
            }

            return sortedItemList;
        }

#if UNITY_EDITOR
        [Button("检查目标状态类型")]
        public void CheckGoalsTargetValueType()
        {
            foreach (var item in goalItemDict)
            {
                item.Value.CheckState();
            }
        }
#endif
    }
}
