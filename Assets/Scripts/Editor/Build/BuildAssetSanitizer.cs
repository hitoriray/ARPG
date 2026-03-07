using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
#if ENABLE_ADDRESSABLES
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
#endif

namespace EditorTools.Build
{
    /// <summary>
    /// Clears unexpected DontSave flags on assets that must be included in player build.
    /// </summary>
    public sealed class BuildAssetSanitizer : IPreprocessBuildWithReport
    {
        private const long StreamingAssetsWarnThresholdBytes = 1024L * 1024L * 1024L; // 1 GB

        private static readonly string[] TargetAssetPaths =
        {
            "Assets/AnimancerController/Resources/UI.png",
        };

        public int callbackOrder => -1000;

        public void OnPreprocessBuild(BuildReport report)
        {
            bool changed = false;

            foreach (string assetPath in TargetAssetPaths)
            {
                changed |= ClearDontSaveFlags(assetPath);
            }

            changed |= EnsureReadableCollisionMeshesForRecast();
            changed |= EnsureAddressablesBuildWithPlayer();
            WarnStreamingAssetsSize();

            if (changed)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                Debug.Log("[BuildAssetSanitizer] Applied build-time asset/addressables sanitization changes.");
            }
        }

        private static bool ClearDontSaveFlags(string assetPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            if (assets == null || assets.Length == 0)
            {
                return false;
            }

            bool changed = false;
            foreach (Object asset in assets.Where(a => a != null))
            {
                if ((asset.hideFlags & HideFlags.DontSave) == 0) continue;

                asset.hideFlags &= ~HideFlags.DontSave;
                asset.hideFlags &= ~HideFlags.DontSaveInBuild;
                asset.hideFlags &= ~HideFlags.DontSaveInEditor;
                EditorUtility.SetDirty(asset);
                changed = true;
            }

            if (changed)
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            }

            return changed;
        }

        private static bool EnsureReadableCollisionMeshesForRecast()
        {
            const string rootFolder = "Assets/Synty";
            if (!AssetDatabase.IsValidFolder(rootFolder))
            {
                return false;
            }

            bool changed = false;
            string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { rootFolder });

            foreach (string guid in modelGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath))
                {
                    continue;
                }

                if (!assetPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!assetPath.Replace('\\', '/').Contains("/Models/Collision/"))
                {
                    continue;
                }

                ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
                if (importer == null || importer.isReadable)
                {
                    continue;
                }

                importer.isReadable = true;
                importer.SaveAndReimport();
                changed = true;
            }

            if (changed)
            {
                Debug.LogWarning(
                    "[BuildAssetSanitizer] Enabled Read/Write on Synty collision FBX models for Recast MeshCollider scanning.");
            }

            return changed;
        }

#if ENABLE_ADDRESSABLES
        private static bool EnsureAddressablesBuildWithPlayer()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("[BuildAssetSanitizer] Addressables settings not found. Player build may use stale catalog content.");
                return false;
            }

            const AddressableAssetSettings.PlayerBuildOption targetOption =
                AddressableAssetSettings.PlayerBuildOption.BuildWithPlayer;

            if (settings.BuildAddressablesWithPlayerBuild == targetOption)
            {
                return false;
            }

            settings.BuildAddressablesWithPlayerBuild = targetOption;
            EditorUtility.SetDirty(settings);
            Debug.LogWarning("[BuildAssetSanitizer] Addressables set to BuildWithPlayer to avoid stale catalog/content in player builds.");
            return true;
        }
#else
        private static bool EnsureAddressablesBuildWithPlayer() => false;
#endif

        private static void WarnStreamingAssetsSize()
        {
            string streamingAssetsPath = Path.Combine(Application.dataPath, "StreamingAssets");
            if (!Directory.Exists(streamingAssetsPath))
            {
                return;
            }

            long totalBytes = CalculateDirectorySize(streamingAssetsPath);
            if (totalBytes >= StreamingAssetsWarnThresholdBytes)
            {
                Debug.LogWarning(
                    $"[BuildAssetSanitizer] StreamingAssets size is {ToMbString(totalBytes)}. " +
                    "Everything in Assets/StreamingAssets is copied to player build.");
            }

            string llamaPath = Path.Combine(streamingAssetsPath, "LlamaLib-v2.0.4");
            if (Directory.Exists(llamaPath))
            {
                long llamaBytes = CalculateDirectorySize(llamaPath);
                if (llamaBytes > 0)
                {
                    Debug.LogWarning(
                        $"[BuildAssetSanitizer] LlamaLib-v2.0.4 contributes {ToMbString(llamaBytes)} in StreamingAssets. " +
                        "This folder alone can add several GB to the package.");
                }
            }
        }

        private static long CalculateDirectorySize(string directoryPath)
        {
            return Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories)
                .Select(path =>
                {
                    try
                    {
                        return new FileInfo(path).Length;
                    }
                    catch
                    {
                        return 0L;
                    }
                })
                .Sum();
        }

        private static string ToMbString(long bytes)
        {
            return $"{bytes / (1024f * 1024f):F2} MB";
        }
    }
}
