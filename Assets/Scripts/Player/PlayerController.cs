using System;
using System.Threading;
using Animancer;
using Arch.Core;
using Arch.Core.Extensions;
using Attribute;
using Battle.ECS;
using Battle.ECS.Component;
using Battle.ECS.Core.Helper;
using Cinemachine;
using Config;
using Data;
using GOAP;
using JKFrame;
using Manager;
using RayAnimation;
using RayPlayerState;
using UnityEngine;

namespace RayPlayer
{
    public class PlayerController : CharacterControllerBase, IStateMachineOwner, ICharacter, IGOAPOwner
    {
        public Entity PlayerEntity { get; private set; }
        [Header("Config")]
        [SerializeField] private CharacterConfig characterConfig;
        [SerializeField] public PlayerSO playerSO;

        [Header("View")]
        [SerializeField] private PlayerView playerView;
        
        [Header("Combat")]
        [SerializeField] private PlayerSkillBrainBase skillBrain;
        [SerializeField] private PlayerSkillInput skillInput;
        [SerializeField] private CharacterAttribute characterAttribute;
        [SerializeField] private WeaponSlotManager weaponSlotManager;
        
        [Header("Camera")]
        [SerializeField] private CameraController cameraController;
        
        [Header("Animancer Skill Layer")]
        [SerializeField] private int skillLayerIndex = 1;
        [SerializeField] private AvatarMask upperBodyMask;
        [SerializeField, Range(0f, 0.3f)] private float skillLayerFadeIn = 0.08f;
        [SerializeField, Range(0f, 0.3f)] private float skillLayerFadeOut = 0.1f;
        
        public PlayerSkillBrainBase SkillBrain => skillBrain;
        public CharacterAttribute CharacterAttribute => characterAttribute;
        public CharacterConfig CharacterConfig => characterConfig;

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
        
        public float WalkSpeed => characterConfig.WalkSpeed;
        public float RunSpeed => characterConfig.RunSpeed;
        public float RotateSpeed => characterConfig.RotateSpeed;

        private bool inSkill;
        private bool currentSkillUpperBody;

        protected override void Awake()
        {
            base.Awake();

            InputService = InputService.Instance;
            TimerService = TimerService.Instance;
            CameraTransform = Camera.main != null ? Camera.main.transform : null;
        }
        
        public void Init(CharacterConfig characterConfig, GameData gameData)
        {
            this.characterConfig = characterConfig;
            if (playerSO == null && characterConfig != null)
                playerSO = characterConfig.PlayerSO;
            if (characterConfig != null)
            {
                ApplyControllerProfile(characterConfig.ControllerProfile);
                if (characterConfig.Avatar != null)
                    animator.avatar = characterConfig.Avatar;
            }
            
            playerView?.Init();
            
            characterAttribute.Init(characterConfig, characterConfig.hpBaseValue, characterConfig.mpBaseValue);

            ReusableData = new PlayerReusableData(animancer, playerSO);
            ReusableLogic = new PlayerReusableLogic(this);
            MovementStateMachine = new PlayerStateMachine(this);
            MovementStateMachine.ChangeState(MovementStateMachine.idleState);
            
            skillBrain.Init(this, DataManager.GetCurrentCharacterSkills());
            skillInput?.Init();
            
            SetupAnimancerLayers();
            
            // 刷新武器槽位索引
            weaponSlotManager.RefreshSlots();
            // 让Cinemachine看向这个player
            var vcam = cameraController != null ? cameraController.GetComponent<CinemachineVirtualCamera>() : null;
            if (vcam != null && playerView != null)
            {
                vcam.Follow = playerView.LookAt;
                vcam.LookAt = playerView.LookAt;
            }

            var context = BattleEcsRunner.Instance.Context;
            if (context != null)
            {
                PlayerEntity = BattleEcsRunner.Instance.RegisterPlayer(this);
                RayDebug.Log($"ECS实体已创建: Entity ID = {PlayerEntity.Id}");
            }
        }

        private void SetupAnimancerLayers()
        {
            var layer = animancer.Layers[skillLayerIndex];
            layer.SetWeight(0f);
            layer.IsAdditive = false;
        }

        protected override void Update()
        {
            base.Update();
            MovementStateMachine?.OnUpdate();
            HandleSkillInput();
            CleanupFinishedSkillLayer();
        }

        protected override void OnAnimatorMove()
        {
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

            var baseLayer = animancer.Layers[0];
            var skillLayer = animancer.Layers[skillLayerIndex];

            skillLayer.IsAdditive = false;
            skillLayer.Mask = upperBody ? upperBodyMask : null;

            // 清除 Layer0 残留状态，防止返回技能后 Mixer 内部权重不等于 1 的警告
            // Layer0 此刻即将降权至 0（全身技能），Stop 后不可见，安全
            baseLayer.Stop();

            // 立即切权重，避免渐变期间"no override layers at weight 1"警告
            // 攻击动画的打击感依赖第一帧即可见，立即生效比 0.08s 淡入更直接
            skillLayer.SetWeight(1f);
            baseLayer.SetWeight(upperBody ? 1f : 0f);
        }

        public void ExitSkillMode()
        {
            if (!inSkill)
                return;

            inSkill = false;
            currentSkillUpperBody = false;
            ClearSkillRootMotion();

            var baseLayer = animancer.Layers[0];
            var skillLayer = animancer.Layers[skillLayerIndex];

            // 冻结当前技能动画，防止它在淡出期间继续播放或循环
            // 这避免了动画Loop回到第0帧，产生错误的过渡姿势
            var skillState = skillLayer.CurrentState;
            if (skillState != null)
            {
                skillState.IsPlaying = false;  // 暂停动画，保持在当前帧
            }

            // 只淡出层权重即可，层权重到 0 后 CleanupFinishedSkillLayer 会停止动画
            skillLayer.StartFade(0f, skillLayerFadeOut);
            baseLayer.StartFade(1f, skillLayerFadeOut);
        }

        /// <summary>
        /// 当 skillLayer 权重淡出完成后，停止层上所有动画状态
        /// 防止技能动画在不可见时持续运行（浪费性能 + 导致下次播放从错误位置开始）
        /// </summary>
        private void CleanupFinishedSkillLayer()
        {
            if (inSkill)
                return;

            var skillLayer = animancer.Layers[skillLayerIndex];
            if (skillLayer.Weight > 0f || skillLayer.CurrentState == null)
                return;

            skillLayer.Stop();
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
                Battle.ECS.Core.Helper.DamageHelper.EmitDamage(PlayerEntity, attackData, transform.position);
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

        public void OnSkillMove(Vector3 deltaPos)
        {
            controller.Move(deltaPos);
        }

        public void OnSkillRotate(Quaternion deltaRot)
        {
            // ModelTransform.rotation *= deltaRot;
            transform.rotation = deltaRot * transform.rotation;
        }

        /// <summary>
        /// 绑定角色模型层
        /// </summary>
        /// <param name="model"></param>
        public void BindModel(GameObject model)
        {
            playerView = model.GetComponent<PlayerView>();
            var vcam = cameraController.GetComponent<CinemachineVirtualCamera>();
            if (vcam != null)
            {
                vcam.Follow = playerView.LookAt;
                vcam.LookAt = playerView.LookAt;
            }
        }
    }
}
