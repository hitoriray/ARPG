using System.Collections.Generic;
using GOAP.Action;
using JKFrame;

namespace GOAP.Plan
{
    public class GOAPPlan
    {
        public GOAPPlanNode root;           // 根节点（最终完成目标效果的节点）
        public GOAPPlanNode runningNode;    // 当前正在运行中的节点
        public string goalName;             // 目标

        public void Init()
        {
        }
    }

    public class GOAPPlanNode
    {
        public GOAPActionBase action; // 当前节点的行为
        public GOAPPlanNode parent;   // 父节点
        public int indexAtParent;     // 自身是父节点的第几个元素
        public List<GOAPPlanNode> children = new(); // 子节点（前置节点）

        public void Destroy()
        {
            if (action == null) return;
            action = null;
            parent?.Destroy();
            parent = null;
            foreach (var child in children)
            {
                child.Destroy();
            }
            children.Clear();
            this.ObjectPushPool();
        }
    }
}