using System;
using System.Threading;
using Animancer;
using Arch.Core;
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
            HandleSkillInterruptByMove();
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
                    skillBrain.ReleaseSkill(i);
                    return;
                }
            }
        }

        private void HandleSkillInterruptByMove()
        {
            if (skillBrain == null || !skillBrain.CanInterrupt)
                return;
            if (InputService == null || InputService.Move == Vector2.zero)
                return;

            skillBrain.InterruptCurrentSkill();
            DestroyWeapon(-1);
            // 先切换状态机（在 baseLayer 上播放移动动画），再淡入 baseLayer
            MovementStateMachine.ChangeState(MovementStateMachine.moveStartState);
            ExitSkillMode();
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
            skillLayer.StartFade(1f, skillLayerFadeIn);

            if (upperBody)
                baseLayer.StartFade(1f, skillLayerFadeIn);
            else
                baseLayer.StartFade(0f, skillLayerFadeIn);
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
            RayDebug.Log("玩家被命中！");
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

            // 先在 baseLayer 上播放 Idle 动画（此时 baseLayer 权重可能仍为 0，用户看不到）
            // 再执行 ExitSkillMode 淡入 baseLayer，确保淡入时上面已是正确的 Idle 姿态
            if (MovementStateMachine.currentState == MovementStateMachine.idleState)
            {
                if (ReusableData != null && ReusableLogic != null)
                {
                    ReusableData.currentCrouchIdleIndex = -1;
                    ReusableData.currentStandIdleIndex = -1;
                    ReusableLogic.InitIdleState();
                    ReusableLogic.PlayNextState();
                }
            }
            else
            {
                MovementStateMachine.ChangeState(MovementStateMachine.idleState);
            }

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
