using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using Battle.ECS;
using UnityEditor;
using UnityEngine;

namespace Editor.BattleDebug.Pages
{
    /// <summary>
    /// 内存页 - Archetype内存分析
    /// </summary>
    public class MemoryPage : IBattleDebugPage
    {
        public string Title => "内存";
        public string Icon => "[M]";
        
        private Vector2 _scrollPosition;
        private string _nameFilter = "";
        private bool _nameInclude = true;
        
        private enum SortColumn
        {
            Name,
            ChunkSize,
            ChunkCount,
            EntityCount,
            EntityCapacity,
            Memory
        }
        
        private readonly float[] _columnWidths = { 250, 70, 70, 70, 70, 80 };
        private SortColumn _sortColumn = SortColumn.Memory;
        private bool _sortAscending = false;
        
        public void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("仅在播放模式下可用", MessageType.Info);
                return;
            }
            
            var runner = BattleEcsRunner.Instance;
            if (runner?.Context?.World == null)
            {
                EditorGUILayout.HelpBox("战斗上下文不可用", MessageType.Warning);
                return;
            }
            
            var world = runner.Context.World;
            
            // ============================================
            // 过滤面板
            // ============================================
            DrawFilterPanel();
            
            // ============================================
            // 表头
            // ============================================
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            DrawSortableHeader("Archetype (组件列表)", SortColumn.Name);
            DrawSortableHeader("Chunk大小", SortColumn.ChunkSize);
            DrawSortableHeader("Chunk数", SortColumn.ChunkCount);
            DrawSortableHeader("实体数", SortColumn.EntityCount);
            DrawSortableHeader("容量", SortColumn.EntityCapacity);
            DrawSortableHeader("内存占用", SortColumn.Memory);
            EditorGUILayout.EndHorizontal();
            
            // ============================================
            // 表格内容
            // ============================================
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            
            long totalMemory = 0;
            int totalChunks = 0;
            int totalEntities = 0;
            int totalCapacity = 0;
            
            var archetypes = world.Archetypes;
            if (archetypes != null)
            {
                var filtered = FilterArchetypes(world);
                var sorted = SortArchetypes(filtered);
                
                foreach (var archetype in sorted)
                {
                    var chunkSize = archetype.ChunkSize;
                    var chunkCount = archetype.ChunkCount;
                    var entityCount = archetype.EntityCount;
                    var entityCapacity = archetype.EntityCapacity;
                    var memory = (long)chunkCount * chunkSize;
                    
                    totalChunks += chunkCount;
                    totalEntities += entityCount;
                    totalCapacity += entityCapacity;
                    totalMemory += memory;
                    
                    DrawArchetypeRow(archetype, chunkSize, chunkCount, entityCount, entityCapacity, memory);
                }
            }
            
            EditorGUILayout.EndScrollView();
            
            // ============================================
            // 总计
            // ============================================
            BattleDebugStyles.DrawSeparator();
            
            EditorGUILayout.BeginHorizontal();
            BattleDebugStyles.DrawStatCard("Archetype数", $"{world.Archetypes?.Count ?? 0}");
            BattleDebugStyles.DrawStatCard("Chunk总数", $"{totalChunks}");
            BattleDebugStyles.DrawStatCard("实体数/容量", $"{totalEntities}/{totalCapacity}");
            BattleDebugStyles.DrawStatCard("总内存", BattleDebugStyles.FormatBytes(totalMemory), 
                totalMemory > 10 * 1024 * 1024 ? BattleDebugStyles.WarningColor : BattleDebugStyles.SuccessColor);
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawFilterPanel()
        {
            EditorGUILayout.BeginHorizontal();
            _nameInclude = EditorGUILayout.Popup(_nameInclude ? 0 : 1, new[] { "包含", "排除" }, GUILayout.Width(50)) == 0;
            EditorGUILayout.LabelField("组件名:", GUILayout.Width(50));
            _nameFilter = EditorGUILayout.TextField(_nameFilter);
            if (GUILayout.Button("清除", GUILayout.Width(50)))
            {
                _nameFilter = "";
                GUI.FocusControl(null);
            }
            EditorGUILayout.EndHorizontal();
        }
        
