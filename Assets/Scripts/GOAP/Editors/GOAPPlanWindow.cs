#if UNITY_EDITOR
using System.Collections.Generic;
using GOAP.Plan;
using UnityEditor;
using UnityEngine;

namespace GOAP.Editor
{
    public class GOAPPlanWindow : EditorWindow
    {
        [MenuItem("GOAP/GOAP Plan Window")]
        static void OpenWindow()
        {
            GetWindow<GOAPPlanWindow>();
        }

        private GOAPPlan plan;
        private Vector2 scrollPos;

        private void OnGUI()
        {
            if (Selection.gameObjects.Length == 0)
                return;
            GameObject go = Selection.gameObjects[0];
            if (go == null)
                return;
            GOAPAgent agent = go.GetComponent<GOAPAgent>();
            if (agent == null)
                return;
            plan = agent.plan;
            if (plan == null || plan.root == null || string.IsNullOrEmpty(plan.goalName))
                return;
            EditorGUILayout.LabelField($"计划:{plan.goalName}");
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            GOAPPlanNode root = plan.root;
            Color oldColor = GUI.color;
            PrintNode(root);
            GUI.color = oldColor;
            GUILayout.EndScrollView();
        }

        private void PrintNode(GOAPPlanNode node, int depth = 0)
        {
            string prefix = new string(' ', depth * 6);
            string nodeName = $"{prefix}{node.action.GetType().Name}";
            GUI.color = plan.runningNode == node ? Color.red : Color.yellow;
            EditorGUILayout.LabelField(nodeName, EditorStyles.boldLabel);
            foreach (var child in node.children)
            {
                PrintNode(child, depth + 1);
            }
        }

        #region 测试

        private void Test()
        {
            plan = new GOAPPlan
            {
                goalName = "测试计划",
                root = new GOAPPlanNode { action = new TestAction() }
            };
            CreateTestPlanData(plan.root, 3, 3, 0);
            EditorGUILayout.LabelField($"计划:{plan.goalName}");
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GOAPPlanNode root = plan.root;
            Color oldColor = GUI.color;
            PrintNode(root);
            GUI.color = oldColor;
            GUILayout.EndScrollView();
        }

        private void CreateTestPlanData(GOAPPlanNode node, int length, int maxDepth, int currentDepth = 0)
        {
            if (currentDepth == maxDepth) return;
            node.children = new List<GOAPPlanNode>(length);
            for (int i = 0; i < length; i++)
            {
                GOAPPlanNode tempNode = new GOAPPlanNode { action = new TestAction() };
                node.children.Add(tempNode);
                CreateTestPlanData(tempNode, length, maxDepth, currentDepth + 1);
            }
        }

        #endregion
    }
}
#endif
