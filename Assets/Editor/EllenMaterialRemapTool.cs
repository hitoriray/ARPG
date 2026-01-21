using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    public static class EllenMaterialRemapTool
    {
        private const string FbxFolder = "Assets/Models/绝区零/艾莲/fbx";
        private const string MatFolder = "Assets/Models/绝区零/艾莲/mat";

        private static readonly Dictionary<string, string> RemapTable = new()
        {
            { "Material #25", "Mat25.mat" },
            { "Material #26", "Mat26.mat" },
            { "Material #27", "Mat27.mat" },
            { "Material #28", "Mat28.mat" },
        };

        [MenuItem("Tools/Material Remap/Remap Ellen FBX Materials")]
        private static void RemapEllenMaterials()
        {
            if (!AssetDatabase.IsValidFolder(FbxFolder))
            {
                Debug.LogError($"FBX folder not found: {FbxFolder}");
                return;
            }

            var matAssets = new Dictionary<string, Material>();
            foreach (var kvp in RemapTable)
            {
                var matPath = Path.Combine(MatFolder, kvp.Value).Replace("\\", "/");
                var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (mat == null)
                {
                    Debug.LogError($"Material not found: {matPath}");
                    return;
                }
                matAssets[kvp.Key] = mat;
            }

            var guids = AssetDatabase.FindAssets("t:Model", new[] { FbxFolder });
            int updated = 0;
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                    continue;

                bool changed = false;
                var externalObjects = importer.GetExternalObjectMap();

                foreach (var kvp in matAssets)
                {
                    var id = new AssetImporter.SourceAssetIdentifier(typeof(Material), kvp.Key);
                    if (externalObjects.TryGetValue(id, out var existing) && existing == kvp.Value)
                        continue;
                    importer.AddRemap(id, kvp.Value);
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                    updated++;
                }
            }

            Debug.Log($"Remap completed. Updated FBX count: {updated}");
        }
    }
}
