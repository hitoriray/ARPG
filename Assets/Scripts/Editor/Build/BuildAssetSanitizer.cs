using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace EditorTools.Build
{
    /// <summary>
    /// Clears unexpected DontSave flags on assets that must be included in player build.
    /// </summary>
    public sealed class BuildAssetSanitizer : IPreprocessBuildWithReport
    {
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

            if (changed)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                Debug.Log("[BuildAssetSanitizer] Cleared DontSave hideFlags on build assets.");
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
    }
}
