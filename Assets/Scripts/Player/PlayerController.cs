using Animancer;
using Arch.Core;
using Arch.Core.Extensions;
using Attribute;
using Battle.ECS;
using Battle.ECS.Core.Helper;
using Cinemachine;
using Config;
using JKFrame;
using Manager;
using RayPlayerState;
using UnityEngine;

namespace RayPlayer
{
    public class PlayerController : CharacterControllerBase, IStateMachineOwner, ICharacter
    {
        public Entity PlayerEntity { get; private set; }

        [Header("View")]
        [SerializeField] private PlayerView playerView;
        
        [Header("Combat")]
        [SerializeField] private PlayerSkillBrainBase skillBrain;
        [SerializeField] private PlayerSkillInput skillInput;
        [SerializeField] private CharacterAttribute characterAttribute;
        [SerializeField] private WeaponSlotManager weaponSlotManager;
        
        [Header("Camera")]
        [SerializeField] private CameraController cameraController;

        [Header("Generic Locomotion")]
        [SerializeField] private GenericPlayerLocomotionController genericLocomotionController;

        [Header("Footstep")]
        [SerializeField, Range(0f, 1f)] private float footstepVolume = 1f;
        [SerializeField, Min(0f)] private float footstepRayDistance = 1.6f;
        [SerializeField] private Vector3 footstepRayOffset = new Vector3(0f, 0.2f, 0f);
        [SerializeField, Min(0f)] private float footstepMinInterval = 0.06f;
        
        [Header("Animancer Skill Layer")]
        [SerializeField] private int skillLayerIndex = 1;
        [SerializeField] private AvatarMask upperBodyMask;
        // [SerializeField, Range(0f, 0.3f)] private float skillLayerFadeIn = 0.08f;
        [SerializeField, Range(0f, 0.3f)] private float skillLayerFadeOut = 0.1f;
        
        public PlayerSkillBrainBase SkillBrain => skillBrain;
        public CharacterAttribute CharacterAttribute => characterAttribute;
        // CharacterConfig / PlayerSO / WalkSpeed / RunSpeed / RotateSpeed 由基类提供

        public AnimancerComponent Animancer => animancer;
        public AnimancerLayer SkillLayer => animancer.Layers[skillLayerIndex];
        public float SkillLayerFadeOut => skillLayerFadeOut;
        public PlayerSkillInput SkillInput => skillInput;

        public Transform ModelTransform => playerView != null ? playerView.transform : transform;

        public InputService InputService { get; private set; }
        public TimerService TimerService { get; private set; }
        public Transform CameraTransform { get; private set; }

        public PlayerReusableData ReusableData { get; private set; }
        public PlayerReusableLogic ReusableLogic { get; private set; }
        public PlayerStateMachine MovementStateMachine { get; private set; }

        public bool IsPlayerControlled => true;

        private bool inSkill;
        private bool currentSkillUpperBody;
        private bool useGenericLocomotion;
        private float lastFootstepTime = -999f;

        protected override void Awake()
        {
            base.Awake();

            InputService = InputService.Instance;
            TimerService = TimerService.Instance;
            CameraTransform = Camera.main != null ? Camera.main.transform : null;
        }
        
