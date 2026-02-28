/// <summary>
/// 全身技能状态（Layer1 承载技能动画，Layer0 权重降为 0）
///
/// 职责划分：
/// - PlayerController.HandleSkillInput()  → 从移动状态首次触发技能 → 切换到本状态
/// - PlayerSkillState.HandleCombatInput() → 技能进行中的连击检测
/// - PlayerSkillState.HandleMoveInterrupt → 移动打断技能
/// - NotifySkillEnd()                     → 技能自然结束回调（由 PlayerController.Change2IdleState 调用）
///
/// 过渡无生硬跳帧的关键：
/// EnterSkillMode 用 SetWeight 立即切层权重，并 Stop 清除 Layer0 残留状态。
/// 技能结束/打断时，先 ExitSkillMode（Layer1 开始淡出，Layer0 开始淡入），
/// 再立即切换状态机 —— 此时 Layer0 weight ≈ 0，新动画在不可见时静默开始，
/// 随着 Layer0 权重恢复自然淡入，无跳帧、无 "total weight ≠ 1" 警告。
/// </summary>
public class PlayerSkillState : PlayerMovementState
{
    // 是否正在执行退出流程（防止重复触发）
    private bool isPendingExit;

    public PlayerSkillState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    // ── 生命周期 ───────────────────────────────────────────────────────

    public override void OnEnter()
    {
        base.OnEnter();
        isPendingExit = false;
        player.EnterSkillMode(false); // 全身技能：Layer1 SetWeight(1), Layer0 Stop + SetWeight(0)
    }

    public override void OnUpdate()
    {
        base.OnUpdate();
        if (isPendingExit) return; // 退出流程中不再响应输入

        ApplySoftLock();       // 自动转向最近敌人
        HandleCombatInput();   // 连击检测
        HandleMoveInterrupt(); // 移动打断
    }

    public override void OnExit()
    {
        base.OnExit();
        // 保底：外部强制切换状态（跌落、死亡等）时确保退出技能模式
        // ExitSkillMode 内有 !inSkill 保护，正常流程下此处为无操作
        player.ExitSkillMode();
    }

    public override void OnAnimationEnd() { }
    public override void OnAnimationUpdate() { }

    // 技能硬直期间不响应闪避/跳跃等输入（如需要可在此注册特定中断键）
    protected override void AddEventListening() { base.AddEventListening(); }
    protected override void RemoveEventListening() { base.RemoveEventListening(); }

    // ── 软转向（Soft Lock）─────────────────────────────────────────────

    private const float DefaultSoftLockRadius = 6f;
    private const float DefaultSoftLockRotateSpeed = 720f; // 度/秒

    // 延迟初始化，避免构造时 player 尚未 Awake
    private Skill.SkillPlayer _cachedSkillPlayer;
    private bool _skillPlayerSearched;

    private Skill.SkillPlayer GetSkillPlayer()
    {
        if (_skillPlayerSearched) return _cachedSkillPlayer;
        _skillPlayerSearched = true;
        _cachedSkillPlayer = player.GetComponentInChildren<Skill.SkillPlayer>();
        return _cachedSkillPlayer;
    }

    /// <summary>
    /// 攻击期间自动平滑转向最近敌人，不吸附位置
    /// </summary>
    private void ApplySoftLock()
    {
        float radius = playerSO?.playerMovementData?.SoftLockRadius ?? DefaultSoftLockRadius;
        float rotSpeed = playerSO?.playerMovementData?.SoftLockRotateSpeed ?? DefaultSoftLockRotateSpeed;

        // 搜索范围内的目标（只找 Enemy Layer 上的碰撞体）
        var skillPlayer = GetSkillPlayer();
        int layerMask = skillPlayer != null ? skillPlayer.attackDetectionLayer : ~0;
        var colliders = UnityEngine.Physics.OverlapSphere(
            player.transform.position,
            radius,
            layerMask
        );

        UnityEngine.Transform nearest = null;
        float nearestSqDist = float.MaxValue;

        foreach (var col in colliders)
        {
            if (col.gameObject == player.gameObject) continue;
            // 只转向有 IHitTarget 的目标（=敌人）
            if (col.GetComponentInParent<IHitTarget>() == null) continue;

            float sqDist = (col.transform.position - player.transform.position).sqrMagnitude;
            if (sqDist < nearestSqDist)
            {
                nearestSqDist = sqDist;
                nearest = col.transform;
            }
        }

        if (nearest == null) return;

        // 计算目标水平方向
        UnityEngine.Vector3 dir = nearest.position - player.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        // 平滑旋转（不跳变）
        UnityEngine.Quaternion targetRot = UnityEngine.Quaternion.LookRotation(dir.normalized);
        player.transform.rotation = UnityEngine.Quaternion.RotateTowards(
            player.transform.rotation,
            targetRot,
            rotSpeed * UnityEngine.Time.deltaTime
        );
    }

    // ── 外部接口 ───────────────────────────────────────────────────────

    /// <summary>
    /// 技能自然结束时由 PlayerController.Change2IdleState() 调用。
    /// ExitSkillMode → 立即切换状态机（Layer0 在不可见时开始播放新动画）→ 随 Layer0 淡入自然过渡
    /// </summary>
    public void NotifySkillEnd()
    {
        if (isPendingExit) return;
        isPendingExit = true;

        player.ExitSkillMode(); // Layer1 开始淡出（freeze 当前帧），Layer0 开始淡入

        // 立即切换状态机：Layer0 weight 此时接近 0，新动画不可见
        // 随着 ExitSkillMode 的 skillLayerFadeOut 淡入完成，动画自然呈现
        if (inputServer.Move != UnityEngine.Vector2.zero)
            playerStateMachine.ChangeState(playerStateMachine.moveLoopState);
        else
            playerStateMachine.ChangeState(playerStateMachine.idleState);
    }

    // ── 内部逻辑 ───────────────────────────────────────────────────────

    /// <summary>
    /// 连击检测（替代 PlayerController.HandleSkillInput 在技能中的部分）
    /// </summary>
    private void HandleCombatInput()
    {
        var skillBrain = player.SkillBrain;
        var skillInput = player.SkillInput;
        if (skillBrain == null || skillInput == null) return;

        for (int i = 0; i < skillBrain.SkillCount; i++)
        {
            bool valid = false;
            int skillIndex = skillBrain.GetSkillIndex(i);

            if (i == 0)
            {
                valid = skillInput.GetBasicAttackState() && skillBrain.CheckReleaseSkill(i);
                if (valid) skillInput.ResetBasicBuffer();
            }

            if (!valid)
            {
                valid = skillInput.GetSkillState(skillIndex) && skillBrain.CheckReleaseSkill(i);
                if (valid) skillInput.ResetSkillBuffer(skillIndex);
            }

            if (valid)
            {
                skillBrain.ReleaseSkill(i); // 触发连击，不切换状态（已在 skillState）
                return;
            }
        }
    }

    /// <summary>
    /// 移动打断技能
    /// </summary>
    private void HandleMoveInterrupt()
    {
        var skillBrain = player.SkillBrain;
        if (skillBrain == null || !skillBrain.CanInterrupt) return;
        if (inputServer.Move == UnityEngine.Vector2.zero) return;

        isPendingExit = true;

        skillBrain.InterruptCurrentSkill();
        player.DestroyWeapon(-1);
        player.ExitSkillMode(); // Layer1 开始淡出，Layer0 开始淡入
        playerStateMachine.ChangeState(playerStateMachine.moveStartState); // 立即切换，新动画随 Layer0 淡入
    }
}