        private void DrawSortableHeader(string label, SortColumn column)
        {
            var style = new GUIStyle(EditorStyles.toolbarButton);
            string displayLabel = label;
            
            if (_sortColumn == column)
            {
                displayLabel += _sortAscending ? " ▲" : " ▼";
                style.fontStyle = FontStyle.Bold;
            }
            
            if (GUILayout.Button(displayLabel, style, GUILayout.Width(_columnWidths[(int)column])))
            {
                if (_sortColumn == column)
                    _sortAscending = !_sortAscending;
                else
                {
                    _sortColumn = column;
                    _sortAscending = false;
                }
            }
        }
        
        private void DrawArchetypeRow(Archetype archetype, int chunkSize, int chunkCount, 
            int entityCount, int entityCapacity, long memory)
        {
            var textColor = entityCount > 0 ? BattleDebugStyles.SuccessColor : BattleDebugStyles.DisabledColor;
            var wrapStyle = new GUIStyle(EditorStyles.label) 
            { 
                wordWrap = true, 
                normal = { textColor = textColor } 
            };
            var centerStyle = new GUIStyle(EditorStyles.label) 
            { 
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = textColor }
            };
            
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField(GetArchetypeName(archetype), wrapStyle, GUILayout.Width(_columnWidths[0]));
            EditorGUILayout.LabelField(BattleDebugStyles.FormatBytes(chunkSize), centerStyle, GUILayout.Width(_columnWidths[1]));
            EditorGUILayout.LabelField(chunkCount.ToString(), centerStyle, GUILayout.Width(_columnWidths[2]));
            EditorGUILayout.LabelField(entityCount.ToString(), centerStyle, GUILayout.Width(_columnWidths[3]));
            EditorGUILayout.LabelField(entityCapacity.ToString(), centerStyle, GUILayout.Width(_columnWidths[4]));
            EditorGUILayout.LabelField(BattleDebugStyles.FormatBytes(memory), centerStyle, GUILayout.Width(_columnWidths[5]));
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        
        private string GetArchetypeName(Archetype archetype)
        {
            var components = archetype.Signature.Components;
            var names = components.ToArray().Select(c => c.Type.Name).ToArray();
            return string.Join(", ", names);
        }
        
        private List<Archetype> FilterArchetypes(World world)
        {
            var result = new List<Archetype>();
            var archetypeArray = world.Archetypes.Items.AsSpan().ToArray();
            
            foreach (var archetype in archetypeArray)
            {
                if (string.IsNullOrEmpty(_nameFilter))
                {
                    result.Add(archetype);
                    continue;
                }
                
                var name = GetArchetypeName(archetype);
                bool contains = name.IndexOf(_nameFilter, System.StringComparison.OrdinalIgnoreCase) >= 0;
                
                if (_nameInclude && contains)
                    result.Add(archetype);
                else if (!_nameInclude && !contains)
                    result.Add(archetype);
            }
            
            return result;
        }
        
        private List<Archetype> SortArchetypes(List<Archetype> archetypes)
        {
            IOrderedEnumerable<Archetype> sorted = _sortColumn switch
            {
                SortColumn.Name => archetypes.OrderBy(GetArchetypeName),
                SortColumn.ChunkSize => archetypes.OrderBy(a => a.ChunkSize),
                SortColumn.ChunkCount => archetypes.OrderBy(a => a.ChunkCount),
                SortColumn.EntityCount => archetypes.OrderBy(a => a.EntityCount),
                SortColumn.EntityCapacity => archetypes.OrderBy(a => a.EntityCapacity),
                SortColumn.Memory => archetypes.OrderBy(a => (long)a.ChunkCount * a.ChunkSize),
                _ => archetypes.OrderBy(a => a.EntityCount)
            };
            
            return _sortAscending ? sorted.ToList() : sorted.Reverse().ToList();
        }
    }
}
