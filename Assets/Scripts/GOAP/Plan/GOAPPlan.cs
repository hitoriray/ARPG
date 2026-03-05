using JKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace GOAP.Plan
{
    public class GOAPPlan
    {
        public GOAPPlanNode root; // 根节点（最终完成目标效果的节点）
        public GOAPPlanNode runningNode; // 当前正在运行中的节点
        public string goalName; // 目标

        [ShowInInspector, ReadOnly] public bool Running { get; private set; }
        public GOAPPlanNode StageNode => runningNode.parent;
        public int RunningNodeChildIndex => runningNode.indexAtParent;

        public void StartRun(string goalName, GOAPPlanNode root)
        {
            this.goalName = goalName;
            this.root = root;
            StartRunNode(GetDeepestNode(root));
        }

        public void Stop()
        {
            RecycleNodes(root);
            runningNode?.Stop();
            runningNode = null;
            root = null;
            Running = false;
            goalName = null;
        }

        /// <summary>
        /// 获取整个计划树最左下的那个节点
        /// </summary>
        private GOAPPlanNode GetDeepestNode(GOAPPlanNode root)
        {
            if (root.children.Count == 0) return root;
            var tmp = root.children[0];
            return GetDeepestNode(tmp);
        }

        public void OnUpdate()
        {
            GOAPRunState nodeState = runningNode.Update();
            if (nodeState == GOAPRunState.Succeed)
            {
                runningNode.Stop();
                // 如果完成的是 root 根节点，代表计划完成
                if (runningNode == root)
                {
                    JKLog.Log("任务全部完成！");
                    Stop();
                    return;
                }
                // 有同层可以执行则运行同层的下一个节点
                if (RunningNodeChildIndex + 1 < StageNode.children.Count)
                {
                    StartRunNode(StageNode.children[RunningNodeChildIndex + 1]);
                }
                // 否则运行父节点（往上执行）
                else
                {
                    StartRunNode(StageNode);
                }
            }
            else if (nodeState == GOAPRunState.Failed)
            {
                Stop();
            }
            // 正则执行中... 不处理
        }

        private void StartRunNode(GOAPPlanNode node)
        {
            runningNode = node;
            Running = runningNode.Start() == GOAPRunState.Running;
            if (!Running)
            {
                RecycleNodes(root);
            }
        }

        public void OnDestroy()
        {
            runningNode?.action?.OnDestroy();
            RecycleNodes(root);
        }

        private void RecycleNodes(GOAPPlanNode node)
        {
            if (node == null)
                return;
            foreach (var child in node.children)
            {
                RecycleNodes(child);
            }
            node.action = null;
            node.parent = null;
            node.indexAtParent = 0;
            node.children.Clear();
        }
    }
}