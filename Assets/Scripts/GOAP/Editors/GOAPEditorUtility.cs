#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

namespace GOAP.Editor
{
    public static class GOAPEditorUtility
    {
        public static GOAPAgent agent;
        public static GOAPGlobalManager GlobalManager { get; private set; }

        [InitializeOnLoadMethod]
        public static void Init()
        {
            TryGetGlobalManager();
            EditorSceneManager.sceneOpened += OnEditorSceneManagerSceneOpened;
        }

        private static void OnEditorSceneManagerSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            GetGlobalManager();
        }

        private static void TryGetGlobalManager()
        {
            if (GlobalManager == null)
                GetGlobalManager();
        }

        private static void GetGlobalManager()
        {
            GlobalManager = GameObject.FindObjectOfType<GOAPGlobalManager>();
        }
    }
}
#endif