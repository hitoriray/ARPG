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
            [LabelText("最终优先级"), ShowInInspector, ReadOnly, HorizontalGroup("1")] public float Priority => proirityMultiply * runtimeProirity;
            [LabelText("目标检查器")] public IGOAPGoalChecker goalChecker;
        }

        private partial class SortedGoalComparer : IComparer<string>
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
            List<string> createList = new();
            foreach (var item in goalItemDict)
            {
                if (item.Value == null || 
                    item.Value.targetValue == null ||
                    item.Value.targetValue.GetType() != GOAPGlobalConfig.GetStateValueType(item.Value.targetState))
                {
                    createList.Add(item.Key);
                }
            }

            foreach (var goalName in createList)
            {
                var item = goalItemDict[goalName];
                if (item == null) continue;
                item.targetValue = GOAPGlobalConfig.CopyState(item.targetState).GetComparer();
            }
        }
#endif
    }
}