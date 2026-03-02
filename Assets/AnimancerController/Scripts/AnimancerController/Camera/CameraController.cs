using Cinemachine;
using RayPlayer;
using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// 依赖Cinemachine，用于平滑控制相机距离
/// </summary>
public class CameraController : MonoBehaviour
{
    public float defaultDistance;
    [Range(0.5f, 3)] public float minDistance;
    [Range(3, 10)] public float maxDistance;
    private float currentDistance;
    public float sensitivity;
    public float smoothness;

    private CinemachineFramingTransposer virtualCamera;
    private PlayableDirector playableDirector;
    private InputService inputService;

    private void Awake()
    {
        inputService = InputService.Instance;

        var vcam = GetComponent<CinemachineVirtualCamera>();
        if (vcam != null)
        {
            virtualCamera = vcam.GetCinemachineComponent<CinemachineFramingTransposer>();
        }

        playableDirector = GetComponent<PlayableDirector>();
        currentDistance = defaultDistance;
        if (virtualCamera != null)
        {
            virtualCamera.m_CameraDistance = currentDistance;
        }
    }
    
    private void Update()
    {
        GetMouseScroll();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        UpdateCameraDistance();
    }

    private void GetMouseScroll()
    {
        if (inputService == null || inputService.inputMap == null)
        {
            inputService = InputService.Instance;
            if (inputService == null || inputService.inputMap == null) return;
        }

        currentDistance -= inputService.inputMap.Player.Scroll.ReadValue<Vector2>().y * Time.deltaTime * sensitivity;
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);
    }

    private void UpdateCameraDistance()
    {
        if (playableDirector != null)
        {
            if (playableDirector?.state == PlayState.Playing)
            {
                // 如果正在播放，跳过更新 m_CameraDistance
                return;
            }
        }

        if (virtualCamera != null)
        {
            virtualCamera.m_CameraDistance = Mathf.Lerp(virtualCamera.m_CameraDistance, currentDistance, Time.deltaTime * smoothness);
        }
    }
}