        public override void Init(CharacterConfig characterConfig)
        {
            if (characterConfig == null) return;
            
            base.Init(characterConfig);
            
            useGenericLocomotion = characterConfig != null && characterConfig.GenericLocomotionConfig != null;
            RayDebug.Log($"Init -> character={characterConfig?.name}, useGeneric={useGenericLocomotion}, genericConfig={characterConfig?.GenericLocomotionConfig?.name}");

            if (useGenericLocomotion)
                SetupGenericLocomotion(characterConfig.GenericLocomotionConfig);
            else
                SetupDefaultLocomotion();

            if (!useGenericLocomotion && playerSO == null)
            {
                RayDebug.Error("PlayerSO is null for non-generic locomotion.");
                return;
            }
            
            playerView?.Init();
            
            characterAttribute.Init(characterConfig, characterConfig.hpBaseValue, characterConfig.mpBaseValue);

            // 从存档读取等级，并应用成长曲线（有成长配置时生效）
            if (characterConfig.LevelGrowthConfig != null)
            {
                var progress = DataManager.GetOrCreateProgressData(DataManager.GameData.SelectedCharacterId);
                int currentLevel = progress?.Level ?? 1;
                characterAttribute.ApplyLevel(currentLevel, characterConfig, characterConfig.LevelGrowthConfig);

                // 恢复存档中的当前血量（-1 表示满状态，直接用 maxHp.Total）
                if (progress != null)
                {
                    float savedHp = progress.CurrentHp > 0f ? progress.CurrentHp : characterAttribute.maxHp.Total;
                    float savedMp = progress.CurrentMp > 0f ? progress.CurrentMp : characterAttribute.maxMp.Total;
                    characterAttribute.SetHp(savedHp);
                    characterAttribute.SetMp(savedMp);
                }

                // 升级时自动刷新属性（UI 层也会收到此事件）
                DataManager.OnLevelUp += OnCharacterLevelUp;
            }

            // 订阅血量变化事件，实时写入存档（不额外触发 SaveGameData，由现有存档时机统一落盘）
            characterAttribute.OnHpChanged += OnHpChangedSave;
            characterAttribute.OnMpChanged += OnMpChangedSave;


            if (!useGenericLocomotion)
            {
                ReusableData = new PlayerReusableData(animancer, playerSO);
                ReusableLogic = new PlayerReusableLogic(this);
                MovementStateMachine = new PlayerStateMachine(this);
                MovementStateMachine.ChangeState(MovementStateMachine.idleState);
                
                if (skillBrain != null)
                    skillBrain.Init(this, DataManager.GetCurrentCharacterSkills());
                skillInput?.Init();
                
                SetupAnimancerLayers();
            }
            else
            {
                ReusableData = null;
                ReusableLogic = null;
                MovementStateMachine = null;
            }
            
            // 刷新武器槽位索引
            weaponSlotManager?.RefreshSlots();

            var context = BattleEcsRunner.Instance.Context;
            if (context != null)
            {
                // 注入飘字服务（UI层 → Battle层，单向依赖，通过接口解耦）
                if (context.DamageNumberService == null && DamageNumberManager.Instance != null)
                    context.DamageNumberService = DamageNumberManager.Instance;
                
                PlayerEntity = BattleEcsRunner.Instance.RegisterCharacter(this);
                RayDebug.Log($"ECS实体已创建: Entity ID = {PlayerEntity.Id}");
            }
        }

        public void RegisterCamera(CameraController cameraController)
        {
            this.cameraController = cameraController;
        }

        private void SetupDefaultLocomotion()
        {
            disableGravity = false;
            disableRootMotion = false;
            ignoreRootMotionY = false;
            applyFullRootMotion = false;

            if (genericLocomotionController != null)
                genericLocomotionController.enabled = false;

            if (animancer != null && !animancer.enabled)
                animancer.enabled = true;
        }

        private void SetupGenericLocomotion(GenericLocomotionConfig genericConfig)
        {
            if (genericConfig == null)
                return;

            disableGravity = true;
            disableRootMotion = true;
            ignoreRootMotionY = true;
            applyFullRootMotion = false;

            if (genericLocomotionController == null)
                genericLocomotionController = GetComponent<GenericPlayerLocomotionController>();
            if (genericLocomotionController == null)
                genericLocomotionController = gameObject.AddComponent<GenericPlayerLocomotionController>();

            // Generic 动画走 AnimatorController，不再让 Animancer 输出姿态，避免出现 T-Pose 互相覆盖。
            if (animancer != null && animancer.enabled)
                animancer.enabled = false;

            genericLocomotionController.Initialize(genericConfig, CameraTransform);
            genericLocomotionController.enabled = true;
            RayDebug.Log($"[PlayerController] Generic locomotion active -> controller={animator.runtimeAnimatorController?.name}, avatar={animator.avatar?.name}, animancerEnabled={animancer != null && animancer.enabled}");
        }

