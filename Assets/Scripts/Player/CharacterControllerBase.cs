using System;
using Animancer;
using Config;
using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(Animator), typeof(CharacterController), typeof(AnimancerComponent))]
public class CharacterControllerBase : MonoBehaviour
{
    public CharacterController controller { get; private set; }
    public Animator animator { get; private set; }
    public AnimancerComponent animancer { get; private set; }

    [Header("重力配置")]
    [LabelText("重力"), SerializeField] public float gravity = -12f;
    [LabelText("速度限制"), SerializeField] public Vector2 velocityLimit = new Vector2(-20f, 60f);
    [LabelText("Ground层级"), SerializeField] public LayerMask whatIsGround;
    [LabelText("Ground检测半径"), SerializeField] private float groundDetectionRadius = 0.2f;
    [LabelText("Ground检测偏移"), SerializeField] private float groundDetectionOffset = -0.06f;

    [Header("角色配置")]
    [SerializeField] protected CharacterConfig characterConfig;
    [SerializeField] public PlayerSO playerSO;

    // ── 快捷属性（供子类和状态机读取）──
    public CharacterConfig CharacterConfig => characterConfig;
    public PlayerSO PlayerSO => playerSO;
    public float WalkSpeed   => characterConfig != null ? characterConfig.WalkSpeed   : 0f;
    public float RunSpeed    => characterConfig != null ? characterConfig.RunSpeed    : 0f;
    public float RotateSpeed => characterConfig != null ? characterConfig.RotateSpeed : 8f;
    
    private Vector3 detectionOrigin;
    public BindableProperty<bool> isOnGround { get; private set; } = new BindableProperty<bool>();
    
    public float verticalSpeed { get; set; }
    private Vector3 verticalVelocity;
    
    private Vector3 horizontalVelocityInAir;
    private Vector3 animationVelocity;
    public Vector3 AnimationVelocity => animationVelocity;

    private Vector3 moveDir;
    public Vector3 animatorDeltaPositionOffset { get; set; }

    public bool applyFullRootMotion { get; set; }
    [LabelText("移速倍率"), Range(0.1f, 10f)] public float moveSpeedMultiplier = 1f;
    public bool disableRootMotion { get; set; }
    public bool ignoreRootMotionY { get; set; }
    public bool ignoreRotationRootMotion { get; set; }
    public bool disableGravity { get; set; }

    public enum RootMotionMode
    {
        Default,
        Suppressed,
        Custom,
    }

    private RootMotionMode rootMotionMode = RootMotionMode.Default;
    private Action<Vector3, Quaternion> rootMotionHandler;
    
    private bool initialized;

