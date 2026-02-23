using UnityEngine;
using Sirenix.OdinInspector;
[CreateAssetMenu(menuName = "Config/PlayerSO")]
public class PlayerSO : ScriptableObject
{
    [field: SerializeField, LabelText("玩家移动配置"), InspectorName("玩家移动配置")]
    public PlayerMovementData playerMovementData { get; private set; }

    [field: SerializeField, LabelText("玩家参数配置"), InspectorName("玩家参数配置")]
    public PlayerParameterData playerParameterData { get; private set; }
}
