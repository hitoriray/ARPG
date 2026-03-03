using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator), typeof(CharacterController))]
public class GenericPlayerLocomotionController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private GenericLocomotionConfig locomotionConfig;
    [SerializeField] private Transform cameraTransform;

    [Header("Run Mode")]
    [SerializeField] private bool holdShiftToRun = true;
    [SerializeField] private bool allowToggleRun = true;

    [Header("Direction")]
    [SerializeField] private bool useCameraForward = true;

    private Animator animator;
    private CharacterController characterController;
    private InputService inputService;

    private float currentSpeed;
    private float verticalVelocity;
    private bool runToggle;
    private bool toggleRunRegistered;

    private int speedHash = -1;
    private int moveXHash = -1;
    private int moveYHash = -1;
    private int isMovingHash = -1;
    private int isRunHash = -1;

    public void Initialize(GenericLocomotionConfig config, Transform cam = null)
    {
        locomotionConfig = config;
        if (cam != null)
            cameraTransform = cam;

        ApplyConfigToAnimator();
        CacheAnimatorHashes();
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();

        ApplyConfigToAnimator();
        CacheAnimatorHashes();
    }

    private void OnEnable()
    {
        TryBindInputService();
    }

    private void OnDisable()
    {
        UnregisterToggleRun();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            animator = GetComponent<Animator>();
            ApplyConfigToAnimator();
            CacheAnimatorHashes();
        }
    }

    private void Update()
    {
        if (locomotionConfig == null || animator == null || characterController == null)
            return;

        TryBindInputService();
        TryBindCamera();

        Vector2 moveInput = GetMoveInput();
        bool hasMoveInput = moveInput.sqrMagnitude > 0.0001f;
        bool isRun = hasMoveInput && GetRunState();

        float targetSpeed = hasMoveInput
            ? (isRun ? locomotionConfig.runSpeed : locomotionConfig.walkSpeed)
            : 0f;

        float speedChange = targetSpeed >= currentSpeed ? locomotionConfig.acceleration : locomotionConfig.deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, speedChange * Time.deltaTime);

        Vector3 worldMoveDir = GetWorldMoveDirection(moveInput);
        UpdateRotation(worldMoveDir);
        UpdateHorizontalMovement(worldMoveDir);
        UpdateVerticalMovement();
        UpdateAnimator(worldMoveDir, hasMoveInput, isRun);
    }

    private void OnAnimatorMove()
    {
        if (locomotionConfig == null || !locomotionConfig.applyRootMotion || animator == null || characterController == null)
            return;

        Vector3 deltaPos = animator.deltaPosition;
        deltaPos.y = 0f;
        if (deltaPos != Vector3.zero)
            characterController.Move(deltaPos);

        if (animator.deltaRotation != Quaternion.identity)
            transform.rotation = animator.deltaRotation * transform.rotation;
    }

    private void ApplyConfigToAnimator()
    {
        if (animator == null || locomotionConfig == null)
            return;

        if (locomotionConfig.animatorController != null)
            animator.runtimeAnimatorController = locomotionConfig.animatorController;

        if (locomotionConfig.avatar != null)
            animator.avatar = locomotionConfig.avatar;

        animator.applyRootMotion = locomotionConfig.applyRootMotion;
        // PlayerModel 在运行时替换后，强制重绑骨骼，避免场景内出现 T-Pose。
        if (Application.isPlaying)
        {
            animator.Rebind();
            animator.Update(0f);
        }
    }

    private void CacheAnimatorHashes()
    {
        speedHash = ToHash(locomotionConfig != null ? locomotionConfig.speedParam : null);
        moveXHash = ToHash(locomotionConfig != null ? locomotionConfig.moveXParam : null);
        moveYHash = ToHash(locomotionConfig != null ? locomotionConfig.moveYParam : null);
        isMovingHash = ToHash(locomotionConfig != null ? locomotionConfig.isMovingParam : null);
        isRunHash = ToHash(locomotionConfig != null ? locomotionConfig.isRunParam : null);
    }

    private static int ToHash(string parameterName)
    {
        return string.IsNullOrWhiteSpace(parameterName) ? -1 : Animator.StringToHash(parameterName);
    }

    private void TryBindInputService()
    {
        if (inputService != null)
            return;

        inputService = InputService.Instance;
        RegisterToggleRun();
    }

    private void RegisterToggleRun()
    {
        if (toggleRunRegistered || inputService == null || inputService.inputMap == null || !allowToggleRun)
            return;

        inputService.inputMap.Player.ToggleRun.started += OnToggleRun;
        toggleRunRegistered = true;
    }

    private void UnregisterToggleRun()
    {
        if (!toggleRunRegistered || inputService == null || inputService.inputMap == null)
            return;

        inputService.inputMap.Player.ToggleRun.started -= OnToggleRun;
        toggleRunRegistered = false;
    }

    private void OnToggleRun(InputAction.CallbackContext _)
    {
        runToggle = !runToggle;
    }

    private void TryBindCamera()
    {
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    private Vector2 GetMoveInput()
    {
        if (inputService != null)
            return Vector2.ClampMagnitude(inputService.Move, 1f);

        return Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")), 1f);
    }

    private bool GetRunState()
    {
        bool holdRun = inputService != null ? inputService.Shift : Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
        return (holdShiftToRun && holdRun) || (allowToggleRun && runToggle);
    }

    private Vector3 GetWorldMoveDirection(Vector2 moveInput)
    {
        if (moveInput.sqrMagnitude <= 0.0001f)
            return Vector3.zero;

        if (!useCameraForward || cameraTransform == null)
            return new Vector3(moveInput.x, 0f, moveInput.y).normalized;

        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        return (right * moveInput.x + forward * moveInput.y).normalized;
    }

    private void UpdateRotation(Vector3 worldMoveDir)
    {
        if (worldMoveDir.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(worldMoveDir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, locomotionConfig.rotateSpeed * Time.deltaTime);
    }

    private void UpdateHorizontalMovement(Vector3 worldMoveDir)
    {
        if (locomotionConfig.applyRootMotion || currentSpeed <= 0f || worldMoveDir.sqrMagnitude <= 0.0001f)
            return;

        characterController.Move(worldMoveDir * (currentSpeed * Time.deltaTime));
    }

    private void UpdateVerticalMovement()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        else
            verticalVelocity += locomotionConfig.gravity * Time.deltaTime;

        characterController.Move(Vector3.up * (verticalVelocity * Time.deltaTime));
    }

    private void UpdateAnimator(Vector3 worldMoveDir, bool hasMoveInput, bool isRun)
    {
        float damp = locomotionConfig.animatorDampTime;
        float normalizedSpeed = locomotionConfig.runSpeed > 0.01f ? Mathf.Clamp01(currentSpeed / locomotionConfig.runSpeed) : 0f;
        Vector3 localMoveDir = transform.InverseTransformDirection(worldMoveDir);

        if (speedHash != -1)
            animator.SetFloat(speedHash, normalizedSpeed, damp, Time.deltaTime);
        if (moveXHash != -1)
            animator.SetFloat(moveXHash, localMoveDir.x, damp, Time.deltaTime);
        if (moveYHash != -1)
            animator.SetFloat(moveYHash, localMoveDir.z, damp, Time.deltaTime);
        if (isMovingHash != -1)
            animator.SetBool(isMovingHash, hasMoveInput);
        if (isRunHash != -1)
            animator.SetBool(isRunHash, isRun);
    }
}
