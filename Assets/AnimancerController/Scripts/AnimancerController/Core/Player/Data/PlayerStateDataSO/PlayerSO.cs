using UnityEngine;
[CreateAssetMenu(menuName = "Config/PlayerSO")]
public class PlayerSO : ScriptableObject
{
    [field:SerializeField]  public PlayerMovementData playerMovementData { get; private set; }
    [field :SerializeField] public PlayerParameterData playerParameterData { get; private set; }
}
