using System.Text;
using UnityEditor;
using UnityEngine;
using Battle.ECS.Core.Helper;
using Battle.ECS.Examples;
using Config;
using Arch.Core;
using Arch.Core.Extensions;
using Battle.ECS;
using Battle.ECS.Component;

namespace Editor.BattleDebug
{
    /// <summary>
    /// Buff调试窗口
    /// </summary>
    public class BuffDebugWindow : EditorWindow
    {
        private BuffConfig selectedBuffConfig;
        private int stackCount = 1;
        private Vector2 scrollPos;
        private Vector2 logScrollPos;
        private StringBuilder debugLog = new StringBuilder();
        private bool autoRefresh = true;
        private double lastRefreshTime;

        [MenuItem("游戏工具/战斗调试/Buff调试")]
        public static void ShowWindow()
        {
            var window = GetWindow<BuffDebugWindow>("Buff调试");
            window.minSize = new Vector2(600, 800);
        }

        private void OnEnable()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (!Application.isPlaying || !autoRefresh) return;

            // 每0.5秒刷新一次
            if (EditorApplication.timeSinceStartup - lastRefreshTime > 0.5)
            {
                lastRefreshTime = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        private void OnGUI()
        {
            GUILayout.Label("Buff 调试工具", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // ========== 配置区域 ==========
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("配置", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Buff配置:", GUILayout.Width(100));
            selectedBuffConfig = EditorGUILayout.ObjectField(selectedBuffConfig, typeof(BuffConfig), false) as BuffConfig;
            EditorGUILayout.EndHorizontal();

            if (selectedBuffConfig != null)
            {
                EditorGUILayout.HelpBox(
                    $"名称: {selectedBuffConfig.buffName}\n" +
                    $"叠加模式: {selectedBuffConfig.stackMode}\n" +
                    $"最大层数: {selectedBuffConfig.maxStack}\n" +
                    $"持续时间: {selectedBuffConfig.duration}秒",
                    MessageType.Info
                );

                if (GUILayout.Button("验证Buff配置"))
                {
                    ValidateBuffConfig();
                }
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("叠加层数:", GUILayout.Width(100));
            stackCount = EditorGUILayout.IntField(stackCount, GUILayout.Width(50));
            stackCount = Mathf.Max(1, stackCount);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            // ========== 操作按钮区域 ==========
            EditorGUILayout.BeginVertical("box");
            GUILayout.Label("操作", EditorStyles.boldLabel);

            GUI.enabled = selectedBuffConfig != null && Application.isPlaying;

            if (GUILayout.Button("添加Buff到玩家", GUILayout.Height(30)))
            {
                AddBuffToPlayer();
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("打印Buff详情", GUILayout.Height(30)))
            {
                PrintBuffList();
            }
            if (GUILayout.Button("打印属性", GUILayout.Height(30)))
            {
                PrintAttributes();
            }
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("清除玩家所有Buff", GUILayout.Height(30)))
            {
                ClearAllBuffs();
            }

            GUI.enabled = true;
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();

            // ========== 实时监控区域 ==========
            if (Application.isPlaying)
            {
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("实时监控", EditorStyles.boldLabel);
                autoRefresh = EditorGUILayout.Toggle("自动刷新", autoRefresh);
                if (GUILayout.Button("手动刷新", GUILayout.Width(80)))
                {
                    Repaint();
                }
                EditorGUILayout.EndHorizontal();

                DrawBuffList();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();

                // ========== 日志区域 ==========
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("调试日志", EditorStyles.boldLabel);
                if (GUILayout.Button("清空日志", GUILayout.Width(80)))
                {
                    debugLog.Clear();
                }
                EditorGUILayout.EndHorizontal();

                logScrollPos = EditorGUILayout.BeginScrollView(logScrollPos, GUILayout.Height(150));
                EditorGUILayout.TextArea(debugLog.ToString(), GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
                EditorGUILayout.EndVertical();
            }
            else
            {
                EditorGUILayout.HelpBox("需要在运行时使用，请先进入Play模式", MessageType.Warning);
            }
        }

        private void AddBuffToPlayer()
        {
            try
            {
                var context = BattleEcsRunner.Instance.Context;
                var playerEntity = context.PlayerIndex.GetEntity(0);

                LogMessage($"====== 添加Buff: {selectedBuffConfig.buffName} x{stackCount} ======");

                var buffEntity = BuffHelper.AddBuff(context, "DebugWindow", playerEntity, playerEntity, selectedBuffConfig, stackCount);

                if (buffEntity.IsAlive())
                {
                    LogMessage($"✓ Buff添加成功! BuffEntity: {buffEntity.Id}");
                    BuffDebugHelper.PrintBuffList(playerEntity, "添加后");
                }
                else
                {
                    LogMessage("✗ Buff添加失败!");
                }
            }
            catch (System.Exception e)
            {
                LogMessage($"✗ 错误: {e.Message}");
                RayDebug.Error($"[BuffDebug] 添加Buff失败: {e}");
            }
        }

        private void ClearAllBuffs()
        {
            try
            {
                var context = BattleEcsRunner.Instance.Context;
                var playerEntity = context.PlayerIndex.GetEntity(0);

                if (!playerEntity.Has<BuffList>())
                {
                    LogMessage("玩家没有任何Buff");
                    return;
                }

                ref var buffList = ref playerEntity.Get<BuffList>();
                int count = buffList.Value.Count;

                // 标记所有Buff为死亡
                for (int i = 0; i < buffList.Value.Count; i++)
                {
                    var buffEntity = buffList.Value[i];
                    if (buffEntity.IsAlive() && !buffEntity.Has<Death>())
                    {
                        buffEntity.Add(new Death());
                    }
                }

                LogMessage($"✓ 已清除 {count} 个Buff");
            }
            catch (System.Exception e)
            {
                LogMessage($"✗ 错误: {e.Message}");
                RayDebug.Error($"[BuffDebug] 清除Buff失败: {e}");
            }
        }

        private void PrintBuffList()
        {
            try
            {
                var context = BattleEcsRunner.Instance.Context;
                var playerEntity = context.PlayerIndex.GetEntity(0);
                BuffDebugHelper.PrintBuffList(playerEntity, "玩家");
            }
            catch (System.Exception e)
            {
                LogMessage($"✗ 错误: {e.Message}");
            }
        }

        private void PrintAttributes()
        {
            try
            {
                var context = BattleEcsRunner.Instance.Context;
                var playerEntity = context.PlayerIndex.GetEntity(0);
                BuffDebugHelper.PrintAttributes(playerEntity, "玩家");
            }
            catch (System.Exception e)
            {
                LogMessage($"✗ 错误: {e.Message}");
            }
        }

        private void ValidateBuffConfig()
        {
            if (selectedBuffConfig == null) return;
            BuffDebugHelper.ValidateBuffConfig(selectedBuffConfig);
        }

        private void DrawBuffList()
        {
            try
            {
                var context = BattleEcsRunner.Instance.Context;
                var playerEntity = context.PlayerIndex.GetEntity(0);

                if (!playerEntity.IsAlive() || !playerEntity.Has<BuffList>())
                {
                    EditorGUILayout.HelpBox("玩家没有任何Buff", MessageType.Info);
                    return;
                }

                ref var buffList = ref playerEntity.Get<BuffList>();

                if (buffList.Value.Count == 0)
                {
                    EditorGUILayout.HelpBox("玩家没有任何Buff", MessageType.Info);
                    return;
                }

                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(250));

                for (int i = 0; i < buffList.Value.Count; i++)
                {
                    var buffEntity = buffList.Value[i];
                    if (!buffEntity.IsAlive()) continue;

                    DrawBuffItem(buffEntity, i);
                }

                EditorGUILayout.EndScrollView();
            }
            catch (System.Exception e)
            {
                EditorGUILayout.HelpBox($"错误: {e.Message}", MessageType.Error);
            }
        }

        private void DrawBuffItem(Entity buffEntity, int index)
        {
            ref var buff = ref buffEntity.Get<Buff>();
            ref var buffStack = ref buffEntity.Get<BuffStack>();
            ref var buffProperty = ref buffEntity.Get<BuffProperty>();

            EditorGUILayout.BeginVertical("box");

            // 标题行
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"[{index}] {buff.Config.buffName}", EditorStyles.boldLabel);

            // 删除按钮
            if (GUILayout.Button("✗", GUILayout.Width(25)))
            {
                if (!buffEntity.Has<Death>())
                {
                    buffEntity.Add(new Death());
                    LogMessage($"移除Buff: {buff.Config.buffName}");
                }
            }
            EditorGUILayout.EndHorizontal();

            // 基础信息
            EditorGUILayout.LabelField($"ID: {buff.Config.buffId}");
            EditorGUILayout.LabelField($"模式: {buffProperty.StackMode}");
            EditorGUILayout.LabelField($"层数: {buffStack.Value.Count}/{buffProperty.MaxStack}");

            // 每层的剩余时间
            if (buffStack.Value.Count > 0)
            {
                EditorGUILayout.LabelField("堆叠详情:");
                EditorGUI.indentLevel++;

                for (int j = 0; j < buffStack.Value.Count; j++)
                {
                    var stackInfo = buffStack.Value[j];
                    float remaining = (float)stackInfo.RemainingTime;
                    float total = (float)buffProperty.Duration;
                    float percent = remaining / total;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"第{j + 1}层:", GUILayout.Width(50));
                    EditorGUI.ProgressBar(
                        EditorGUILayout.GetControlRect(GUILayout.Width(150)),
                        percent,
                        $"{remaining:F1}s / {total:F1}s"
                    );
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUI.indentLevel--;
            }

            // Tick信息
            if (buffEntity.Has<Tick>())
            {
                ref var tick = ref buffEntity.Get<Tick>();
                EditorGUILayout.LabelField($"Tick: {(float)tick.Elapsed:F1}s / {(float)tick.Interval:F1}s (剩余{tick.Count}次)");
            }

            EditorGUILayout.EndVertical();
        }

        private void LogMessage(string message)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            debugLog.AppendLine($"[{timestamp}] {message}");

            // 限制日志长度
            if (debugLog.Length > 10000)
            {
                debugLog.Remove(0, 5000);
            }
        }
    }
}
