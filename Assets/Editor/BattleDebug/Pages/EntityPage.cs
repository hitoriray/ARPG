using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.LowLevel;
using Battle.ECS;
using UnityEditor;
using UnityEngine;
using EntityExtensions = Arch.Core.Extensions.EntityExtensions;

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
            
            // 手动刷新按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 刷新", GUILayout.Width(60)))
            {
                RefreshEntityList(world);
                // 清除无效的选中实体
                if (!_selectedEntity.IsAlive())
                {
                    _selectedEntity = Entity.Null;
                }
            }
            BattleDebugStyles.DrawSearchField(ref _searchTerm);
            EditorGUILayout.EndHorizontal();
            
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
                var factionColor = GetFactionColor(entity);
                
                // 根据选中状态和阵营设置颜色
                var originalColor = GUI.backgroundColor;
                GUI.backgroundColor = isSelected 
                    ? Color.Lerp(factionColor, Color.white, 0.5f) 
                    : Color.Lerp(factionColor, Color.gray, 0.6f);
                
                var style = isSelected ? BattleDebugStyles.TabButtonActiveStyle : BattleDebugStyles.TabButtonStyle;
                var label = GetEntityLabel(entity);
                
                if (GUILayout.Button(label, style))
                {
                    _selectedEntity = entity;
                }
                
                GUI.backgroundColor = originalColor;
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
            var (faction, entityType) = GetEntityInfo(entity);
            return $"{faction} {entityType} #{entity.Id}";
        }
        
        /// <summary>
        /// 获取实体信息：阵营和类型
        /// </summary>
        private (string faction, string entityType) GetEntityInfo(Entity entity)
        {
            var archetype = entity.GetArchetype();
            var components = archetype.Signature.Components.ToArray();
            var typeNames = new HashSet<string>();
            foreach (var comp in components)
            {
                typeNames.Add(comp.Type.Name);
            }
            
            // 判断阵营
            string faction;
            if (typeNames.Contains("Player"))
                faction = "🔵";  // 玩家
            else if (typeNames.Contains("Monster") || typeNames.Contains("Enemy"))
                faction = "🔴";  // 敌人
            else if (typeNames.Contains("NPC"))
                faction = "🟡";  // 中立
            else if (typeNames.Contains("Buff"))
                faction = "🟢";  // Buff
            else if (typeNames.Contains("Modifier"))
                faction = "🟣";  // Modifier
            else
                faction = "⚪";  // 其他
            
            // 判断实体类型
            string entityType;
            if (typeNames.Contains("Player"))
                entityType = "[玩家]";
            else if (typeNames.Contains("Monster"))
                entityType = "[怪物]";
            else if (typeNames.Contains("Summon"))
                entityType = "[召唤物]";
            else if (typeNames.Contains("Bullet"))
                entityType = "[子弹]";
            else if (typeNames.Contains("Buff"))
                entityType = "[Buff]";
            else if (typeNames.Contains("Modifier"))
                entityType = "[修改器]";
            else if (typeNames.Contains("NPC"))
                entityType = "[NPC]";
            else if (typeNames.Contains("Skill"))
                entityType = "[技能]";
            else if (typeNames.Contains("Damage"))
                entityType = "[伤害]";
            else
                entityType = "[实体]";
            
            return (faction, entityType);
        }
        
        /// <summary>
        /// 获取阵营颜色
        /// </summary>
        private Color GetFactionColor(Entity entity)
        {
            var archetype = entity.GetArchetype();
            var components = archetype.Signature.Components.ToArray();
            
            foreach (var comp in components)
            {
                var name = comp.Type.Name;
                if (name == "Player") return BattleDebugStyles.PlayerColor;
                if (name == "Monster" || name == "Enemy") return BattleDebugStyles.EnemyColor;
                if (name == "NPC") return BattleDebugStyles.NeutralColor;
                if (name == "Buff") return BattleDebugStyles.BuffColor;
                if (name == "Modifier") return BattleDebugStyles.ModifierColor;
            }
            
            return BattleDebugStyles.DisabledColor;
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
            var fields = componentType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var properties = componentType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (fields.Length == 0 && properties.Length == 0)
            {
                EditorGUILayout.LabelField("(Tag组件 - 无数据)", BattleDebugStyles.DisabledStyle);
                return;
            }
            
            EditorGUI.indentLevel++;
            
            // 尝试通过反射获取组件实例
            object componentInstance = null;
            try
            {
                // 使用Arch的Get方法获取组件（通过反射调用泛型方法）
                var getMethod = typeof(EntityExtensions).GetMethods()
                    .FirstOrDefault(m => m.Name == "Get" && m.IsGenericMethod && m.GetParameters().Length == 1);
                if (getMethod != null)
                {
                    var genericGet = getMethod.MakeGenericMethod(componentType);
                    componentInstance = genericGet.Invoke(null, new object[] { entity });
                }
            }
            catch
            {
                // 获取失败，使用备选方案
            }
            
            foreach (var field in fields)
            {
                DrawFieldValue(field.Name, field.FieldType, componentInstance != null ? field.GetValue(componentInstance) : null);
            }
            
            foreach (var prop in properties.Where(p => p.CanRead && p.GetIndexParameters().Length == 0))
            {
                try
                {
                    var value = componentInstance != null ? prop.GetValue(componentInstance) : null;
                    DrawFieldValue(prop.Name, prop.PropertyType, value);
                }
                catch
                {
                    EditorGUILayout.LabelField($"{prop.Name}", "(无法读取)", BattleDebugStyles.DisabledStyle);
                }
            }
            
            EditorGUI.indentLevel--;
        }
        
        private void DrawFieldValue(string name, Type type, object value)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(name, GUILayout.Width(120));
            
            if (value == null)
            {
                EditorGUILayout.LabelField("null", BattleDebugStyles.DisabledStyle);
            }
            else
            {
                var valueStr = FormatValue(value, type);
                EditorGUILayout.LabelField(valueStr);
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        private string FormatValue(object value, Type type)
        {
            if (value == null) return "null";
            
            // 特殊类型格式化
            if (type == typeof(Entity))
            {
                var e = (Entity)value;
                return e.IsAlive() ? $"Entity #{e.Id}" : "Entity.Null";
            }
            if (type.Name == "FP")
            {
                return $"{value:F2}";
            }
            if (type == typeof(Vector3))
            {
                var v = (Vector3)value;
                return $"({v.x:F2}, {v.y:F2}, {v.z:F2})";
            }
            if (type.IsEnum)
            {
                return value.ToString();
            }
            
            // 检查是否是List或Array
            if (value is System.Collections.IList list)
            {
                return $"[{list.Count} 项]";
            }
            
            return value.ToString();
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
