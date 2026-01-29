using System;
using System.Collections.Generic;
using System.Linq;
using GOAP.Action;
using GOAP.Goals;
using GOAP.Plan;
using JKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GOAP
{
    /// <summary>
    /// GOAP代理组件
    /// </summary>
    public class GOAPAgent : SerializedMonoBehaviour
    {
        [LabelText("目标")] public GOAPGoals goals;
        [LabelText("局部状态")] public GOAPStates states;
        [LabelText("全部行为")] public GOAPActions actions;
        [LabelText("计划树")] public GOAPPlan plan;
        
        private IGOAPOwner owner;
        
        public void Init(IGOAPOwner owner)
        {
            this.owner = owner;
            actions.Init(this, owner);
            goals.Init(this, owner);
        }
        
        public void OnUpdate()
        {
            if (owner == null) 
                return;
            // TODO：计划在执行就不需要去构建任务了
            SortedList<string, GOAPGoals.GoalItem> sortedGoals = goals.UpdateGoals();
            foreach (var item in sortedGoals)
            {
                // 优先级为正 且 可以基于这个目标生成计划
                if (item.Value.Priority > 0 && GeneratePlan(item.Key))
                {
                    Debug.Log($"任务构建成功:{item.Key}");
                    break;
                }
            }
        }

        public void ApplyEffect(GOAPTypeAndComparer effect)
        {
            states.ApplyEffect(effect);
        }
        
        public bool CheckState(GOAPStateType stateType, GOAPStateComparer stateComparer)
        {
            if (GOAPGlobalConfig.IsGlobalState(stateType))
                return GOAPGlobalManager.Instance.GlobalStates.CheckState(stateType, stateComparer);
            return states.CheckState(stateType, stateComparer);
        }
        
        #region 生成计划

        private class PlanNodePriorityComparer : IComparer<GOAPPlanNode>
        {
            public int Compare(GOAPPlanNode x, GOAPPlanNode y)
            {
                return y.action.Priority.CompareTo(x.action.Priority);
            }
        }

        private SortedSet<GOAPPlanNode> GetSortedPlanNodes()
        {
            SortedSet<GOAPPlanNode> nodes = PoolSystem.GetObject<SortedSet<GOAPPlanNode>>();
            if (nodes == null)
            {
                nodes = new SortedSet<GOAPPlanNode>(new PlanNodePriorityComparer());
            }
            return nodes;
        }

        private void RecycleSortedPlanNode(SortedSet<GOAPPlanNode> planNode)
        {
            foreach (var node in planNode)
            {
                node.Destroy();
            }
            planNode.Clear();
            planNode.ObjectPushPool();
        }

        /// <summary>
        /// 找到符合某个效果的所有行为并形成计划节点
        /// </summary>
        /// <param name="targetStateType"></param>
        /// <param name="comparer"></param>
        /// <returns></returns>
        private SortedSet<GOAPPlanNode> GetPlanNodes(GOAPStateType targetStateType, GOAPStateComparer comparer)
        {
            SortedSet<GOAPPlanNode> nodes = GetSortedPlanNodes();
            if (actions.ActionEffectDict.TryGetValue(targetStateType, out var actionList))
            {
                foreach (var action in actionList)
                {
                    foreach (var effect in action.effects)
                    {
                        if (effect.stateType == targetStateType && effect.stateComparer.EqualsComparator(comparer))
                        {
                            action.UpdatePriority();
                            GOAPPlanNode node = ResSystem.GetOrNew<GOAPPlanNode>();
                            node.action = action;
                            nodes.Add(node);
                            break;  // 设计是不允许同样的action
                        }
                    }
                }
            }
            return nodes;
        }

        /// <summary>
        /// 基于根节点构建计划路径
        /// 失败的情况：某个环节中无法达成某个前置条件
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        private bool TryBuildPlanPath(GOAPPlanNode root)
        {
            // 遍历所有条件，必须全部满足才能构建成功
            foreach (var precondition in root.action.preconditions)
            {
                // 当前状态不满足，需要寻找其他可以满足的Action作为子节点
                bool check = CheckState(precondition.stateType, precondition.stateComparer);
                if (!check)
                {
                    SortedSet<GOAPPlanNode> preNodes = GetPlanNodes(precondition.stateType, precondition.stateComparer);
                    GOAPPlanNode targetNode = null;
                    foreach (var preNode in preNodes)
                    {
                        // 避免自己是自己的前提
                        if (preNode != root && TryBuildPlanPath(preNode))
                        {
                            targetNode = preNode;
                            preNode.parent = root;
                            // 塞到父节点的子节点列表的最后
                            preNode.indexAtParent = root.children.Count; 
                            root.children.Add(preNode);
                            check = true;
                            break;
                        }
                    }

                    if (targetNode != null)
                    {
                        preNodes.Remove(targetNode);
                    }
                    RecycleSortedPlanNode(preNodes);
                    if (!check) // 如果还是false意味着当前无法满足条件
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private bool GeneratePlan(string goalName)
        {
            bool success = false;
            GOAPGoals.GoalItem goal = goals.goalItemDict[goalName];
            var targetState = goal.targetState;
            // 获取符合效果的全部 Action 以此尝试构建计划，成功的作为初始Action
            SortedSet<GOAPPlanNode> nodes = GetPlanNodes(targetState, goal.targetValue);
            GOAPPlanNode targetNode = null;
            foreach (var node in nodes)
            {
                if (TryBuildPlanPath(node))
                {
                    success = true;
                    targetNode = node;
                    node.parent = null;
                    node.indexAtParent = 0;
                    plan.goalName = goalName;
                    plan.root = node;
                    break;
                }
            }

            if (targetNode != null)
            {
                nodes.Remove(targetNode);
            }
            RecycleSortedPlanNode(nodes);

            return success;
        }
        
        #endregion
    }
}