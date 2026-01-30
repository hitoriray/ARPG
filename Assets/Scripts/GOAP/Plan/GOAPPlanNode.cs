using System.Collections.Generic;
using GOAP.Action;
using JKFrame;

namespace GOAP.Plan
{
    public class GOAPPlanNode
    {
        public GOAPActionBase action; // 当前节点的行为
        public GOAPPlanNode parent;   // 父节点
        public int indexAtParent;     // 自身是父节点的第几个元素
        public List<GOAPPlanNode> children = new(); // 子节点（即前置节点）

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

        public GOAPRunState Start()
        {
            return action.StartRun();
        }
        
        public GOAPRunState Update()
        {
            return action.OnUpdate();
        }
        
        public void Stop()
        {
            action?.OnStop();
        }
    }
}