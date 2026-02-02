using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.LowLevel;
using Battle.ECS;
using UnityEditor;
using UnityEngine;

namespace Editor.BattleDebug.Pages
{
    /// <summary>
    /// 实体页 - 实体浏览器和组件查看
    /// </summary>
    public class EntityPage : IBattleDebugPage
    {
        public string Title => "实体";
        public string Icon => "[E]";
        
        private Vector2 _listScrollPosition;
        private Vector2 _detailScrollPosition;
        private string _searchTerm = "";
        private Entity _selectedEntity;
        private List<Entity> _cachedEntities = new();
        private float _lastRefreshTime;
        
        // 组件类型过滤
        private string _componentFilter = "";
        private bool _showOnlyWithComponents = false;
        
        public void OnGUI()
        {
            var runner = BattleEcsRunner.Instance;
            
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("仅在播放模式下可用", MessageType.Info);
                return;
            }
            
            if (runner?.Context?.World == null)
            {
                EditorGUILayout.HelpBox("战斗上下文不可用", MessageType.Warning);
                return;
            }
            
            var world = runner.Context.World;
            
            // 定期刷新实体列表
            if (Time.realtimeSinceStartup - _lastRefreshTime > 0.5f)
            {
                RefreshEntityList(world);
                _lastRefreshTime = Time.realtimeSinceStartup;
            }
            
            EditorGUILayout.BeginHorizontal();
            
            // ============================================
            // 左侧：实体列表 (固定宽度)
            // ============================================
            EditorGUILayout.BeginVertical(GUILayout.Width(220), GUILayout.MinWidth(180));
            
            // 搜索栏
            EditorGUILayout.LabelField("实体列表", BattleDebugStyles.HeaderStyle);
            BattleDebugStyles.DrawSearchField(ref _searchTerm);
            
            // 组件过滤
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("组件过滤:", GUILayout.Width(60));
            _componentFilter = EditorGUILayout.TextField(_componentFilter);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.LabelField($"共 {_cachedEntities.Count} 个实体", BattleDebugStyles.DisabledStyle);
            
            BattleDebugStyles.DrawSeparator();
            
            // 实体列表
            _listScrollPosition = EditorGUILayout.BeginScrollView(_listScrollPosition);
            
            var filteredEntities = FilterEntities();
            foreach (var entity in filteredEntities.Take(100)) // 限制显示数量
            {
                if (!entity.IsAlive()) continue;
                
                var isSelected = entity.Equals(_selectedEntity);
                var style = isSelected ? BattleDebugStyles.TabButtonActiveStyle : BattleDebugStyles.TabButtonStyle;
                
                var label = GetEntityLabel(entity);
                if (GUILayout.Button(label, style))
                {
                    _selectedEntity = entity;
                }
            }
            
            if (filteredEntities.Count > 100)
            {
                EditorGUILayout.LabelField($"... 还有 {filteredEntities.Count - 100} 个实体", BattleDebugStyles.DisabledStyle);
            }
            
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            
            // ============================================
            // 右侧：实体详情 (自适应宽度)
            // ============================================
            EditorGUILayout.BeginVertical("box", GUILayout.ExpandWidth(true));
            
            if (_selectedEntity.IsAlive())
            {
                DrawEntityDetail(_selectedEntity);
            }
            else
            {
                EditorGUILayout.LabelField("选择一个实体查看详情", BattleDebugStyles.CenterStyle);
            }
            
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.EndHorizontal();
        }
        
        private void RefreshEntityList(World world)
        {
            _cachedEntities.Clear();
            
            var count = world.Size;
            if (count == 0) return;
            
            using var entities = new UnsafeArray<Entity>(count);
            world.GetEntities(new QueryDescription(), entities.AsSpan());
            
            foreach (var entity in entities)
            {
                if (entity.IsAlive())
                    _cachedEntities.Add(entity);
            }
        }
        
        private List<Entity> FilterEntities()
        {
            var result = new List<Entity>();
            
            foreach (var entity in _cachedEntities)
            {
                if (!entity.IsAlive()) continue;
                
                // 搜索过滤
                if (!string.IsNullOrEmpty(_searchTerm))
                {
                    var label = GetEntityLabel(entity);
                    if (!label.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase))
                        continue;
                }
                
                // 组件过滤
                if (!string.IsNullOrEmpty(_componentFilter))
                {
                    var archetype = entity.GetArchetype();
                    var components = archetype.Signature.Components.ToArray();
                    bool hasComponent = false;
                    foreach (var comp in components)
                    {
                        if (comp.Type.Name.Contains(_componentFilter, StringComparison.OrdinalIgnoreCase))
                        {
                            hasComponent = true;
                            break;
                        }
                    }
                    if (!hasComponent) continue;
                }
                
                result.Add(entity);
            }
            
