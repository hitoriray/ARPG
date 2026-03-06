using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Manager
{
    /// <summary>
    /// 放置在场景中的触发体，用于在玩家进入/离开时自动加载/卸载 Addressables 子场景。
    /// 
    /// 使用步骤：
    ///   1. 新建 Empty GameObject，添加 Collider（勾选 Is Trigger），调整大小为加载范围。
    ///   2. 挂上此脚本，填写 sceneAddressKey（Addressables 中场景的 Address 字符串）。
    ///   3. 可选：填写 preloadRadius 实现提前预加载（默认与触发器范围一致）。
    ///   4. 场景需要提前在 Addressables Groups 中注册并设置好 Address。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SceneLoadTrigger : MonoBehaviour
    {
        [Header("Addressables 目标")]
        [Tooltip("在 Addressables Groups 中配置的场景 Address（如 Scene_Forest）")]
        [SerializeField] private string sceneAddressKey;

        [Header("加载策略")]
        [Tooltip("进入触发器时加载场景（Additive）")]
        [SerializeField] private bool loadOnEnter = true;
        [Tooltip("离开触发器时卸载场景")]
        [SerializeField] private bool unloadOnExit = true;
        [Tooltip("检测玩家的 Tag")]
        [SerializeField] private string playerTag = "Player";

        [Header("调试")]
        [SerializeField] private Color gizmoColor = new Color(0.2f, 0.8f, 0.4f, 0.25f);

        // ──────────────────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            if (!loadOnEnter) return;

            var loader = SubSceneLoader.Instance;
            if (loader == null)
            {
                Debug.LogWarning("[SceneLoadTrigger] 未找到 SubSceneLoader，请确保场景中存在该单例。");
                return;
            }

            if (!loader.IsLoaded(sceneAddressKey))
                loader.LoadSceneAsync(sceneAddressKey).Forget();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            if (!unloadOnExit) return;

            var loader = SubSceneLoader.Instance;
            if (loader == null) return;

            if (loader.IsLoaded(sceneAddressKey))
                loader.UnloadSceneAsync(sceneAddressKey).Forget();
        }

        // ── 编辑器可视化 ────────────────────────────────────────────
        private void OnDrawGizmos()
        {
            var col = GetComponent<Collider>();
            if (col == null) return;

            Gizmos.color = gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;

            if (col is BoxCollider box)
            {
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (col is SphereCollider sphere)
            {
                Gizmos.DrawSphere(sphere.center, sphere.radius);
                Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
                Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            }

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * 1.5f,
                $"[Load] {sceneAddressKey}",
                new GUIStyle { normal = { textColor = Color.green }, fontSize = 11 });
#endif
        }
    }
}
