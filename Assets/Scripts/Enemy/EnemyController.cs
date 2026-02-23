using System;
using System.Collections.Generic;
using Animancer;
using Attribute;
using Config;
using GOAP;
using Manager;
using Sirenix.OdinInspector;
using Skill;
using UnityEngine;
using UnityEngine.AI;

namespace Enemy
{
    /// <summary>
    /// 敌人控制器（HFSM + CharacterController + Animancer）
    /// 设计目标：复用玩家控制器的运动/根运动能力，同时把AI决策和动画驱动解耦。
    /// </summary>
    public class EnemyController : CharacterControllerBase, ICharacter, IGOAPOwner
    {
        public enum BrainMode
        {
            Simple = 0,
            GOAP = 1,
        }

        private enum BossMoveIntent
        {
            None = 0,
            Chase = 1,
            Retreat = 2,
            StrafeLeft = 3,
            StrafeRight = 4,
        }

        private enum BossEvasionDirection
        {
            Forward = 0,
            Backward = 1,
            Left = 2,
            Right = 3,
        }

        private enum PostAttackRetreatActionType
        {
            AvoidBackward = 0,
            SlideBackward = 1,
            RollBackward = 2,
            Auto = 3,
        }

        [Serializable]
        private class EnemyAIData
        {
            [Header("感知")]
            [Min(0f)] public float detectRadius = 12f;
            [Min(0f)] public float loseTargetRadius = 18f;
            [Min(0f)] public float targetRefreshInterval = 0.2f;

            [Header("追击")]
            [Min(0f)] public float chaseSpeed = 3.5f;
            [Min(0f)] public float rotateSpeed = 12f;
            [Min(0f)] public float repathInterval = 0.12f;
            public bool useNavMeshAgent = true;

            [Header("轻量决策(参考RPG Plus)")]
            [Min(0f)] public float walkRange = 6f;
            [Min(0f)] public float walkSpeed = 2.2f;
            [Min(0f)] public float runSpeed = 4f;
            [Range(0f, 1f)] public float vigilantChance = 0.66f;
            [Min(0f)] public float vigilantRange = 4f;
            [Min(0f)] public float vigilantSpeed = 2.4f;
            [Min(0f)] public float vigilantTimeMin = 0.2f;
            [Min(0f)] public float vigilantTimeMax = 2.5f;
            [Min(0f)] public float vigilantCheckInterval = 0.6f;
            [Min(0f)] public float vigilantStopTolerance = 0.5f;

            [Header("走位")]
            [Min(0f)] public float preferredMinRange = 2.0f;
            [Min(0f)] public float preferredMaxRange = 4.8f;
            [Range(0.2f, 1.5f)] public float strafeSpeedMultiplier = 0.8f;
            [Min(0f)] public float strafeSwitchIntervalMin = 0.5f;
            [Min(0f)] public float strafeSwitchIntervalMax = 1.1f;

            [Header("攻击")]
            [Min(0f)] public float attackRange = 2.2f;
            [Min(0f)] public float attackCooldown = 1.2f;
            [Min(0f)] public float attackFallbackTime = 1.2f;
            public bool allowAttackRootMotion = true;
            [Tooltip("当接了技能系统时使用的默认技能索引")]
            public int defaultAttackSkillIndex = 0;

            [Header("攻击后后撤")]
            public bool retreatAfterAttack = true;
            public PostAttackRetreatActionType retreatAfterAttackAction = PostAttackRetreatActionType.AvoidBackward;
            [Min(0f)] public float retreatAfterAttackStartDelay = 0.12f;

            [Header("受击")]
            [Min(0f)] public float hitRecoverTime = 0.25f;
        }

        [Serializable]
        private class EnemyAnimationData
        {
            [Header("基础动作")]
            public ClipTransition idle;
            public ClipTransition move;
            public ClipTransition hit;
            public ClipTransition death;

            [Header("攻击动作（顺序轮询）")]
            public List<ClipTransition> attacks = new();
        }

        [Serializable]
        private class MindReadingConfig
        {
            [LabelText("启用读指令")] public bool enable = true;
            [LabelText("读指令概率"), Range(0f, 1f)] public float readProbability = 0.35f;
            [LabelText("反应延迟最小"), Min(0f)] public float reactionDelayMin = 0.12f;
            [LabelText("反应延迟最大"), Min(0f)] public float reactionDelayMax = 0.24f;
            [LabelText("触发冷却"), Min(0f)] public float readCooldown = 2f;
            [LabelText("触发距离"), Min(0f)] public float triggerRange = 4f;
            [LabelText("信号记忆时间"), Min(0f)] public float signalMemory = 0.35f;
            [LabelText("窗口时长"), Min(0.1f)] public float windowDuration = 10f;
            [LabelText("窗口最大触发次数"), Min(1)] public int maxReadsPerWindow = 3;
        }

        [Serializable]
        private class BossDebugConfig
        {
            [LabelText("启用Boss调试")] public bool enable = true;
            [LabelText("状态快照间隔"), Min(0.05f)] public float snapshotInterval = 0.35f;
            [LabelText("仅变化时输出")] public bool snapshotOnlyOnChange = true;
            [LabelText("输出决策日志")] public bool logDecision = true;
            [LabelText("输出GOAP日志")] public bool logGoap = true;
            [LabelText("输出读指令日志")] public bool logMindReading = true;
        }

        [Header("Config")]
        [SerializeField] private CharacterConfig characterConfig;
        [SerializeField] private EnemyAIData aiData = new();
        [SerializeField] private EnemyAnimationData animationData = new();

        [Header("Combat")]
        [SerializeField] private CharacterAttribute characterAttribute;
        [SerializeField] private WeaponSlotManager weaponSlotManager;
        [SerializeField] private SkillBrainBase skillBrain;

        [Header("View")]
        [SerializeField] private Transform modelTransform;
        [SerializeField] private LayerMask targetLayerMask = ~0;
        [SerializeField] private Transform forcedTarget;

        [Header("Navigation")]
        [SerializeField] private NavMeshAgent navMeshAgent;

        [Header("Animancer Skill Layer")]
        [SerializeField] private int skillLayerIndex = 1;
        [SerializeField] private AvatarMask upperBodyMask;
        [SerializeField, Range(0f, 0.3f)] private float skillLayerFadeOut = 0.1f;

        [Header("Debug")]
        [SerializeField] private bool drawDebugGizmos = true;

        [Header("Boss Brain")]
        [SerializeField] private BrainMode brainMode = BrainMode.Simple;
        [SerializeField] private GOAPAgent goapAgent;
        [SerializeField] private bool autoBuildDefaultBossGoap = true;
        [SerializeField, Min(0f)] private float retreatDistance = 4.2f;

        [Header("Boss Motion Source")]
        [SerializeField] private bool usePlayerSoLocomotion = true;
        [SerializeField] private PlayerSO bossMotionSource;
        [SerializeField, Min(0f)] private float locomotionRotationSmooth = 0.2f;
        [SerializeField, Min(0f)] private float locomotionSpeedSmooth = 0.2f;
        [SerializeField] private float walkSpeedParameterValue = 1f;
        [SerializeField] private float runSpeedParameterValue = 2f;

        [Header("Boss Mind Reading")]
        [SerializeField] private MindReadingConfig mindReading = new();

        [Header("Boss Debug")]
        [SerializeField] private BossDebugConfig bossDebug = new();

        private readonly Collider[] targetBuffer = new Collider[24];
        private EnemyStateMachine stateMachine;
        private BossMoveIntent moveIntent;
        private bool goapInitialized;

        private Transform currentTarget;
        private float nextTargetRefreshTime;
        private float nextAttackTime;
        private float nextRepathTime;
        private bool simpleVigilantMode;
        private float simpleVigilantEndTime;
        private float nextSimpleVigilantCheckTime;
        private bool retreatAfterAttackPending;
        private float retreatAfterAttackStartTime;

        private bool isDead;
        private bool isInSkill;
        private bool currentSkillUpperBody;
        private bool isAttackPlaying;
        private bool usingSkillAttack;
        private float attackFallbackEndTime;
        private int attackClipCursor = -1;
        private bool isDodging;
        private float dodgeFallbackEndTime;
        private float nextEvasiveActionTime;
        private int strafeSign = 1;
        private float nextStrafeSwitchTime;

        private bool pendingDodgeThreat;
        private float pendingDodgeExpireTime;
        private float queuedMindReadTime = -1f;
        private float nextMindReadTime;
        private float mindReadWindowStartTime;
        private int mindReadCountInWindow;
        private float nextMindSignalLogTime;
        private float nextMindSkipLogTime;

        private float hitEndTime;
        private Vector3 repelVelocity;
        private float repelRemainTime;
        private float nextDebugSnapshotTime;
        private string lastDebugSnapshot = string.Empty;

        private SmoothedFloatParameter standValueParameter;
        private SmoothedFloatParameter rotationValueParameter;
        private SmoothedFloatParameter speedValueParameter;
        private SmoothedFloatParameter lockValueParameter;
        private SmoothedFloatParameter lockXValueParameter;
        private SmoothedFloatParameter lockYValueParameter;
        private bool playingMoveStartTransition;

        public AnimancerComponent Animancer => animancer;
        public AnimancerLayer SkillLayer => animancer.Layers[skillLayerIndex];
        public Transform ModelTransform => modelTransform != null ? modelTransform : transform;
        public bool IsDead => isDead;
        public bool HasTarget => currentTarget != null;
        public float RetreatDistance => retreatDistance;
        public float AttackRange => aiData.attackRange;
        public float PreferredMinRange => Mathf.Min(aiData.preferredMinRange, aiData.preferredMaxRange);
        public float PreferredMaxRange => Mathf.Max(aiData.preferredMinRange, aiData.preferredMaxRange);

