using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    /// <summary>
    /// 一键替换 Shader 工具
    /// 菜单：Tools → Shader Replace Tool
    /// </summary>
    public class ShaderReplaceTool : EditorWindow
    {
        // ── 用户配置 ──────────────────────────────────────────────
        private Shader   _targetShader;          // 替换成哪个 Shader
        private string   _folderPath = "Assets"; // 搜索范围
        private bool     _backupMaterials = true; // 替换前备份材质

        // ── 过滤选项 ──────────────────────────────────────────────
        private bool     _filterBySourceShader;
        private Shader   _sourceShader;          // 只替换来自此 Shader 的材质

        // ── 属性映射（将旧 Shader 的属性名映射到新 Shader） ───────
        private bool     _showPropMapping;
        private List<PropMap> _propMaps = new();

        // ── 预览 ──────────────────────────────────────────────────
        private List<Material> _preview = new();
        private Vector2        _scrollPos;
        private bool           _previewDone;

        // ── 常量 ──────────────────────────────────────────────────
        private const string BackupSuffix = "_ShaderBackup";

        [Serializable]
        private class PropMap
        {
            public string oldName = "";
            public string newName = "";
        }

        // ──────────────────────────────────────────────────────────

        [MenuItem("Tools/Shader Replace Tool")]
        public static void Open()
        {
            var win = GetWindow<ShaderReplaceTool>("Shader Replace Tool");
            win.minSize = new Vector2(480, 520);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            GUILayout.Label("── 目标 Shader ──────────────────────────────", EditorStyles.boldLabel);

            _targetShader = (Shader)EditorGUILayout.ObjectField(
                "替换成", _targetShader, typeof(Shader), false);

            EditorGUILayout.Space(4);
            GUILayout.Label("── 搜索范围 ─────────────────────────────────", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                _folderPath = EditorGUILayout.TextField("文件夹路径", _folderPath);
                if (GUILayout.Button("Browse", GUILayout.Width(60)))
                {
                    string picked = EditorUtility.OpenFolderPanel("选择文件夹", _folderPath, "");
                    if (!string.IsNullOrEmpty(picked))
                    {
                        // 转换成 Assets/ 相对路径
                        if (picked.StartsWith(Application.dataPath))
                            _folderPath = "Assets" + picked.Substring(Application.dataPath.Length);
                        else
                            _folderPath = picked;
                    }
                }
            }

            EditorGUILayout.Space(4);
            GUILayout.Label("── 过滤 ─────────────────────────────────────", EditorStyles.boldLabel);

            _filterBySourceShader = EditorGUILayout.Toggle("只替换指定来源 Shader", _filterBySourceShader);
            if (_filterBySourceShader)
            {
                _sourceShader = (Shader)EditorGUILayout.ObjectField(
                    "来源 Shader", _sourceShader, typeof(Shader), false);
            }

            EditorGUILayout.Space(4);
            GUILayout.Label("── 属性映射（可选） ──────────────────────────", EditorStyles.boldLabel);
            _showPropMapping = EditorGUILayout.Foldout(_showPropMapping, "属性名重映射");
            if (_showPropMapping)
            {
                EditorGUI.indentLevel++;
                for (int i = 0; i < _propMaps.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        _propMaps[i].oldName = EditorGUILayout.TextField("旧属性名", _propMaps[i].oldName);
                        _propMaps[i].newName = EditorGUILayout.TextField("→ 新属性名", _propMaps[i].newName);
                        if (GUILayout.Button("✕", GUILayout.Width(22)))
                        {
                            _propMaps.RemoveAt(i);
                            break;
                        }
                    }
                }
                if (GUILayout.Button("+ 添加映射"))
                    _propMaps.Add(new PropMap());
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(6);
            _backupMaterials = EditorGUILayout.Toggle("替换前创建备份材质", _backupMaterials);

            EditorGUILayout.Space(8);

            // 操作按钮
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("🔍 预览（不修改）", GUILayout.Height(32)))
                    Preview();

                GUI.enabled = _targetShader != null;
                if (GUILayout.Button("⚡ 一键替换", GUILayout.Height(32)))
                {
                    if (EditorUtility.DisplayDialog("确认替换",
                            $"将把 {_preview.Count} 个材质的 Shader 替换为 [{_targetShader?.name}]。\n此操作不可撤销（除非已勾选备份）。",
                            "替换", "取消"))
                        DoReplace();
                }
                GUI.enabled = true;
            }

            // 预览结果列表
            if (_previewDone)
            {
                EditorGUILayout.Space(6);
                GUILayout.Label($"找到 {_preview.Count} 个材质：", EditorStyles.boldLabel);
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos,
                    GUILayout.MaxHeight(200));
                foreach (var mat in _preview)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.ObjectField(mat, typeof(Material), false);
                        EditorGUILayout.LabelField(mat.shader?.name ?? "?",
                            GUILayout.Width(180));
                    }
                }
                EditorGUILayout.EndScrollView();
            }
        }

        // ── 预览：收集满足过滤条件的材质 ───────────────────────────
        private void Preview()
        {
            _preview.Clear();
            _previewDone = false;

            var guids = AssetDatabase.FindAssets("t:Material", new[] { _folderPath });
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;
                if (_filterBySourceShader && _sourceShader != null && mat.shader != _sourceShader)
                    continue;
                _preview.Add(mat);
            }

            _previewDone = true;
            Repaint();
        }

        // ── 执行替换 ────────────────────────────────────────────────
        private void DoReplace()
        {
            if (_targetShader == null)
            {
                EditorUtility.DisplayDialog("错误", "请先指定目标 Shader。", "OK");
                return;
            }

            if (!_previewDone || _preview.Count == 0)
                Preview();

            int replaced = 0;
            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (var mat in _preview)
                {
                    // 备份
                    if (_backupMaterials)
                    {
                        string srcPath = AssetDatabase.GetAssetPath(mat);
                        string dir     = Path.GetDirectoryName(srcPath);
                        string name    = Path.GetFileNameWithoutExtension(srcPath);
                        string bakPath = $"{dir}/{name}{BackupSuffix}.mat";
                        // 仅当备份不存在时创建
                        if (!File.Exists(bakPath))
                            AssetDatabase.CopyAsset(srcPath, bakPath);
                    }

                    // 收集旧属性值（只收集 Texture 和 Color，Float 会由新 Shader 保留）
                    var textures  = new Dictionary<string, Texture>();
                    var colors    = new Dictionary<string, Color>();
                    var floats    = new Dictionary<string, float>();

                    var oldShader = mat.shader;
                    int propCount = ShaderUtil.GetPropertyCount(oldShader);
                    for (int i = 0; i < propCount; i++)
                    {
                        string propName = ShaderUtil.GetPropertyName(oldShader, i);
                        var propType    = ShaderUtil.GetPropertyType(oldShader, i);
                        switch (propType)
                        {
                            case ShaderUtil.ShaderPropertyType.TexEnv:
                                textures[propName] = mat.GetTexture(propName);
                                break;
                            case ShaderUtil.ShaderPropertyType.Color:
                                colors[propName] = mat.GetColor(propName);
                                break;
                            case ShaderUtil.ShaderPropertyType.Float:
                            case ShaderUtil.ShaderPropertyType.Range:
                                floats[propName] = mat.GetFloat(propName);
                                break;
                        }
                    }

                    // 替换 Shader
                    mat.shader = _targetShader;

                    // 恢复属性（名字相同直接恢复，有映射的按映射）
                    int newPropCount = ShaderUtil.GetPropertyCount(_targetShader);
                    for (int i = 0; i < newPropCount; i++)
                    {
                        string newName = ShaderUtil.GetPropertyName(_targetShader, i);
                        string srcName = GetSourcePropName(newName);
                        var    type    = ShaderUtil.GetPropertyType(_targetShader, i);
                        switch (type)
                        {
                            case ShaderUtil.ShaderPropertyType.TexEnv:
                                if (textures.TryGetValue(srcName, out var tex) && tex != null)
                                    mat.SetTexture(newName, tex);
                                break;
                            case ShaderUtil.ShaderPropertyType.Color:
                                if (colors.TryGetValue(srcName, out var col))
                                    mat.SetColor(newName, col);
                                break;
                            case ShaderUtil.ShaderPropertyType.Float:
                            case ShaderUtil.ShaderPropertyType.Range:
                                if (floats.TryGetValue(srcName, out var flt))
                                    mat.SetFloat(newName, flt);
                                break;
                        }
                    }

                    EditorUtility.SetDirty(mat);
                    replaced++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog("完成",
                $"已将 {replaced} 个材质的 Shader 替换为 [{_targetShader.name}]。",
                "OK");
        }

        /// <summary>根据属性映射表，将新 Shader 属性名反查到来源属性名</summary>
        private string GetSourcePropName(string newName)
        {
            foreach (var map in _propMaps)
            {
                if (map.newName == newName && !string.IsNullOrEmpty(map.oldName))
                    return map.oldName;
            }
            return newName; // 无映射：直接同名
        }
    }
}
