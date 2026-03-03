using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "GenericLocomotionConfig", menuName = "Config/Character/Generic Locomotion Config")]
public class GenericLocomotionConfig : ScriptableObject
{
    [Title("Animator")]
    [LabelText("Animator Controller")] public RuntimeAnimatorController animatorController;
    [LabelText("Avatar(Generic)")] public Avatar avatar;
    [LabelText("Enable RootMotion")] public bool applyRootMotion = false;

    [Title("Movement")]
    [LabelText("Walk Speed"), Min(0f)] public float walkSpeed = 3.5f;
    [LabelText("Run Speed"), Min(0f)] public float runSpeed = 6.5f;
    [LabelText("Rotate Speed"), Min(0f)] public float rotateSpeed = 12f;
    [LabelText("Acceleration"), Min(0f)] public float acceleration = 14f;
    [LabelText("Deceleration"), Min(0f)] public float deceleration = 18f;
    [LabelText("Gravity")] public float gravity = -20f;
    [LabelText("Animator Damp"), Min(0f)] public float animatorDampTime = 0.08f;

    [Title("Animator Parameters")]
    [LabelText("Speed Param")] public string speedParam = "Speed";
    [LabelText("MoveX Param")] public string moveXParam = "MoveX";
    [LabelText("MoveY Param")] public string moveYParam = "MoveY";
    [LabelText("IsMoving Param")] public string isMovingParam = "IsMoving";
    [LabelText("IsRun Param")] public string isRunParam = "IsRun";
}