            return result;
        }
        
        private string GetEntityLabel(Entity entity)
        {
            var archetype = entity.GetArchetype();
            var components = archetype.Signature.Components.ToArray();
            var typeNames = new string[components.Length];
            for (int i = 0; i < components.Length; i++)
            {
                typeNames[i] = components[i].Type.Name;
            }
            
            // 优先显示重要组件
            string prefix = "Entity";
            if (typeNames.Contains("Player")) prefix = "[Player]";
            else if (typeNames.Contains("Monster")) prefix = "[Monster]";
            else if (typeNames.Contains("Summon")) prefix = "[Summon]";
            else if (typeNames.Contains("Bullet")) prefix = "[Bullet]";
            
            return $"{prefix} #{entity.Id}";
        }
        
        private void DrawEntityDetail(Entity entity)
        {
            EditorGUILayout.LabelField($"实体详情 - #{entity.Id}", BattleDebugStyles.HeaderStyle);
            
            // 操作按钮
            EditorGUILayout.BeginHorizontal();
            
            GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
            if (GUILayout.Button("销毁实体", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("确认", $"确定要销毁实体 #{entity.Id} 吗？", "确定", "取消"))
                {
                    var world = BattleEcsRunner.Instance?.Context?.World;
                    if (world != null && entity.IsAlive())
                    {
                        world.Destroy(entity);
                        _selectedEntity = Entity.Null;
                        // 销毁后立即返回，避免继续访问已销毁的实体
                        GUI.backgroundColor = Color.white;
                        EditorGUILayout.EndHorizontal();
                        return;
                    }
                }
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.EndHorizontal();
            
            BattleDebugStyles.DrawSeparator();
            
            // 组件列表
            EditorGUILayout.LabelField("组件列表", BattleDebugStyles.SubHeaderStyle);
            
            _detailScrollPosition = EditorGUILayout.BeginScrollView(_detailScrollPosition);
            
            var archetype = entity.GetArchetype();
            var componentTypes = archetype.Signature.Components.ToArray();
            foreach (var compType in componentTypes)
            {
                EditorGUILayout.BeginVertical("box");
                
                var type = compType.Type;
                EditorGUILayout.LabelField(type.Name, EditorStyles.boldLabel);
                
                // 尝试获取并显示组件值
                try
                {
                    DrawComponentFields(entity, type);
                }
                catch (Exception ex)
                {
                    EditorGUILayout.LabelField($"无法读取: {ex.Message}", BattleDebugStyles.ErrorStyle);
                }
                
                EditorGUILayout.EndVertical();
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void DrawComponentFields(Entity entity, Type componentType)
        {
            var fields = componentType.GetFields();
            var properties = componentType.GetProperties();
            
            if (fields.Length == 0 && properties.Length == 0)
            {
                EditorGUILayout.LabelField("(Tag组件)", BattleDebugStyles.DisabledStyle);
                return;
            }
            
            EditorGUI.indentLevel++;
            
            // 使用反射获取组件值
            // 注意：这里需要使用Arch的API来获取组件引用
            // 由于泛型限制，我们只能显示基本信息
            
            foreach (var field in fields)
            {
                var fieldType = field.FieldType;
                string typeName = GetFriendlyTypeName(fieldType);
                EditorGUILayout.LabelField($"{field.Name}", $"({typeName})", BattleDebugStyles.DisabledStyle);
            }
            
            foreach (var prop in properties.Where(p => p.CanRead))
            {
                var propType = prop.PropertyType;
                string typeName = GetFriendlyTypeName(propType);
                EditorGUILayout.LabelField($"{prop.Name}", $"({typeName})", BattleDebugStyles.DisabledStyle);
            }
            
            EditorGUI.indentLevel--;
        }
        
        private string GetFriendlyTypeName(Type type)
        {
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            if (type == typeof(Vector3)) return "Vector3";
            return type.Name;
        }
    }
}