        private void SetupAnimancerLayers()
        {
            SetupAnimancerLayers(skillLayerIndex);
        }

        protected override void Update()
        {
            if (useGenericLocomotion)
                return;

            base.Update();
            MovementStateMachine?.OnUpdate();
            HandleSkillInput();
            CleanupFinishedSkillLayer();
        }

        protected override void OnAnimatorMove()
        {
            if (useGenericLocomotion)
                return;

            base.OnAnimatorMove();
            MovementStateMachine?.OnAnimationUpdate();
        }

        public void AnimationEnd()
        {
            MovementStateMachine?.OnAnimationEnd();
        }

        private void HandleSkillInput()
        {
            if (skillBrain == null || skillInput == null)
                return;

            if (UISystem.CheckMouseOnUI())
                return;

            // 已在技能状态，连击由 PlayerSkillState.HandleCombatInput() 负责
            if (MovementStateMachine?.currentState == MovementStateMachine?.skillState)
                return;

            for (int i = 0; i < skillBrain.SkillCount; i++)
            {
                bool valid = false;
                int skillIndex = skillBrain.GetSkillIndex(i);

                if (i == 0)
                {
                    valid = skillInput.GetBasicAttackState() && skillBrain.CheckReleaseSkill(i);
                    if (valid)
                        skillInput.ResetBasicBuffer();
                }

                if (!valid)
                {
                    valid = skillInput.GetSkillState(skillIndex) && skillBrain.CheckReleaseSkill(i);
                    if (valid)
                        skillInput.ResetSkillBuffer(skillIndex);
                }

                if (valid)
                {
                    // 先切状态机：OnEnter → EnterSkillMode → Layer1 SetWeight(1)
                    // 后触发技能：ReleaseSkill 向 Layer1 播动画时，Layer1 已在 weight 1，避免警告
                    MovementStateMachine.ChangeState(MovementStateMachine.skillState);
                    skillBrain.ReleaseSkill(i);
                    return;
                }
            }
        }

        public void EnterSkillMode(bool upperBody)
        {
            if (inSkill && currentSkillUpperBody == upperBody)
                return;

            inSkill = true;
            currentSkillUpperBody = upperBody;
            base.EnterSkillMode(upperBody, skillLayerIndex, upperBodyMask);
        }

        public void ExitSkillMode()
        {
            if (!inSkill)
                return;

            inSkill = false;
            currentSkillUpperBody = false;
            base.ExitSkillMode(skillLayerIndex, skillLayerFadeOut);
        }

        private void CleanupFinishedSkillLayer()
        {
            if (inSkill) return;
            base.CleanupFinishedSkillLayer(skillLayerIndex);
        }
        
        public void ChangeState(PlayerState newState, bool reCurrstate = false)
        {
            if (MovementStateMachine == null)
                return;

            switch (newState)
            {
                case PlayerState.Idle:
                    MovementStateMachine.ChangeState(MovementStateMachine.idleState);
                    ExitSkillMode();
                    break;
                case PlayerState.Move:
                    MovementStateMachine.ChangeState(MovementStateMachine.moveStartState);
                    ExitSkillMode();
                    break;
                case PlayerState.Skill:
                    break;
                case PlayerState.Hurt:
                    ExitSkillMode();
                    MovementStateMachine.ChangeState(MovementStateMachine.hurtState);
                    break;
                case PlayerState.Dead:
                    ExitSkillMode();
                    MovementStateMachine.ChangeState(MovementStateMachine.deadState);
                    break;
            }
        }

        public void Rotate(Vector3 inputDir, float rotateSpeed = 0f)
        {
            if (rotateSpeed == 0f) rotateSpeed = RotateSpeed;
            if (CameraTransform == null) return;
            // 获取相机的旋转值
            float y = CameraTransform.rotation.eulerAngles.y;
            // 让input也旋转y角度
            Vector3 moveDir = Quaternion.Euler(0f, y, 0f) * inputDir;
            // 处理旋转
            
            // ModelTransform.rotation = Quaternion.Slerp(ModelTransform.rotation,
            //     Quaternion.LookRotation(moveDir), Time.deltaTime * rotateSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(moveDir), Time.deltaTime * rotateSpeed);
        }

