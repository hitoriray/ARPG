using Sirenix.OdinInspector;
using UnityEngine;

[DisallowMultipleComponent]
public class FootstepSurface : MonoBehaviour
{
    [LabelText("地表类型")] public FootstepSurfaceType SurfaceType = FootstepSurfaceType.Default;
}