        private bool UseGoapBrain => brainMode == BrainMode.GOAP && goapAgent != null;
        private bool UsePlayerSoMotion =>
            usePlayerSoLocomotion &&
            bossMotionSource != null &&
            bossMotionSource.playerMovementData != null;

        private float ChaseSpeed
        {
            get
            {
                if (aiData.chaseSpeed > 0f)
                    return aiData.chaseSpeed;
                if (characterConfig != null && characterConfig.RunSpeed > 0f)
                    return characterConfig.RunSpeed;
                return 3.5f;
            }
        }

        private float WalkSpeed
        {
            get
            {
                if (aiData.walkSpeed > 0f)
                    return aiData.walkSpeed;
                return Mathf.Max(0.5f, ChaseSpeed * 0.65f);
            }
        }

        private float RunSpeed
        {
            get
            {
                if (aiData.runSpeed > 0f)
                    return aiData.runSpeed;
                return ChaseSpeed;
            }
        }

        private float VigilantSpeed
        {
            get
            {
                if (aiData.vigilantSpeed > 0f)
                    return aiData.vigilantSpeed;
                return Mathf.Max(0.5f, WalkSpeed);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            if (modelTransform == null)
                modelTransform = transform;

            if (navMeshAgent == null)
                navMeshAgent = GetComponent<NavMeshAgent>();

            stateMachine = new EnemyStateMachine(this);
        }

        private void Start()
        {
            InitByConfig();
            SetupSkillLayer();
            SetupNavigationAgent();
            InitPlayerSoMotionIfNeeded();

            // 敌人默认由程序驱动位移，避免巡逻/追击时被root motion抢控制权。
            disableRootMotion = true;
            ignoreRootMotionY = true;

            skillBrain?.Init(this);
            weaponSlotManager?.RefreshSlots();
            InitGoapIfNeeded();
            mindReadWindowStartTime = Time.time;

            if (IsBossDebugEnabled)
            {
                RayDebug.Info(
                    $"[BossDebug] 启用调试: Brain={brainMode}, UsePlayerSO={UsePlayerSoMotion}, MindRead={mindReading.enable}",
                    this);
            }

            stateMachine.ChangeState(stateMachine.idleState);
        }

        protected override void Update()
        {
            base.Update();
            UpdateMindReadingThreatIfNeeded();
            UpdateDodgeStateIfNeeded();
            UpdateGoapIfNeeded();
            stateMachine?.OnUpdate();
            ApplyRepelMotion();
            UpdatePlayerSoLockParameters();
            CleanupFinishedSkillLayer();
            SyncAgentPosition();
            DebugTickSnapshot();
        }

        private void OnDestroy()
        {
            DisposeLocomotionParameters();
        }

        protected override void OnAnimatorMove()
        {
            base.OnAnimatorMove();
            stateMachine?.OnAnimationUpdate();
            SyncAgentPosition();
        }

        private void InitByConfig()
        {
            if (characterConfig == null)
                return;

            ApplyControllerProfile(characterConfig.ControllerProfile);
            if (characterConfig.Avatar != null)
                animator.avatar = characterConfig.Avatar;

            if (characterAttribute != null)
                characterAttribute.Init(characterConfig, characterConfig.hpBaseValue, characterConfig.mpBaseValue);
        }

        private void SetupSkillLayer()
        {
            var layer = SkillLayer;
            layer.SetWeight(0f);
            layer.IsAdditive = false;
        }

        private void SetupNavigationAgent()
        {
            if (navMeshAgent == null)
                return;

            navMeshAgent.updatePosition = false;
            navMeshAgent.updateRotation = false;
            navMeshAgent.speed = RunSpeed;
            navMeshAgent.stoppingDistance = Mathf.Max(0.1f, aiData.attackRange * 0.8f);
        }

        private void SyncAgentPosition()
        {
            if (!CanUseNavMesh())
                return;

            navMeshAgent.nextPosition = transform.position;
        }

        private bool CanUseNavMesh()
        {
            return aiData.useNavMeshAgent &&
                   navMeshAgent != null &&
                   navMeshAgent.enabled &&
                   navMeshAgent.isOnNavMesh;
        }

        private void InitPlayerSoMotionIfNeeded()
        {
            DisposeLocomotionParameters();
            if (!UsePlayerSoMotion || bossMotionSource.playerParameterData == null)
                return;

            var parameterData = bossMotionSource.playerParameterData;
            standValueParameter = CreateSmoothedParameter(parameterData.standValueParameter, 0.15f);
            rotationValueParameter = CreateSmoothedParameter(parameterData.rotationValueParameter, locomotionRotationSmooth);
            speedValueParameter = CreateSmoothedParameter(parameterData.speedValueParameter, locomotionSpeedSmooth);
            lockValueParameter = CreateSmoothedParameter(parameterData.LockValueParameter, 0.1f);
            lockXValueParameter = CreateSmoothedParameter(parameterData.Lock_X_ValueParameter, 0.2f);
            lockYValueParameter = CreateSmoothedParameter(parameterData.Lock_Y_ValueParameter, 0.2f);

            SetParameterCurrent(standValueParameter, 1f);
            SetParameterCurrent(rotationValueParameter, 0f);
            SetParameterCurrent(speedValueParameter, 0f);
            SetParameterCurrent(lockValueParameter, 0f);
            SetParameterCurrent(lockXValueParameter, 0f);
            SetParameterCurrent(lockYValueParameter, 0f);
        }

        private SmoothedFloatParameter CreateSmoothedParameter(StringAsset parameterName, float smoothTime)
        {
            if (parameterName == null || animancer == null)
                return null;

            return new SmoothedFloatParameter(animancer, parameterName, Mathf.Max(0.01f, smoothTime));
        }

        private void DisposeLocomotionParameters()
        {
            DisposeParameter(ref standValueParameter);
            DisposeParameter(ref rotationValueParameter);
            DisposeParameter(ref speedValueParameter);
            DisposeParameter(ref lockValueParameter);
            DisposeParameter(ref lockXValueParameter);
            DisposeParameter(ref lockYValueParameter);
        }

        private static void DisposeParameter(ref SmoothedFloatParameter parameter)
        {
            if (parameter == null)
                return;

            parameter.Dispose();
            parameter = null;
        }

        private static void SetParameterCurrent(SmoothedFloatParameter parameter, float value)
        {
            if (parameter != null)
                parameter.CurrentValue = value;
        }

        private void UpdatePlayerSoLockParameters()
        {
            if (lockValueParameter != null)
                lockValueParameter.TargetValue = HasTarget ? 1f : 0f;

            if (HasTarget)
                return;

            if (lockXValueParameter != null)
                lockXValueParameter.TargetValue = 0f;
            if (lockYValueParameter != null)
                lockYValueParameter.TargetValue = 0f;
        }

        private void UpdatePlayerSoLocomotionParameters(Vector3 velocity, float speedParameter = -1f)
        {
            if (!UsePlayerSoMotion)
                return;

            Vector3 planarVelocity = Vector3.ProjectOnPlane(velocity, Vector3.up);
            float targetSpeed = speedParameter;
            if (targetSpeed < 0f)
            {
                float normalized = Mathf.Clamp01(planarVelocity.magnitude / Mathf.Max(0.01f, ChaseSpeed));
                targetSpeed = Mathf.Lerp(walkSpeedParameterValue, runSpeedParameterValue, normalized);
                if (planarVelocity.sqrMagnitude < 0.0001f)
                    targetSpeed = 0f;
            }

            if (speedValueParameter != null)
                speedValueParameter.TargetValue = targetSpeed;

            if (planarVelocity.sqrMagnitude <= 0.0001f)
            {
                if (lockXValueParameter != null)
                    lockXValueParameter.TargetValue = 0f;
                if (lockYValueParameter != null)
                    lockYValueParameter.TargetValue = 0f;
                return;
            }

            float signedAngle = GetSignedAngleToDirection(planarVelocity);
            if (rotationValueParameter != null)
                rotationValueParameter.TargetValue = signedAngle * Mathf.Deg2Rad;

            Vector3 localDirection = ModelTransform.InverseTransformDirection(planarVelocity.normalized);
            if (lockXValueParameter != null)
                lockXValueParameter.TargetValue = localDirection.x * targetSpeed;
            if (lockYValueParameter != null)
                lockYValueParameter.TargetValue = localDirection.z * targetSpeed;
        }

        private float GetSignedAngleToDirection(Vector3 worldDirection)
        {
            Vector3 planar = Vector3.ProjectOnPlane(worldDirection, Vector3.up);
            if (planar.sqrMagnitude < 0.0001f)
                return 0f;

            float targetYaw = Quaternion.LookRotation(planar.normalized, Vector3.up).eulerAngles.y;
            return Mathf.DeltaAngle(ModelTransform.eulerAngles.y, targetYaw);
        }

        private float GetSafePreferredMinRange()
        {
            return Mathf.Max(0f, Mathf.Min(aiData.preferredMinRange, aiData.preferredMaxRange));
        }

        private float GetSafePreferredMaxRange()
        {
            float minRange = GetSafePreferredMinRange();
            return Mathf.Max(minRange + 0.1f, Mathf.Max(aiData.preferredMinRange, aiData.preferredMaxRange));
        }

        private bool BrainIsTooCloseToTarget()
        {
            if (!HasTarget)
                return false;

            return BrainDistanceToTarget() < GetSafePreferredMinRange();
        }

        private bool BrainIsTooFarFromTarget()
        {
            if (!HasTarget)
                return false;

            return BrainDistanceToTarget() > GetSafePreferredMaxRange();
        }

        private bool BrainShouldRetreatForSpacing()
        {
            // 后撤改为“攻击后播放后退闪避/翻滚动画（RootMotion）”，不再走手动位移退距。
            return false;
        }

        private void UpdateMindReadingThreatIfNeeded()
        {
            if (!UseGoapBrain || isDead || mindReading == null || !mindReading.enable)
                return;

            if (!HasTarget)
            {
                queuedMindReadTime = -1f;
                ClearPendingMindReadThreat();
                return;
            }

            if (TryDetectPlayerAttackIntentSignal())
                TryQueueMindReadDodgeRequest();

            if (queuedMindReadTime > 0f && Time.time >= queuedMindReadTime)
            {
                queuedMindReadTime = -1f;
                pendingDodgeThreat = true;
                pendingDodgeExpireTime = Time.time + Mathf.Max(0.05f, mindReading.signalMemory);
                LogBossMindReading($"读指令触发成功：进入可闪避窗口，过期时间={pendingDodgeExpireTime:F2}");
                DebugTickSnapshot("MindReadTriggered", true);
            }

            if (pendingDodgeThreat && Time.time >= pendingDodgeExpireTime)
                ClearPendingMindReadThreat("可闪避窗口过期");
        }

        private bool TryDetectPlayerAttackIntentSignal()
        {
            InputService inputService = InputService.Instance;
            if (inputService == null || inputService.inputMap == null)
                return false;

            float distance = BrainDistanceToTarget();
            if (distance > Mathf.Max(0f, mindReading.triggerRange))
                return false;

            var playerActions = inputService.inputMap.Player;
            bool basic = playerActions.BasicAttack.triggered;
            bool skill1 = playerActions.Skill1.triggered;
            bool skill2 = playerActions.Skill2.triggered;
            bool skill3 = playerActions.Skill3.triggered;
            bool hasSignal = basic || skill1 || skill2 || skill3;
            if (!hasSignal)
                return false;

            if (Time.time >= nextMindSignalLogTime)
            {
                nextMindSignalLogTime = Time.time + 0.1f;
                LogBossMindReading(
                    $"检测到玩家攻击输入信号(Basic={basic}, S1={skill1}, S2={skill2}, S3={skill3}, Dist={distance:F2})");
            }

            return true;
        }

        private void TryQueueMindReadDodgeRequest()
        {
            if (Time.time < nextMindReadTime)
            {
                if (Time.time >= nextMindSkipLogTime)
                {
                    nextMindSkipLogTime = Time.time + 0.5f;
                    LogBossMindReading($"跳过读指令：冷却中，剩余={Mathf.Max(0f, nextMindReadTime - Time.time):F2}s");
                }
                return;
            }

            float duration = Mathf.Max(0.1f, mindReading.windowDuration);
            if (Time.time - mindReadWindowStartTime > duration)
            {
                mindReadWindowStartTime = Time.time;
                mindReadCountInWindow = 0;
                LogBossMindReading("重置读指令窗口计数");
            }

            if (mindReadCountInWindow >= Mathf.Max(1, mindReading.maxReadsPerWindow))
            {
                if (Time.time >= nextMindSkipLogTime)
                {
                    nextMindSkipLogTime = Time.time + 0.5f;
                    LogBossMindReading("跳过读指令：已达到窗口触发上限");
                }
                return;
            }

            float random = UnityEngine.Random.value;
            float probability = Mathf.Clamp01(mindReading.readProbability);
            if (random > probability)
            {
                if (Time.time >= nextMindSkipLogTime)
                {
                    nextMindSkipLogTime = Time.time + 0.5f;
                    LogBossMindReading($"跳过读指令：概率未命中(random={random:F2}, p={probability:F2})");
                }
                return;
            }

            float minDelay = Mathf.Max(0f, mindReading.reactionDelayMin);
            float maxDelay = Mathf.Max(minDelay, mindReading.reactionDelayMax);
            float delay = UnityEngine.Random.Range(minDelay, maxDelay);
            queuedMindReadTime = Time.time + delay;
            nextMindReadTime = Time.time + Mathf.Max(0f, mindReading.readCooldown);
            mindReadCountInWindow++;
            LogBossMindReading(
                $"排队读指令成功：delay={delay:F2}s, 下次可触发={nextMindReadTime:F2}, 窗口计数={mindReadCountInWindow}");
        }

        private bool ShouldTriggerMindReadDodgeGoal()
        {
            if (!pendingDodgeThreat || !HasTarget || !UsePlayerSoMotion || bossMotionSource.playerMovementData == null)
                return false;

            if (isDodging || isDead || isAttackPlaying)
                return false;

            if (stateMachine == null ||
                (stateMachine.currentState != stateMachine.chaseState &&
                 stateMachine.currentState != stateMachine.idleState))
            {
                return false;
            }

            if (Time.time >= pendingDodgeExpireTime)
            {
                ClearPendingMindReadThreat();
                return false;
            }

            return BrainDistanceToTarget() <= Mathf.Max(0f, mindReading.triggerRange + 1f);
        }

        private void ClearPendingMindReadThreat(string reason = null)
        {
            bool hadThreat = pendingDodgeThreat;
            pendingDodgeThreat = false;
            pendingDodgeExpireTime = 0f;
            if (hadThreat && !string.IsNullOrEmpty(reason))
                LogBossMindReading(reason);
        }

        private void UpdateDodgeStateIfNeeded()
        {
            if (!isDodging)
                return;

            if (Time.time >= dodgeFallbackEndTime)
                EndDefensiveDodge();
        }

        private bool IsBossDebugEnabled => bossDebug != null && bossDebug.enable;

        private void LogBossDecision(string message)
        {
            if (!IsBossDebugEnabled || !bossDebug.logDecision)
                return;

            RayDebug.Info($"[BossDecision] {message}", this);
        }

        private void LogBossMindReading(string message)
        {
            if (!IsBossDebugEnabled || !bossDebug.logMindReading)
                return;

            RayDebug.Info($"[BossMindRead] {message}", this);
        }

        public void BrainDebugGoap(string message)
        {
            if (!IsBossDebugEnabled || !bossDebug.logGoap)
                return;

            RayDebug.Info($"[BossGOAP] {message}", this);
            DebugTickSnapshot("GOAP", true);
        }

        private void DebugTickSnapshot(string reason = null, bool force = false)
        {
            if (!IsBossDebugEnabled)
                return;

            float interval = Mathf.Max(0.05f, bossDebug.snapshotInterval);
            if (!force && Time.time < nextDebugSnapshotTime)
                return;

            nextDebugSnapshotTime = Time.time + interval;
            string snapshot = BuildBossSnapshot();
            if (!force && bossDebug.snapshotOnlyOnChange && snapshot == lastDebugSnapshot)
                return;

            lastDebugSnapshot = snapshot;
            string prefix = string.IsNullOrEmpty(reason) ? string.Empty : $"[{reason}] ";
            RayDebug.Info($"[BossState] {prefix}{snapshot}", this);
        }

        private string BuildBossSnapshot()
        {
            string stateName = stateMachine?.currentState?.GetType().Name ?? "None";
            string targetName = currentTarget != null ? currentTarget.name : "None";
            float distance = BrainDistanceToTarget();
            string distText = distance >= float.MaxValue * 0.5f ? "-" : distance.ToString("F2");
            float retreatDelay = retreatAfterAttackPending
                ? Mathf.Max(0f, retreatAfterAttackStartTime - Time.time)
                : 0f;
            bool inAttackRange = HasTargetInAttackRange();
            bool attackReady = IsAttackReadyForBrain();
            bool needDodge = ReadGoapBool(BossGoapStateNames.NeedDodge);
            bool needRetreat = ReadGoapBool(BossGoapStateNames.NeedRetreat);
            bool attacked = ReadGoapBool(BossGoapStateNames.Attacked);
            string goapGoal = "None";
            if (UseGoapBrain && goapAgent?.plan != null && goapAgent.plan.Running)
                goapGoal = goapAgent.plan.goalName ?? "Running";

            return $"State={stateName} Intent={moveIntent} Target={targetName} Dist={distText} " +
                   $"InRange={inAttackRange} AttackReady={attackReady} Attacking={isAttackPlaying} Dodging={isDodging} " +
                   $"NeedRetreat={needRetreat} NeedDodge={needDodge} Attacked={attacked} " +
                   $"MindSignal={(queuedMindReadTime > 0f)} PendingDodge={pendingDodgeThreat} SimpleVigilant={simpleVigilantMode} " +
                   $"PostAttackRetreat={retreatAfterAttackPending} RetreatAction={aiData.retreatAfterAttackAction} RetreatDelay={retreatDelay:F2} " +
                   $"PreferredRange=[{GetSafePreferredMinRange():F1},{GetSafePreferredMaxRange():F1}] GOAP={goapGoal}";
        }

        private void SetMoveIntent(BossMoveIntent intent, string reason = null)
        {
            if (moveIntent == intent)
                return;

            BossMoveIntent previous = moveIntent;
            moveIntent = intent;
            if (string.IsNullOrEmpty(reason))
                LogBossDecision($"MoveIntent: {previous} -> {intent}");
            else
                LogBossDecision($"MoveIntent: {previous} -> {intent} | {reason}");

            DebugTickSnapshot("MoveIntentChanged", true);
        }

        private void InitGoapIfNeeded()
        {
            if (brainMode == BrainMode.GOAP && goapAgent == null)
            {
                RayDebug.Warn("[EnemyController] BrainMode=GOAP 但未挂 GOAPAgent，已回退到 Simple 模式。");
                return;
            }

            if (!UseGoapBrain)
                return;

            EnsureGoapGlobalManager();

            if (autoBuildDefaultBossGoap)
                BossGoapDefaultFactory.Configure(goapAgent);

            goapAgent.Init(this);
            goapInitialized = true;
            BrainDebugGoap("GOAP 初始化完成");
        }

        private void UpdateGoapIfNeeded()
        {
            if (!UseGoapBrain || !goapInitialized || isDead)
                return;

            SyncGoapWorldState();
            goapAgent.OnUpdate();
        }

        private void SyncGoapWorldState()
        {
            bool attackReady = IsAttackReadyForBrain();
            SetGoapBool(BossGoapStateNames.HasTarget, HasTarget);
            SetGoapBool(BossGoapStateNames.InAttackRange, IsTargetInAttackRange(aiData.attackRange));
            SetGoapBool(BossGoapStateNames.AttackReady, attackReady);
            SetGoapBool(BossGoapStateNames.NeedRetreat, BrainShouldRetreatForSpacing());
            SetGoapBool(BossGoapStateNames.NeedDodge, ShouldTriggerMindReadDodgeGoal());
            SetGoapBool(BossGoapStateNames.TooClose, BrainIsTooCloseToTarget());
            SetGoapBool(BossGoapStateNames.TooFar, BrainIsTooFarFromTarget());

            // 非攻击态就重置“已攻击”标记，使冷却期仍能进入等待/走位计划。
            if (!BrainIsAttacking() || attackReady)
                SetGoapBool(BossGoapStateNames.Attacked, false);

            // 丢失目标时清空攻击节奏状态，避免重新发现目标后沿用旧节奏导致行为突兀。
            if (!HasTarget)
            {
                SetGoapBool(BossGoapStateNames.NeedRetreat, false);
                SetGoapBool(BossGoapStateNames.Attacked, false);
                SetGoapBool(BossGoapStateNames.NeedDodge, false);
                SetGoapBool(BossGoapStateNames.TooClose, false);
                SetGoapBool(BossGoapStateNames.TooFar, false);
                ClearRetreatAfterAttack("GOAP丢失目标");
                ClearPendingMindReadThreat();
            }
        }

        private void SetGoapBool(string stateName, bool value)
        {
            if (goapAgent == null || goapAgent.states == null)
                return;

            if (goapAgent.states.TryGetState<BoolState>(stateName, out var state))
                state.value = value;
        }

        private bool ReadGoapBool(string stateName)
        {
            if (goapAgent == null || goapAgent.states == null)
                return false;

            if (!goapAgent.states.TryGetState<BoolState>(stateName, out var state))
                return false;

            return state.value;
        }

        private void EnsureGoapGlobalManager()
        {
            if (GOAPGlobalManager.Instance != null)
                return;

            var go = new GameObject("[Runtime]GOAPGlobalManager");
            go.AddComponent<GOAPGlobalManager>();
        }

        #region AI Runtime

        private void RefreshTarget(bool allowAcquire = true)
        {
            if (forcedTarget != null)
            {
                currentTarget = forcedTarget;
                return;
            }

            if (currentTarget != null)
            {
                if (!IsTargetValid(currentTarget) || IsOutOfLoseRange(currentTarget.position))
                    currentTarget = null;
                else if (!allowAcquire)
                    return;
            }

            if (!allowAcquire)
                return;

            if (Time.time < nextTargetRefreshTime)
                return;

            nextTargetRefreshTime = Time.time + aiData.targetRefreshInterval;
            currentTarget = FindNearestTarget(aiData.detectRadius);
        }

        private Transform FindNearestTarget(float radius)
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                radius,
                targetBuffer,
                targetLayerMask,
                QueryTriggerInteraction.Ignore);