        public void AddBuff(BuffConfig buffConfig, int stack)
        {
            BuffHelper.AddBuff(BattleEcsRunner.Instance.Context, nameof(PlayerController), PlayerEntity, PlayerEntity, buffConfig, stack);
        }

        public void CreateWeapon(int slotIndex, GameObject weaponPrefab)
        {
            weaponSlotManager.CreateWeapon(slotIndex, weaponPrefab);
        }

        public void DestroyWeapon(int slotIndex)
        {
            weaponSlotManager.DestroyWeapon(slotIndex);
        }

        public void OnHit(AttackData attackData)
        {
            // 1. 发射 ECS 伤害请求
            if (PlayerEntity.IsAlive())
            {
                DamageHelper.EmitDamage(PlayerEntity, attackData, transform.position);
            }

            // 2. 无敌帧期间不进入受伤状态
            if (ReusableData != null && ReusableData.isInvincible)
                return;

            // 3. 记录受击方向并切换到受伤状态
            if (ReusableData != null)
            {
                Vector3 hitDir = attackData.hitPoint - transform.position;
                hitDir.y = 0f;
                if (hitDir.sqrMagnitude < 0.0001f)
                    hitDir = -transform.forward; // 默认从正前方受击
                ReusableData.lastHitDirection = hitDir.normalized;
            }

            ChangeState(PlayerState.Hurt);
            RayDebug.Log($"玩家被命中！伤害值: {attackData.attackValue}");
        }

        /// <summary>
        /// 由 DeathSystem 通过 IDeathCallback 接口调用
        /// </summary>
        public void OnDeath()
        {
            RayDebug.Info("[PlayerController] 玩家死亡！");
            
            // 死亡瞬间关闭所有碰撞体，防止在死亡动画期间发生发生攻击判定
            var colliders = GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }

            ChangeState(PlayerState.Dead);
        }

        public float GetAttackValue(SkillAttackDetectionEvent detectionEvent)
        {
            return characterAttribute.attack.Total * detectionEvent.AttackHitConfig.AttackMultiply;
        }

        public void OnSkillRotate()
        {
            Vector2 moveInput = InputService.Move;
            if (moveInput.x != 0 || moveInput.y != 0)
            {
                Rotate(new Vector3(moveInput.x, 0f, moveInput.y));
            }
        }
        
        public void TryReleaseBasicAttack()
        {
            if (skillBrain == null || MovementStateMachine == null) return;
            // 如果已经在放技能则不再触发，连击由内部状态机控制
            if (MovementStateMachine.currentState == MovementStateMachine.skillState) return;

            // 0号通常是普攻
            if (skillBrain.CheckReleaseSkill(0))
            {
                MovementStateMachine.ChangeState(MovementStateMachine.skillState);
                skillBrain.ReleaseSkill(0);
            }
        }

        public void TryReleaseSkillBySkillIndex(int skillIndex)
        {
            if (skillBrain == null || MovementStateMachine == null || skillIndex < 0) return;
            if (MovementStateMachine.currentState == MovementStateMachine.skillState) return;

            for (int i = 0; i < skillBrain.SkillCount; i++)
            {
                if (skillBrain.GetSkillIndex(i) == skillIndex)
                {
                    if (skillBrain.CheckReleaseSkill(i))
                    {
                        MovementStateMachine.ChangeState(MovementStateMachine.skillState);
                        skillBrain.ReleaseSkill(i);
                    }
                    return;
                }
            }
        }

        public void Change2IdleState()
        {
            if (MovementStateMachine == null)
                return;

            // 技能自然结束：ExitSkillMode 先执行（Layer0淡入），等淡出完成后再切换状态机
            if (MovementStateMachine.currentState == MovementStateMachine.skillState)
            {
                MovementStateMachine.skillState.NotifySkillEnd();
                return;
            }

            // 保底：不在技能状态时直接退出（外部非预期调用的兜底）
            ExitSkillMode();
        }
        
