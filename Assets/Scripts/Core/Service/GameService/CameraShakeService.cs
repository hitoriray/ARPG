using Cinemachine;
using UnityEngine;

/// <summary>
/// 镜头震动服务。
/// 在场景中任意 GameObject 上挂载此组件即可自动初始化；
/// 运行时通过 CameraShakeService.Shake(force) 在任何地方触发震动。
/// 需要在 CinemachineBrain 所在的 Camera 或附近的 GameObject 上挂一个
/// CinemachineImpulseListener（非本脚本放置 GameObject）。
/// </summary>
public class CameraShakeService : MonoBehaviour
{
    public static CameraShakeService Instance { get; private set; }

    [SerializeField] private CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // 自动补挂 ImpulseSource
        if (impulseSource == null)
            impulseSource = GetComponent<CinemachineImpulseSource>();
        if (impulseSource == null)
            impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// 触发一次镜头震动。
    /// </summary>
    /// <param name="force">震动力度，对应 ImpulseSource 的 GenerateImpulse 参数。</param>
    public static void Shake(float force)
    {
        if (force <= 0f) return;
        if (Instance == null || Instance.impulseSource == null) return;
        Instance.impulseSource.GenerateImpulse(force);
    }

    /// <summary>
    /// 在指定世界位置触发震动（距离衰减由 ImpulseSource 配置控制）。
    /// </summary>
    public static void ShakeAt(Vector3 worldPosition, float force)
    {
        if (force <= 0f) return;
        if (Instance == null || Instance.impulseSource == null) return;
        Instance.impulseSource.GenerateImpulseAt(worldPosition, Vector3.one * force);
    }
}
