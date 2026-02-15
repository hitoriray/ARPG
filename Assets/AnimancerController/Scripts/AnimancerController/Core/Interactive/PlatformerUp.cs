using RayPlayer;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 平台跳跃交互
/// </summary>
public class PlatformerUp : MonoBehaviour
{
    LayerMask playerMask;

    [FormerlySerializedAs("forceHight")] [SerializeField]
    private float forceHeight = 15;

    private void Awake()
    {
        playerMask = LayerMask.GetMask("Player");
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((1 << other.gameObject.layer & playerMask) != 0)
        {
            if (other.TryGetComponent<PlayerController>(out var player))
            {
                player.ReusableData.jumpExternalForce = forceHeight;
                player.MovementStateMachine.ChangeState(player.MovementStateMachine.platformerUpState);
            }
        }
    }
}