        /// <summary>
        /// 绑定角色模型层
        /// </summary>
        /// <param name="model"></param>
        public void BindModel(GameObject model)
        {
            playerView = model.GetComponent<PlayerView>();
            var vcam = cameraController != null ? cameraController.GetComponent<CinemachineVirtualCamera>() : null;
            if (vcam != null && playerView != null)
            {
                vcam.Follow = playerView.LookAt;
                vcam.LookAt = playerView.LookAt;
            }
        }

        /// <summary>
        /// 角色升级回调：刷新属性并通知 UI（事件由 DataManager.AddExperience 触发）。
        /// </summary>
        private void OnCharacterLevelUp(int characterId, int newLevel)
        {
            if (characterId != DataManager.GameData?.SelectedCharacterId) return;
            if (characterConfig?.LevelGrowthConfig == null) return;

            characterAttribute.ApplyLevel(newLevel, characterConfig, characterConfig.LevelGrowthConfig);
            RayDebug.Info($"[PlayerController] 升级！当前等级: {newLevel}");
            // TODO: 通知 UI 播放升级特效 / 刷新属性面板
        }

        protected void OnDestroy()
        {
            DataManager.OnLevelUp -= OnCharacterLevelUp;
            characterAttribute.OnHpChanged -= OnHpChangedSave;
            characterAttribute.OnMpChanged -= OnMpChangedSave;
        }

        private void OnHpChangedSave(float current, float max)
        {
            if (DataManager.GameData == null) return;
            var progress = DataManager.GetOrCreateProgressData(DataManager.GameData.SelectedCharacterId);
            if (progress != null) progress.CurrentHp = current;
        }

        private void OnMpChangedSave(float current, float max)
        {
            if (DataManager.GameData == null) return;
            var progress = DataManager.GetOrCreateProgressData(DataManager.GameData.SelectedCharacterId);
            if (progress != null) progress.CurrentMp = current;
        }

        public void PlayFootSound()
        {
            TryPlayFootstepSound(characterConfig.FootstepAudioSet);
        }

        public void PlayFootEndSound()
        {
            TryPlayFootstepSound(characterConfig.FootstepEndAudioSet);
        }

        private void TryPlayFootstepSound(FootstepSurfaceAudioSet audioSet)
        {
            if (footstepMinInterval > 0f && Time.time - lastFootstepTime < footstepMinInterval)
                return;

            if (!TryGetFootstepSurface(out var surfaceType, out var hitPosition))
                return;

            AudioClip[] clips = audioSet.GetClips(surfaceType);

            if (clips == null || clips.Length == 0)
            {
                RayDebug.Error($"没有 {surfaceType} 对应的脚步声资源！");
                return;
            }
            
            var clip = clips[UnityEngine.Random.Range(0, clips.Length)];
            lastFootstepTime = Time.time;
            AudioSystem.PlayOneShot(clip, hitPosition, false, footstepVolume);
        }

        private bool TryGetFootstepSurface(out FootstepSurfaceType surfaceType, out Vector3 hitPosition)
        {
            surfaceType = FootstepSurfaceType.Default;
            hitPosition = transform.position;

            Vector3 origin = GetFootstepRayOrigin();
            if (!Physics.Raycast(origin, Vector3.down, out var hit, footstepRayDistance, whatIsGround,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            hitPosition = hit.point;
            var surface = hit.collider.GetComponentInParent<FootstepSurface>();
            if (surface != null)
                surfaceType = surface.SurfaceType;

            return true;
        }

        private Vector3 GetFootstepRayOrigin()
        {
            if (controller != null)
            {
                var bounds = controller.bounds;
                return bounds.center + Vector3.down * bounds.extents.y + footstepRayOffset;
            }

            return transform.position + footstepRayOffset;
        }
    }
}