            float bestSqr = float.MaxValue;
            Transform best = null;

            for (int i = 0; i < count; i++)
            {
                var collider = targetBuffer[i];
                if (collider == null)
                    continue;

                var character = collider.GetComponentInParent<ICharacter>();
                if (character == null || ReferenceEquals(character, this))
                    continue;

                var characterComp = character as Component;
                if (characterComp == null || !characterComp.gameObject.activeInHierarchy)
                    continue;

                var target = character.ModelTransform != null ? character.ModelTransform : characterComp.transform;
                float sqr = GetPlanarSqrDistance(target.position, transform.position);
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = target;
                }
            }

            return best;
        }

        private bool IsTargetValid(Transform target)
        {
            if (target == null || !target.gameObject.activeInHierarchy)
                return false;

            return target.GetComponentInParent<IHitTarget>() != null;
        }

        private bool HasTargetInAttackRange()
        {
            return IsTargetInAttackRange(aiData.attackRange);
        }

        private bool IsTargetInAttackRange(float range)
        {
            if (currentTarget == null)
                return false;

            float sqr = GetPlanarSqrDistance(currentTarget.position, transform.position);
            return sqr <= range * range;
        }

        private bool IsOutOfLoseRange(Vector3 targetPos)
        {
            float sqr = GetPlanarSqrDistance(targetPos, transform.position);
            return sqr > aiData.loseTargetRadius * aiData.loseTargetRadius;
        }

        private static float GetPlanarSqrDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return (a - b).sqrMagnitude;
        }

        private void ScheduleRetreatAfterAttack(string reason = null)
        {
            if (!aiData.retreatAfterAttack || !HasTarget || isDead)
                return;

            float delay = Mathf.Max(0f, aiData.retreatAfterAttackStartDelay);
            retreatAfterAttackPending = true;
            retreatAfterAttackStartTime = Time.time + delay;

            string suffix = string.IsNullOrEmpty(reason) ? string.Empty : $" ({reason})";
            LogBossDecision(
                $"安排攻击后后撤{suffix}: Action={aiData.retreatAfterAttackAction}, Start={retreatAfterAttackStartTime:F2}, Delay={delay:F2}");
            DebugTickSnapshot("ScheduleRetreatAfterAttack", true);
        }

        private void ClearRetreatAfterAttack(string reason = null)
        {
            if (!retreatAfterAttackPending)
                return;

            retreatAfterAttackPending = false;
            retreatAfterAttackStartTime = 0f;
            if (!string.IsNullOrEmpty(reason))
                LogBossDecision($"结束攻击后后撤: {reason}");
        }

        private bool TickPostAttackRetreatIfNeeded()
        {
            if (!retreatAfterAttackPending)
                return false;

            if (!HasTarget || isDead)
            {
                ClearRetreatAfterAttack("后撤取消：目标无效");
                return false;
            }

            if (Time.time < retreatAfterAttackStartTime)
            {
                StopNavigation();
                RotateToTarget(aiData.rotateSpeed);
                UpdatePlayerSoLocomotionParameters(Vector3.zero, 0f);
                return true;
            }

            bool started = TryStartPostAttackRetreatByAnimation();
            ClearRetreatAfterAttack(started ? "已触发后撤动作" : "后撤动作缺失或播放失败");
            return started;
        }

        private void ResetSimpleVigilant(string reason = null)
        {
            if (!simpleVigilantMode)
                return;

            simpleVigilantMode = false;
            simpleVigilantEndTime = 0f;
            LogBossDecision(string.IsNullOrEmpty(reason)
                ? "退出对峙状态"
                : $"退出对峙状态: {reason}");
        }

        private void TryEnterSimpleVigilant(float distance)
        {
            if (simpleVigilantMode)
                return;

            if (distance <= aiData.attackRange * 1.15f)
                return;

            if (Time.time < nextSimpleVigilantCheckTime)
                return;

            nextSimpleVigilantCheckTime = Time.time + Mathf.Max(0.1f, aiData.vigilantCheckInterval);
            float chance = Mathf.Clamp01(aiData.vigilantChance);
            if (UnityEngine.Random.value > chance)
                return;

            float min = Mathf.Max(0f, aiData.vigilantTimeMin);
            float max = Mathf.Max(min, aiData.vigilantTimeMax);
            simpleVigilantMode = true;
            simpleVigilantEndTime = Time.time + UnityEngine.Random.Range(min, max);
            LogBossDecision($"进入对峙状态: 目标时长={simpleVigilantEndTime:F2}, 当前距离={distance:F2}");
            DebugTickSnapshot("EnterVigilant", true);
        }

        private void TickSimpleChaseDecision()
        {
            float distance = BrainDistanceToTarget();
            float walkRange = Mathf.Max(aiData.attackRange + 0.2f, aiData.walkRange);
            float vigilantRange = Mathf.Max(aiData.attackRange + 0.5f, aiData.vigilantRange);

            // 参考 RPG Plus：远距离 Run 追击，近中距离 Walk 并带概率对峙。
            if (distance > walkRange)
            {
                ResetSimpleVigilant("目标超出walkRange");
                MoveTowardsTarget(RunSpeed);
                return;
            }

            TryEnterSimpleVigilant(distance);

            if (simpleVigilantMode)
            {
                if (Time.time >= simpleVigilantEndTime)
                {
                    ResetSimpleVigilant("对峙计时结束");
                }
                else
                {
                    float tolerance = Mathf.Max(0.05f, aiData.vigilantStopTolerance);
                    if (distance < vigilantRange - tolerance)
                    {
                        MoveAwayFromTarget(VigilantSpeed);
                    }
                    else if (distance > vigilantRange + tolerance)
                    {
                        MoveTowardsTarget(VigilantSpeed);
                    }
                    else
                    {
                        StopNavigation();
                        RotateToTarget(aiData.rotateSpeed);
                        UpdatePlayerSoLocomotionParameters(Vector3.zero, 0f);
                    }

                    return;
                }
            }

            if (distance <= aiData.attackRange && Time.time >= nextAttackTime)
            {
                stateMachine.ChangeState(stateMachine.attackState);
                return;
            }

            MoveTowardsTarget(WalkSpeed);
        }

        private void MoveTowardsTarget()
        {
            MoveTowardsTarget(ChaseSpeed);
        }

        private void MoveTowardsTarget(float maxSpeed)
        {
            if (currentTarget == null)
            {
                UpdatePlayerSoLocomotionParameters(Vector3.zero, 0f);
                return;
            }

            float speedLimit = Mathf.Max(0.01f, maxSpeed);
            Vector3 targetPos = currentTarget.position;
            targetPos.y = transform.position.y;

            Vector3 toTarget = targetPos - transform.position;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                UpdatePlayerSoLocomotionParameters(Vector3.zero, 0f);
                return;
            }

            Vector3 velocity;
            if (CanUseNavMesh())
            {
                navMeshAgent.speed = speedLimit;
                if (Time.time >= nextRepathTime)
                {
                    nextRepathTime = Time.time + aiData.repathInterval;
                    navMeshAgent.SetDestination(targetPos);
                }

                velocity = navMeshAgent.desiredVelocity;
            }
            else
            {
                velocity = toTarget.normalized * speedLimit;
            }

            velocity.y = 0f;
            float speed = velocity.magnitude;
            if (speed > speedLimit && speed > 0.0001f)
                velocity = velocity / speed * speedLimit;

            UpdateCharacterMove(velocity * Time.deltaTime, Quaternion.identity);
            RotateToDirection(velocity.sqrMagnitude > 0.0001f ? velocity : toTarget, aiData.rotateSpeed);

            float normalized = Mathf.Clamp01(speedLimit / Mathf.Max(0.01f, RunSpeed));
            float animSpeed = Mathf.Lerp(walkSpeedParameterValue, runSpeedParameterValue, normalized);
            UpdatePlayerSoLocomotionParameters(velocity, animSpeed);
        }

        private void MoveAwayFromTarget()
        {
            MoveAwayFromTarget(ChaseSpeed);
        }

        private void MoveAwayFromTarget(float maxSpeed)
        {
            if (currentTarget == null)
            {
                UpdatePlayerSoLocomotionParameters(Vector3.zero, 0f);
                return;
            }

            float speedLimit = Mathf.Max(0.01f, maxSpeed);
            Vector3 away = transform.position - currentTarget.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
                away = -transform.forward;

            Vector3 velocity = away.normalized * speedLimit;
            UpdateCharacterMove(velocity * Time.deltaTime, Quaternion.identity);
            RotateToDirection(currentTarget.position - transform.position, aiData.rotateSpeed);

            float normalized = Mathf.Clamp01(speedLimit / Mathf.Max(0.01f, RunSpeed));
            float animSpeed = Mathf.Lerp(walkSpeedParameterValue, runSpeedParameterValue, normalized);
            UpdatePlayerSoLocomotionParameters(velocity, animSpeed);
        }

        private void MoveStrafeAroundTarget(bool right)
        {
            if (currentTarget == null)
            {
                UpdatePlayerSoLocomotionParameters(Vector3.zero, 0f);
                return;
            }

            Vector3 toTarget = currentTarget.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
            {
                MoveTowardsTarget();
                return;
            }

            Vector3 radial = toTarget.normalized;
            Vector3 tangent = Vector3.Cross(Vector3.up, radial) * (right ? 1f : -1f);
            Vector3 correction = Vector3.zero;

            if (BrainIsTooCloseToTarget())
                correction = -radial * 0.25f;
            else if (BrainIsTooFarFromTarget())
                correction = radial * 0.25f;

            float speedMul = Mathf.Clamp(aiData.strafeSpeedMultiplier, 0.2f, 1.5f);
            Vector3 velocity = (tangent + correction).normalized * (ChaseSpeed * speedMul);
            UpdateCharacterMove(velocity * Time.deltaTime, Quaternion.identity);
            RotateToDirection(toTarget, aiData.rotateSpeed);

            float strafeSpeed = Mathf.Lerp(walkSpeedParameterValue, runSpeedParameterValue, 0.35f);
            UpdatePlayerSoLocomotionParameters(velocity, strafeSpeed);
        }

        private void RotateToDirection(Vector3 direction, float rotateSpeed)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                return;

            Quaternion targetRot = Quaternion.LookRotation(direction.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotateSpeed);
        }

        private void RotateToTarget(float rotateSpeed)
        {
            if (currentTarget == null)
                return;

            RotateToDirection(currentTarget.position - transform.position, rotateSpeed);
        }

        private void StopNavigation()
        {
            if (!CanUseNavMesh())
                return;

            navMeshAgent.ResetPath();
            navMeshAgent.velocity = Vector3.zero;
        }

        private Vector3 GetMoveDirectionForIntent(BossMoveIntent intent)
        {
            if (currentTarget == null)
                return ModelTransform.forward;

            Vector3 toTarget = currentTarget.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.0001f)
                toTarget = ModelTransform.forward;

            switch (intent)
            {
                case BossMoveIntent.Retreat:
                    return -toTarget.normalized;
                case BossMoveIntent.StrafeLeft:
                    return Vector3.Cross(Vector3.up, toTarget.normalized) * -1f;
                case BossMoveIntent.StrafeRight:
                    return Vector3.Cross(Vector3.up, toTarget.normalized);
                default:
                    return toTarget.normalized;
            }
        }

        private bool TryPlayPlayerSoIdle()
        {
            if (!UsePlayerSoMotion || bossMotionSource.playerMovementData?.PlayerIdleData == null)
                return false;

            TransitionAsset idle = bossMotionSource.playerMovementData.PlayerIdleData.idle;
            if (idle == null)
                return false;

            animancer.Play(idle);
            return true;
        }

        private bool TryPlayPlayerSoMoveStart(BossMoveIntent intent)
        {
            if (!UsePlayerSoMotion)
                return false;

            var movementData = bossMotionSource.playerMovementData;
            if (movementData?.PlayerMoveStartData == null)
                return false;

            Vector3 moveDirection = GetMoveDirectionForIntent(intent);
            TransitionAsset moveStart = PickMoveStartTransition(movementData.PlayerMoveStartData, moveDirection);
            if (moveStart == null)
                return false;

            AnimancerState state = animancer.Play(moveStart);
            if (state == null)
                return false;

            playingMoveStartTransition = true;
            state.Events(this).OnEnd = OnMoveStartTransitionEnd;
            return true;
        }

        private void OnMoveStartTransitionEnd()
        {
            playingMoveStartTransition = false;
            if (stateMachine == null || stateMachine.currentState != stateMachine.chaseState)
                return;

            TryPlayPlayerSoMoveLoop();
        }

        private bool TryPlayPlayerSoMoveLoop()
        {
            if (!UsePlayerSoMotion)
                return false;

            TransitionAsset moveLoop = bossMotionSource.playerMovementData?.PlayerMoveLoopData?.moveLoop;
            if (moveLoop == null)
                return false;

            animancer.Play(moveLoop);
            return true;
        }

        private TransitionAsset PickMoveStartTransition(PlayerMoveStartData moveStartData, Vector3 moveDirection)
        {
            if (moveStartData == null)
                return null;

            float angle = GetSignedAngleToDirection(moveDirection);
            if ((angle >= 0f && angle < 22.5f) || (angle < 0f && angle > -22.5f))
                return moveStartData.moveStart_F ?? moveStartData.moveStart;
            if (angle >= 22.5f && angle < 67.5f)
                return moveStartData.moveStart_R45 ?? moveStartData.moveStart;
            if (angle >= 67.5f && angle < 112.5f)
                return moveStartData.moveStart_R90 ?? moveStartData.moveStart;
            if (angle >= 112.5f && angle < 157.5f)
                return moveStartData.moveStart_R135 ?? moveStartData.moveStart;
            if (angle >= 157.5f || angle < -157.5f)
                return moveStartData.moveStart_R180 ?? moveStartData.moveStart;
            if (angle >= -157.5f && angle < -112.5f)
                return moveStartData.moveStart_L135 ?? moveStartData.moveStart;
            if (angle >= -112.5f && angle < -67.5f)
                return moveStartData.moveStart_L90 ?? moveStartData.moveStart;
            if (angle >= -67.5f && angle < -22.5f)
                return moveStartData.moveStart_L45 ?? moveStartData.moveStart;

            return moveStartData.moveStart;
        }

        #endregion

        #region State Runtime

        private void EnterIdle()
        {
            LogBossDecision("进入状态 Idle");
            ClearRetreatAfterAttack("进入Idle");
            ResetSimpleVigilant("进入Idle");
            playingMoveStartTransition = false;
            if (!TryPlayPlayerSoIdle())
                PlayClip(animationData.idle);

            StopNavigation();
            UpdateCharacterMove(Vector3.zero, Quaternion.identity);
            UpdatePlayerSoLocomotionParameters(Vector3.zero, 0f);
            DebugTickSnapshot("EnterIdle", true);
        }

        private void TickIdle()
        {
            RefreshTarget();
            if (UseGoapBrain)
            {
                if (currentTarget != null && moveIntent != BossMoveIntent.None)
                    stateMachine.ChangeState(stateMachine.chaseState);
                return;
            }

            if (currentTarget != null)
                stateMachine.ChangeState(stateMachine.chaseState);
        }

        private void EnterChase()
        {
            LogBossDecision($"进入状态 Chase (Intent={moveIntent})");
            if (retreatAfterAttackPending)
            {
                playingMoveStartTransition = false;
                StopNavigation();
                if (!TryPlayPlayerSoIdle())
                    PlayClip(animationData.idle);
                UpdatePlayerSoLocomotionParameters(Vector3.zero, 0f);
                DebugTickSnapshot("EnterChaseWaitRetreat", true);
                return;
            }

            if (!TryPlayPlayerSoMoveStart(moveIntent))
            {
                if (!TryPlayPlayerSoMoveLoop())
                    PlayClip(animationData.move);
            }
            DebugTickSnapshot("EnterChase", true);
        }

        private void TickChase()
        {
            RefreshTarget();
            if (currentTarget == null)
            {
                ClearRetreatAfterAttack("目标丢失");
                ResetSimpleVigilant("目标丢失");
                stateMachine.ChangeState(stateMachine.idleState);
                return;
            }

            if (TickPostAttackRetreatIfNeeded())
            {
                ResetSimpleVigilant("攻击后后撤优先");
                return;
            }

            if (isDodging)
            {
                RotateToTarget(aiData.rotateSpeed);
                return;
            }

            if (UseGoapBrain)
            {
                if (moveIntent == BossMoveIntent.Retreat)
                {
                    MoveAwayFromTarget();
                    return;
                }

                if (moveIntent == BossMoveIntent.StrafeLeft)
                {
                    MoveStrafeAroundTarget(false);
                    return;
                }

                if (moveIntent == BossMoveIntent.StrafeRight)
                {
                    MoveStrafeAroundTarget(true);
                    return;
                }

                if (moveIntent == BossMoveIntent.Chase)
                {
                    MoveTowardsTarget();
                    return;
                }

                StopNavigation();
                UpdatePlayerSoLocomotionParameters(Vector3.zero, 0f);
                return;
            }

            TickSimpleChaseDecision();
        }

        private void EnterAttack()
        {
            LogBossDecision("进入状态 Attack");
            ClearRetreatAfterAttack("进入Attack重置后撤计划");
            ResetSimpleVigilant("进入Attack");
            if (isDodging)
                EndDefensiveDodge();

            ClearPendingMindReadThreat();
            queuedMindReadTime = -1f;
            StopNavigation();
            RotateToTarget(aiData.rotateSpeed);

            isAttackPlaying = true;
            usingSkillAttack = false;
            nextAttackTime = Time.time + aiData.attackCooldown;
            UpdatePlayerSoLocomotionParameters(Vector3.zero, 0f);

            disableRootMotion = !aiData.allowAttackRootMotion;
            ignoreRootMotionY = true;

            if (TryReleaseSkillAttack())
            {
                usingSkillAttack = true;
                attackFallbackEndTime = Time.time + Mathf.Max(0.1f, aiData.attackFallbackTime);
                LogBossDecision("Attack决策：使用SkillBrain攻击");
                DebugTickSnapshot("AttackBySkill", true);
                return;
            }

            var attackState = PlayAttackClip();
            if (attackState != null)
            {
                float clipLen = Mathf.Max(0.1f, attackState.Length);
                attackFallbackEndTime = Time.time + clipLen + 0.05f;
                attackState.Events(this).OnEnd = OnAttackClipEnd;
                LogBossDecision($"Attack决策：播放普通攻击Clip={attackState}");
            }
            else
            {
                attackFallbackEndTime = Time.time + 0.1f;
                LogBossDecision("Attack决策：无可用攻击Clip，走fallback");
            }

            DebugTickSnapshot("EnterAttack", true);
        }

        private void TickAttack()
        {
            RefreshTarget(false);
            RotateToTarget(aiData.rotateSpeed);

            if (!isAttackPlaying)
            {
                stateMachine.ChangeState(currentTarget != null ? stateMachine.chaseState : stateMachine.idleState);
                return;
            }

            if (Time.time >= attackFallbackEndTime)
            {
                if (usingSkillAttack)
                    skillBrain?.InterruptCurrentSkill();
                OnAttackClipEnd();
            }
        }

        private void ExitAttack()
        {
            disableRootMotion = true;
            ignoreRootMotionY = true;

            if (usingSkillAttack)
                skillBrain?.InterruptCurrentSkill();

            usingSkillAttack = false;
            isAttackPlaying = false;
            ExitSkillMode();
        }

        private void OnAttackClipEnd()
        {
            if (!isAttackPlaying && !usingSkillAttack)
                return;

            isAttackPlaying = false;
            usingSkillAttack = false;
            ScheduleRetreatAfterAttack("Attack结束");
        }

        private AnimancerState PlayAttackClip()
        {
            if (animationData.attacks == null || animationData.attacks.Count == 0)
                return null;

            int count = animationData.attacks.Count;
            for (int i = 0; i < count; i++)
            {
                attackClipCursor = (attackClipCursor + 1) % count;
                var clip = animationData.attacks[attackClipCursor];
                if (clip != null && clip.Clip != null)
                    return animancer.Play(clip);
            }

            return null;
        }

        private bool TryReleaseSkillAttack()
        {
            if (skillBrain == null || skillBrain.SkillCount == 0)
                return false;

            int skillIndex = Mathf.Clamp(aiData.defaultAttackSkillIndex, 0, skillBrain.SkillCount - 1);
            if (!skillBrain.CheckReleaseSkill(skillIndex))
                return false;

            skillBrain.ReleaseSkill(skillIndex);
            return true;
        }

        private void EnterHit()
        {
            LogBossDecision("进入状态 Hit");
            ClearRetreatAfterAttack("进入Hit");
            ResetSimpleVigilant("进入Hit");
            hitEndTime = Time.time + Mathf.Max(0.01f, aiData.hitRecoverTime);
            isAttackPlaying = false;
            usingSkillAttack = false;
            EndDefensiveDodge();
            ClearPendingMindReadThreat();
            queuedMindReadTime = -1f;

            StopNavigation();
            ExitSkillMode();
            disableRootMotion = true;
            ignoreRootMotionY = true;
            UpdatePlayerSoLocomotionParameters(Vector3.zero, 0f);

            PlayClip(animationData.hit);
            DebugTickSnapshot("EnterHit", true);
        }

        private void TickHit()
        {
            RefreshTarget(false);
            if (Time.time >= hitEndTime)
            {
                stateMachine.ChangeState(currentTarget != null ? stateMachine.chaseState : stateMachine.idleState);
            }
        }

        private void ExitHit()
        {
        }

        private void EnterDead()
        {
            if (isDead)
                return;

            LogBossDecision("进入状态 Dead");
            isDead = true;
            ClearRetreatAfterAttack("进入Dead");
            ResetSimpleVigilant("进入Dead");
            SetMoveIntent(BossMoveIntent.None, "死亡清空意图");
            EndDefensiveDodge();
            ClearPendingMindReadThreat();
            queuedMindReadTime = -1f;
            StopNavigation();
            ExitSkillMode();
            goapAgent?.StopPlan();

            isAttackPlaying = false;
            usingSkillAttack = false;
            repelRemainTime = 0f;
            disableRootMotion = true;
            ignoreRootMotionY = true;
            UpdatePlayerSoLocomotionParameters(Vector3.zero, 0f);

            var deathState = PlayClip(animationData.death);
            if (deathState != null)
            {
                deathState.Events(this).OnEnd = OnDeadAnimationEnd;
            }
            else
            {
                OnDeadAnimationEnd();
            }

            DebugTickSnapshot("EnterDead", true);
        }

        private void OnDeadAnimationEnd()
        {
            if (navMeshAgent != null)
                navMeshAgent.enabled = false;

            if (controller != null)
                controller.enabled = false;
        }

        private AnimancerState PlayClip(ClipTransition clip)
        {
            if (clip == null || clip.Clip == null)
                return null;

            return animancer.Play(clip);
        }

        private void ApplyRepelMotion()
        {
            if (repelRemainTime <= 0f || isDead)
                return;

            repelRemainTime -= Time.deltaTime;
            Vector3 delta = Vector3.ProjectOnPlane(repelVelocity, Vector3.up) * Time.deltaTime;
            if (controller != null && controller.enabled)
                controller.Move(delta);

            float damp = 1f - Mathf.Exp(-10f * Time.deltaTime);
            repelVelocity = Vector3.Lerp(repelVelocity, Vector3.zero, damp);
        }

        private void ApplyRepel(AttackData attackData)
        {
            var hitConfig = attackData.detectionEvent?.AttackHitConfig;
            if (hitConfig == null || hitConfig.RepelTime <= 0f)
                return;

            Vector3 worldRepel = hitConfig.RepelStrength;
            if (attackData.source?.ModelTransform != null)
                worldRepel = attackData.source.ModelTransform.TransformDirection(hitConfig.RepelStrength);

            worldRepel.y = 0f;
            repelVelocity = worldRepel;
            repelRemainTime = hitConfig.RepelTime;
        }

        private bool TryStartPostAttackRetreatByAnimation()
        {
            if (!UsePlayerSoMotion ||
                bossMotionSource.playerMovementData == null ||
                isDead ||
                isDodging ||
                isAttackPlaying ||
                stateMachine.currentState == stateMachine.hitState ||
                stateMachine.currentState == stateMachine.deadState)
            {
                return false;
            }

            if (!TryGetPostAttackRetreatClip(
                    aiData.retreatAfterAttackAction,
                    out ClipTransition selectedClip,
                    out float cooldown,
                    out PostAttackRetreatActionType resolvedAction))
            {
                LogBossDecision(
                    $"攻击后后撤失败：PlayerSO未配置可用Back动作 (Prefer={aiData.retreatAfterAttackAction})");
                return false;
            }

            StopNavigation();
            SetMoveIntent(BossMoveIntent.None, "攻击后后撤动作执行");
            disableRootMotion = false;
            ignoreRootMotionY = true;

            AnimancerState dodgeState = animancer.Play(selectedClip);
            if (dodgeState == null)
            {
                disableRootMotion = true;
                ignoreRootMotionY = true;
                return false;
            }

            isDodging = true;
            nextEvasiveActionTime = Time.time + Mathf.Max(0.05f, cooldown);
            dodgeFallbackEndTime = Time.time + Mathf.Max(0.1f, dodgeState.Length + 0.05f);
            dodgeState.Events(this).OnEnd = EndDefensiveDodge;
            LogBossDecision(
                $"执行攻击后后撤: Action={resolvedAction}, Clip={selectedClip.Clip.name}, Cooldown={cooldown:F2}, End={dodgeFallbackEndTime:F2}");
            DebugTickSnapshot("PostAttackRetreat", true);
            return true;
        }

        private bool TryGetPostAttackRetreatClip(
            PostAttackRetreatActionType preferredAction,
            out ClipTransition selectedClip,
            out float cooldown,
            out PostAttackRetreatActionType resolvedAction)
        {
            selectedClip = null;
            cooldown = 0.2f;
            resolvedAction = preferredAction;

            PostAttackRetreatActionType firstChoice;
            PostAttackRetreatActionType secondChoice;
            PostAttackRetreatActionType thirdChoice;

            switch (preferredAction)
            {
                case PostAttackRetreatActionType.AvoidBackward:
                    firstChoice = PostAttackRetreatActionType.AvoidBackward;
                    secondChoice = PostAttackRetreatActionType.RollBackward;
                    thirdChoice = PostAttackRetreatActionType.SlideBackward;
                    break;

                case PostAttackRetreatActionType.SlideBackward:
                    firstChoice = PostAttackRetreatActionType.SlideBackward;
                    secondChoice = PostAttackRetreatActionType.AvoidBackward;
                    thirdChoice = PostAttackRetreatActionType.RollBackward;
                    break;

                case PostAttackRetreatActionType.RollBackward:
                    firstChoice = PostAttackRetreatActionType.RollBackward;
                    secondChoice = PostAttackRetreatActionType.AvoidBackward;
                    thirdChoice = PostAttackRetreatActionType.SlideBackward;
                    break;

                default:
                    firstChoice = PostAttackRetreatActionType.AvoidBackward;
                    secondChoice = PostAttackRetreatActionType.RollBackward;
                    thirdChoice = PostAttackRetreatActionType.SlideBackward;
                    break;
            }

            if (TryGetBackwardRetreatClip(firstChoice, out selectedClip, out cooldown))
            {
                resolvedAction = firstChoice;
                return true;
            }

            if (TryGetBackwardRetreatClip(secondChoice, out selectedClip, out cooldown))
            {
                resolvedAction = secondChoice;
                return true;
            }

            if (TryGetBackwardRetreatClip(thirdChoice, out selectedClip, out cooldown))
            {
                resolvedAction = thirdChoice;
                return true;
            }

            return false;
        }

        private bool TryGetBackwardRetreatClip(
            PostAttackRetreatActionType actionType,
            out ClipTransition clip,
            out float cooldown)
        {
            clip = null;
            cooldown = 0.2f;
            switch (actionType)
            {
                case PostAttackRetreatActionType.AvoidBackward:
                    clip = TryGetAvoidClip(BossEvasionDirection.Backward, out cooldown);
                    break;
                case PostAttackRetreatActionType.SlideBackward:
                    clip = TryGetSlideClip(BossEvasionDirection.Backward, out cooldown);
                    break;
                case PostAttackRetreatActionType.RollBackward:
                    clip = TryGetRollClip(BossEvasionDirection.Backward, out cooldown);
                    break;
            }

            return clip != null && clip.Clip != null;
        }

        private bool TryStartDefensiveDodge()
        {
            if (!UsePlayerSoMotion ||
                bossMotionSource.playerMovementData == null ||
                isDead ||
                isDodging ||
                Time.time < nextEvasiveActionTime ||
                !HasTarget ||
                stateMachine.currentState == stateMachine.attackState ||
                stateMachine.currentState == stateMachine.hitState ||
                stateMachine.currentState == stateMachine.deadState)
            {
                return false;
            }

            ClipTransition selectedClip = null;
            float cooldown = 0.5f;
            float distance = BrainDistanceToTarget();
            BossEvasionDirection direction = DecideDefensiveDirection();

            // 近距离更偏向 Roll，远一点优先 Avoid，保证“躲招但不神仙走位”。
            bool preferRoll = distance <= aiData.attackRange * 1.15f || UnityEngine.Random.value < 0.45f;
            if (preferRoll)
                selectedClip = TryGetRollClip(direction, out cooldown);

            if (selectedClip == null || selectedClip.Clip == null)
                selectedClip = TryGetAvoidClip(direction, out cooldown);

            if ((selectedClip == null || selectedClip.Clip == null) && !preferRoll)
                selectedClip = TryGetRollClip(direction, out cooldown);

            if (selectedClip == null || selectedClip.Clip == null)
                selectedClip = TryGetSlideClip(direction, out cooldown);

            if (selectedClip == null || selectedClip.Clip == null)
                return false;

            StopNavigation();
            SetMoveIntent(BossMoveIntent.None, "执行闪避前清空意图");
            disableRootMotion = false;
            ignoreRootMotionY = true;

            AnimancerState dodgeState = animancer.Play(selectedClip);
            if (dodgeState == null)
                return false;

            isDodging = true;
            ClearPendingMindReadThreat("闪避开始，消费读指令窗口");
            nextEvasiveActionTime = Time.time + Mathf.Max(0.05f, cooldown);
            dodgeFallbackEndTime = Time.time + Mathf.Max(0.1f, dodgeState.Length + 0.05f);
            dodgeState.Events(this).OnEnd = EndDefensiveDodge;
            LogBossDecision(
                $"执行防御闪避: Clip={selectedClip.Clip.name}, Dir={direction}, Cooldown={cooldown:F2}, End={dodgeFallbackEndTime:F2}");
            DebugTickSnapshot("StartDodge", true);
            return true;
        }

        private void EndDefensiveDodge()
        {
            if (!isDodging)
                return;

            isDodging = false;
            disableRootMotion = true;
            ignoreRootMotionY = true;
            dodgeFallbackEndTime = 0f;
            ResumeLocomotionAfterDodge();
            UpdatePlayerSoLocomotionParameters(Vector3.zero, 0f);
            LogBossDecision("闪避结束");
            DebugTickSnapshot("EndDodge", true);
        }

        private void ResumeLocomotionAfterDodge()
        {
            if (isDead || retreatAfterAttackPending || stateMachine == null || stateMachine.currentState != stateMachine.chaseState)
                return;

            playingMoveStartTransition = false;

            // 闪避结束后强制恢复追击循环动画，避免停留在Idle但仍被逻辑位移导致“飘着走”。
            if (TryPlayPlayerSoMoveLoop())
            {
                LogBossDecision("闪避结束：恢复追击动画 MoveLoop");
                return;
            }

            if (TryPlayPlayerSoMoveStart(moveIntent))
            {
                LogBossDecision($"闪避结束：恢复追击动画 MoveStart (Intent={moveIntent})");
                return;
            }

            PlayClip(animationData.move);
            LogBossDecision("闪避结束：恢复追击动画 FallbackMove");
        }

        private BossEvasionDirection DecideDefensiveDirection()
        {
            if (currentTarget == null)
                return BossEvasionDirection.Backward;

            float distance = BrainDistanceToTarget();
            float sideChance = distance > aiData.attackRange * 0.9f ? 0.7f : 0.45f;
            if (UnityEngine.Random.value < sideChance)
                return UnityEngine.Random.value < 0.5f ? BossEvasionDirection.Left : BossEvasionDirection.Right;

            return BossEvasionDirection.Backward;
        }

        private ClipTransition TryGetAvoidClip(BossEvasionDirection direction, out float cooldown)
        {
            cooldown = 0.3f;
            var data = bossMotionSource.playerMovementData?.PlayerAvoidData;
            if (data == null)
                return null;

            cooldown = Mathf.Max(0.05f, data.cooldown);
            return direction switch
            {
                BossEvasionDirection.Forward => data.avoidForward,
                BossEvasionDirection.Left => data.avoidLeft,
                BossEvasionDirection.Right => data.avoidRight,
                _ => data.avoidBackward,
            };
        }

        private ClipTransition TryGetSlideClip(BossEvasionDirection direction, out float cooldown)
        {
            cooldown = 0.35f;
            var data = bossMotionSource.playerMovementData?.PlayerSlideData;
            if (data == null)
                return null;

            cooldown = Mathf.Max(0.05f, data.cooldown);
            return direction switch
            {
                BossEvasionDirection.Forward => data.slideForward,
                BossEvasionDirection.Left => data.slideLeft,
                BossEvasionDirection.Right => data.slideRight,
                _ => data.slideBackward,
            };
        }

        private ClipTransition TryGetRollClip(BossEvasionDirection direction, out float cooldown)
        {
            cooldown = 0.5f;
            var data = bossMotionSource.playerMovementData?.PlayerRollData;
            if (data == null)
                return null;

            cooldown = Mathf.Max(0.05f, data.cooldown);
            return direction switch
            {
                BossEvasionDirection.Forward => data.rollForward,
                BossEvasionDirection.Left => data.rollLeft,
                BossEvasionDirection.Right => data.rollRight,
                _ => data.rollBackward,
            };
        }

        private void UpdateStrafeDirectionIfNeeded()
        {
            if (Time.time < nextStrafeSwitchTime)
                return;

            strafeSign = UnityEngine.Random.value < 0.5f ? -1 : 1;
            float min = Mathf.Max(0.05f, aiData.strafeSwitchIntervalMin);
            float max = Mathf.Max(min, aiData.strafeSwitchIntervalMax);
            nextStrafeSwitchTime = Time.time + UnityEngine.Random.Range(min, max);
        }

        #region Boss Brain API

        public void BrainRefreshTarget()
        {
            RefreshTarget();
        }

        public void BrainSetMoveIntentChase()
        {
            if (isDodging)
            {
                LogBossDecision("忽略追击意图：当前处于闪避");
                return;
            }

            SetMoveIntent(BossMoveIntent.Chase, "GOAP:追击");
            if (stateMachine.currentState == stateMachine.idleState)
                stateMachine.ChangeState(stateMachine.chaseState);
        }

        public void BrainSetMoveIntentRetreat()
        {
            if (isDodging)
            {
                LogBossDecision("忽略后撤意图：当前处于闪避");
                return;
            }

            SetMoveIntent(BossMoveIntent.Retreat, "GOAP:拉开距离");
            if (stateMachine.currentState == stateMachine.idleState)
                stateMachine.ChangeState(stateMachine.chaseState);
        }

        public void BrainSetMoveIntentStrafe()
        {
            if (isDodging || !HasTarget)
            {
                if (isDodging)
                    LogBossDecision("忽略环绕意图：当前处于闪避");
                return;
            }

            UpdateStrafeDirectionIfNeeded();
            SetMoveIntent(strafeSign >= 0 ? BossMoveIntent.StrafeRight : BossMoveIntent.StrafeLeft, "GOAP:环绕走位");
            if (stateMachine.currentState == stateMachine.idleState)
                stateMachine.ChangeState(stateMachine.chaseState);
        }

        public void BrainAdjustSpacingWhenWaiting()
        {
            if (isDodging || !HasTarget)
                return;

            if (BrainIsTooCloseToTarget())
                BrainSetMoveIntentRetreat();
            else if (BrainIsTooFarFromTarget())
                BrainSetMoveIntentChase();
            else
                BrainSetMoveIntentStrafe();
        }

        public void BrainClearMoveIntent()
        {
            if (!isDodging)
                SetMoveIntent(BossMoveIntent.None, "清空意图");
        }

        public bool IsAttackReadyForBrain()
        {
            return Time.time >= nextAttackTime &&
                   !isDead &&
                   !isDodging &&
                   stateMachine.currentState != stateMachine.hitState &&
                   stateMachine.currentState != stateMachine.deadState;
        }

        public bool BrainTryStartAttack()
        {
            if (!IsAttackReadyForBrain() || !HasTargetInAttackRange() || isDodging)
                return false;

            if (stateMachine.currentState == stateMachine.attackState)
                return true;

            SetMoveIntent(BossMoveIntent.None, "攻击前清空意图");
            stateMachine.ChangeState(stateMachine.attackState);
            LogBossDecision("GOAP决策：开始攻击");
            return true;
        }

        public bool BrainIsAttacking()
        {
            return stateMachine.currentState == stateMachine.attackState && isAttackPlaying;
        }

        public bool BrainTryStartDefensiveDodge()
        {
            return TryStartDefensiveDodge();
        }

        public bool BrainIsDodging()
        {
            return isDodging;
        }

        public bool BrainNeedRetreatForSpacing()
        {
            return BrainShouldRetreatForSpacing();
        }

        public bool BrainHasTargetInRange(float range)
        {
            return IsTargetInAttackRange(range);
        }

        public float BrainDistanceToTarget()
        {
            if (currentTarget == null)
                return float.MaxValue;

            return Mathf.Sqrt(GetPlanarSqrDistance(currentTarget.position, transform.position));
        }

        #endregion

        #endregion

        #region ICharacter

        public void OnHit(AttackData attackData)
        {
            if (isDead)
                return;

            ClearRetreatAfterAttack("受击打断");
            ResetSimpleVigilant("受击");
            SetMoveIntent(BossMoveIntent.None, "受击清空意图");
            EndDefensiveDodge();
            ClearPendingMindReadThreat();
            queuedMindReadTime = -1f;

            float damage = Mathf.Max(0f, attackData.attackValue);
            if (characterAttribute != null)
                characterAttribute.AddHp(-damage);

            ApplyRepel(attackData);

            if (characterAttribute != null && characterAttribute.currentHp <= 0f)
            {
                stateMachine.ChangeState(stateMachine.deadState);
                return;
            }

            // 死亡状态不再切回受击
            if (stateMachine.currentState != stateMachine.deadState)
                stateMachine.ChangeState(stateMachine.hitState);
        }

        public float GetAttackValue(SkillAttackDetectionEvent detectionEvent)
        {
            float baseAttack = characterAttribute != null ? characterAttribute.attack.Total : 1f;
            float multiply = 1f;
            if (detectionEvent?.AttackHitConfig != null)
                multiply = detectionEvent.AttackHitConfig.AttackMultiply;

            return baseAttack * multiply;
        }

        public void OnSkillRotate()
        {
            RotateToTarget(aiData.rotateSpeed);
        }

        public void AddBuff(BuffConfig buffConfig, int stack)
        {
            RayDebug.Warn($"[EnemyController] AddBuff 尚未接入敌人Buff系统: {buffConfig?.name}");
        }

        public void CreateWeapon(int slotIndex, GameObject weaponPrefab)
        {
            weaponSlotManager?.CreateWeapon(slotIndex, weaponPrefab);
        }

        public void DestroyWeapon(int slotIndex)
        {
            weaponSlotManager?.DestroyWeapon(slotIndex);
        }

        public void Change2IdleState()
        {
            if (isDead)
                return;

            if (stateMachine.currentState == stateMachine.attackState && isAttackPlaying)
            {
                LogBossDecision("忽略Change2IdleState：攻击尚未结束，避免攻击中飘移");
                return;
            }

            ClearRetreatAfterAttack("技能切Idle");
            ResetSimpleVigilant("技能结束切Idle");
            SetMoveIntent(BossMoveIntent.None, "切回Idle前清空意图");
            EndDefensiveDodge();
            ClearPendingMindReadThreat();
            queuedMindReadTime = -1f;
            isAttackPlaying = false;
            usingSkillAttack = false;
            ExitSkillMode();
            UpdatePlayerSoLocomotionParameters(Vector3.zero, 0f);

            if (currentTarget != null && !IsOutOfLoseRange(currentTarget.position))
                stateMachine.ChangeState(stateMachine.chaseState);
            else
                stateMachine.ChangeState(stateMachine.idleState);
        }

        public void OnSkillMove(Vector3 deltaPos)
        {
            if (controller != null && controller.enabled)
                controller.Move(deltaPos);
        }

        public void OnSkillRotate(Quaternion deltaRot)
        {
            transform.rotation = deltaRot * transform.rotation;
        }

        public void EnterSkillMode(bool upperBody)
        {
            if (isInSkill && currentSkillUpperBody == upperBody)
                return;

            isInSkill = true;
            currentSkillUpperBody = upperBody;

            var baseLayer = animancer.Layers[0];
            var skillLayer = SkillLayer;
            skillLayer.IsAdditive = false;
            skillLayer.Mask = upperBody ? upperBodyMask : null;

            baseLayer.Stop();
            skillLayer.SetWeight(1f);
            baseLayer.SetWeight(upperBody ? 1f : 0f);
        }

        public void ExitSkillMode()
        {
            if (!isInSkill)
                return;

            isInSkill = false;
            currentSkillUpperBody = false;
            ClearSkillRootMotion();

            var baseLayer = animancer.Layers[0];
            var skillLayer = SkillLayer;
            var skillState = skillLayer.CurrentState;
            if (skillState != null)
                skillState.IsPlaying = false;

            skillLayer.StartFade(0f, skillLayerFadeOut);
            baseLayer.StartFade(1f, skillLayerFadeOut);
        }

        public void SetSkillRootMotion(Action<Vector3, Quaternion> handler, bool applyRootMotion)
        {
            if (applyRootMotion && handler != null)
                SetRootMotionMode(RootMotionMode.Custom, handler);
            else
                SetRootMotionMode(RootMotionMode.Suppressed, null);
        }

        public void ClearSkillRootMotion()
        {
            SetRootMotionMode(RootMotionMode.Default, null);
        }

        private void CleanupFinishedSkillLayer()
        {
            if (isInSkill)
                return;

            var skillLayer = SkillLayer;
            if (skillLayer.Weight > 0f || skillLayer.CurrentState == null)
                return;

            skillLayer.Stop();
        }

        #endregion

        #region State Objects

        private sealed class EnemyStateMachine : StateMachineBase
        {
            public readonly IdleState idleState;
            public readonly ChaseState chaseState;
            public readonly AttackState attackState;
            public readonly HitState hitState;
            public readonly DeadState deadState;

            public EnemyStateMachine(EnemyController owner)
            {
                idleState = new IdleState(owner);
                chaseState = new ChaseState(owner);
                attackState = new AttackState(owner);
                hitState = new HitState(owner);
                deadState = new DeadState(owner);
            }
        }

        private abstract class EnemyStateBase : IState
        {
            protected readonly EnemyController enemy;

            protected EnemyStateBase(EnemyController enemy)
            {
                this.enemy = enemy;
            }

            public virtual void OnEnter() { }
            public virtual void OnUpdate() { }
            public virtual void OnAnimationUpdate() { }
            public virtual void OnExit() { }
            public virtual void OnAnimationEnd() { }
        }

        private sealed class IdleState : EnemyStateBase
        {
            public IdleState(EnemyController enemy) : base(enemy) { }

            public override void OnEnter() => enemy.EnterIdle();
            public override void OnUpdate() => enemy.TickIdle();
        }

        private sealed class ChaseState : EnemyStateBase
        {
            public ChaseState(EnemyController enemy) : base(enemy) { }

            public override void OnEnter() => enemy.EnterChase();
            public override void OnUpdate() => enemy.TickChase();
        }

        private sealed class AttackState : EnemyStateBase
        {
            public AttackState(EnemyController enemy) : base(enemy) { }

            public override void OnEnter() => enemy.EnterAttack();
            public override void OnUpdate() => enemy.TickAttack();
            public override void OnExit() => enemy.ExitAttack();
        }

        private sealed class HitState : EnemyStateBase
        {
            public HitState(EnemyController enemy) : base(enemy) { }

            public override void OnEnter() => enemy.EnterHit();
            public override void OnUpdate() => enemy.TickHit();
            public override void OnExit() => enemy.ExitHit();
        }

        private sealed class DeadState : EnemyStateBase
        {
            public DeadState(EnemyController enemy) : base(enemy) { }

            public override void OnEnter() => enemy.EnterDead();
        }

        #endregion

        private void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmos || aiData == null)
                return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, aiData.detectRadius);

            Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
            Gizmos.DrawWireSphere(transform.position, aiData.loseTargetRadius);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, aiData.attackRange);

            Gizmos.color = new Color(0.3f, 0.8f, 1f, 1f);
            Gizmos.DrawWireSphere(transform.position, GetSafePreferredMinRange());

            Gizmos.color = new Color(0.2f, 0.5f, 1f, 1f);
            Gizmos.DrawWireSphere(transform.position, GetSafePreferredMaxRange());

            if (mindReading != null && mindReading.enable)
            {
                Gizmos.color = new Color(1f, 0.2f, 0.8f, 0.75f);
                Gizmos.DrawWireSphere(transform.position, Mathf.Max(0f, mindReading.triggerRange));
            }

            if (currentTarget != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position + Vector3.up, currentTarget.position + Vector3.up);
            }
        }
    }
}