    protected virtual void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
        animancer = GetComponent<AnimancerComponent>();
    }

    protected virtual void Start()
    {
        if (!initialized)
            Init(characterConfig);
    }

    public virtual void Init(CharacterConfig config)
    {
        if (config == null)
        {
            return;
        }
        
        initialized = true;
        
        characterConfig = config;
        if (playerSO == null && characterConfig != null)
            playerSO = characterConfig.PlayerSO;
        
        if (characterConfig != null)
        {
            ApplyControllerProfile(characterConfig.ControllerProfile);
            if (characterConfig.Avatar != null)
                animator.avatar = characterConfig.Avatar;
        }
    }

    protected virtual void Update()
    {
        CheckOnGround();
        CharacterGravity();
        CharacterVerticalVelocity();
        ResetHorizontalVelocity();
    }

    public void SetRootMotionMode(RootMotionMode mode, Action<Vector3, Quaternion> handler = null)
    {
        rootMotionMode = mode;
        rootMotionHandler = handler;
    }

    private bool CheckOnGround()
    {
        detectionOrigin = GetGroundDetectionOrigin();
        var isHit = Physics.CheckSphere(detectionOrigin, groundDetectionRadius, whatIsGround,
            QueryTriggerInteraction.Ignore);
        isOnGround.Value = isHit && verticalSpeed < 0f;
        return isOnGround.Value;
    }

    private void CharacterGravity()
    {
        if (disableGravity)
            return;
        
        if (isOnGround.Value)
        {
            verticalSpeed = -2f;
        }
        else
        {
            verticalSpeed += gravity * Time.deltaTime;
            verticalSpeed = Mathf.Clamp(verticalSpeed, velocityLimit.x, velocityLimit.y);
        }
        verticalVelocity = new Vector3(0f, verticalSpeed, 0f);
    }
    
    private void CharacterVerticalVelocity()
    {
        if (disableGravity)
            verticalVelocity = Vector3.zero;

        if (controller.enabled)
            controller.Move((verticalVelocity + horizontalVelocityInAir) * Time.deltaTime);
    }

    private void ResetHorizontalVelocity()
    {
        if (isOnGround.Value && horizontalVelocityInAir != Vector3.zero)
            horizontalVelocityInAir = Vector3.zero;
    }

    /// <summary>
    /// 在播放动画时会调用此方法,没有动画则不调用
    /// </summary>
    protected virtual void OnAnimatorMove()
    {
        if (rootMotionMode == RootMotionMode.Suppressed)
            return;

        if (rootMotionMode == RootMotionMode.Custom)
        {
            if (rootMotionHandler != null)
            {
                Vector3 deltaPos = animator.deltaPosition + animatorDeltaPositionOffset;
                if (ignoreRootMotionY)
                    deltaPos.y = 0f;
                Quaternion deltaRot = ignoreRotationRootMotion ? Quaternion.identity : animator.deltaRotation;
                rootMotionHandler.Invoke(deltaPos * moveSpeedMultiplier, deltaRot);
            }
            return;
        }

        if (disableRootMotion)
            return;

        if (applyFullRootMotion)
        {
            animator.ApplyBuiltinRootMotion();
            return;
        }

        Vector3 animationMovement = animator.deltaPosition + animatorDeltaPositionOffset;
        if (ignoreRootMotionY)
            animationMovement.y = 0f;
        moveDir = SetDirOnSlop(animationMovement) * moveSpeedMultiplier;
        UpdateCharacterMove(moveDir, animator.deltaRotation);
    }

    /// <summary>
    /// 斜坡的处理
    /// </summary>
    private Vector3 SetDirOnSlop(Vector3 dir)
    {
        if (Physics.Raycast(transform.position, Vector3.down, out var hitInfo, 1f))
        {
            if (Vector3.Dot(hitInfo.normal, Vector3.up) != 1f)
                return Vector3.ProjectOnPlane(dir, hitInfo.normal);
        }
        return dir;
    }

    public void UpdateCharacterMove(Vector3 deltaDir, Quaternion deltaRot)
    {
        if (!ignoreRotationRootMotion && deltaRot != Quaternion.identity)
            transform.rotation = deltaRot * transform.rotation;

        // 每帧移动 deltaDir 个单位
        if (controller.enabled)
        {
            animationVelocity = deltaDir;
            controller.Move(deltaDir);
        }
    }

    public float ChangeVerticalSpeed(float speed)
    {
        verticalSpeed = speed;
        return verticalSpeed;
    }

    public void AddHorizontalVelocityInAir(Vector3 vel)
    {
        horizontalVelocityInAir = new Vector3(vel.x, 0f, vel.z);
    }

    public void ClearHorizontalVelocity()
    {
        horizontalVelocityInAir = Vector3.zero;
    }
    
    public void ApplyControllerProfile(CharacterControllerProfile profile)
    {
        if (controller != null)
        {
            controller.height = profile.height;
            controller.radius = profile.radius;
            controller.center = profile.center;
            controller.stepOffset = profile.stepOffset;
            controller.skinWidth = profile.skinWidth;
        }

        groundDetectionRadius = profile.groundRadius;
        groundDetectionOffset = profile.groundDetectedOffset;
    }

    private Vector3 GetGroundDetectionOrigin()
    {
        if (controller == null)
            return transform.position + Vector3.up * groundDetectionOffset;

        var bounds = controller.bounds;
        var origin = bounds.center + Vector3.down * bounds.extents.y;
        return origin + Vector3.up * groundDetectionOffset;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = CheckOnGround() ? Color.green : Color.red;
        Gizmos.DrawWireSphere(GetGroundDetectionOrigin(), groundDetectionRadius);
    }

    // ════════════════════════════════════════════
    // 技能层管理（Player / Boss / Npc 通用）
    // ════════════════════════════════════════════

    /// <summary>初始化指定索引的 Animancer 技能层（权重=0，非叠加）</summary>
    protected void SetupAnimancerLayers(int skillLayerIndex)
    {
        var layer = animancer.Layers[skillLayerIndex];
        layer.SetWeight(0f);
        layer.IsAdditive = false;
    }

    /// <summary>进入技能模式：切换技能层权重，覆盖或上半身混合</summary>
    public void EnterSkillMode(bool upperBody, int skillLayerIndex, AvatarMask upperBodyMask)
    {
        var baseLayer  = animancer.Layers[0];
        var skillLayer = animancer.Layers[skillLayerIndex];

        skillLayer.IsAdditive = false;
        skillLayer.Mask = upperBody ? upperBodyMask : null;

        baseLayer.Stop();
        skillLayer.SetWeight(1f);
        baseLayer.SetWeight(upperBody ? 1f : 0f);
    }

    /// <summary>退出技能模式：淡出技能层，淡入基础层</summary>
    public void ExitSkillMode(int skillLayerIndex, float fadeOut)
    {
        ClearSkillRootMotion();

        var baseLayer  = animancer.Layers[0];
        var skillLayer = animancer.Layers[skillLayerIndex];

        var skillState = skillLayer.CurrentState;
        if (skillState != null)
            skillState.IsPlaying = false;

        skillLayer.StartFade(0f, fadeOut);
        baseLayer.StartFade(1f, fadeOut);
    }

    /// <summary>每帧调用：当技能层权重淡出至 0 时停止层内所有状态</summary>
    protected void CleanupFinishedSkillLayer(int skillLayerIndex)
    {
        var skillLayer = animancer.Layers[skillLayerIndex];
        if (skillLayer.Weight > 0f || skillLayer.CurrentState == null)
            return;

        skillLayer.Stop();
    }

    /// <summary>设置技能根运动模式</summary>
    public void SetSkillRootMotion(Action<Vector3, Quaternion> handler, bool applyRootMotion)
    {
        if (applyRootMotion && handler != null)
            SetRootMotionMode(RootMotionMode.Custom, handler);
        else
            SetRootMotionMode(RootMotionMode.Suppressed, null);
    }

    /// <summary>清除技能根运动，恢复默认模式</summary>
    public void ClearSkillRootMotion()
    {
        SetRootMotionMode(RootMotionMode.Default, null);
    }

    /// <summary>技能位移（直接推动 CharacterController）</summary>
    public void OnSkillMove(Vector3 deltaPos)
    {
        if (controller == null || !controller.enabled || !gameObject.activeInHierarchy)
            return;
        controller.Move(deltaPos);
    }

    /// <summary>技能旋转（增量四元数）</summary>
    public void OnSkillRotate(Quaternion deltaRot)
    {
        transform.rotation = deltaRot * transform.rotation;
    }
}
