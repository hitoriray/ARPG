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
            ExitSkillMode();
            MovementStateMachine.ChangeState(MovementStateMachine.moveStartState);
        }

        public void EnterSkillMode(bool upperBody)
        {
            inSkill = true;
            var baseLayer = animancer.Layers[0];
            var skillLayer = animancer.Layers[skillLayerIndex];

            skillLayer.IsAdditive = false;
            skillLayer.Mask = upperBody ? upperBodyMask : null;
            skillLayer.SetWeight(1f);
            
            baseLayer.SetWeight(upperBody ? 1f : 0f);
        }

        public void ExitSkillMode()
        {
            if (!inSkill)
                return;

            inSkill = false;
            ClearSkillRootMotion();
            
            var baseLayer = animancer.Layers[0];
            var skillLayer = animancer.Layers[skillLayerIndex];

            skillLayer.Stop();
            skillLayer.SetWeight(0f);
            skillLayer.Mask = null;
            baseLayer.SetWeight(1f);
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
                    ExitSkillMode();
                    MovementStateMachine.ChangeState(MovementStateMachine.idleState);
                    break;
                case PlayerState.Move:
                    ExitSkillMode();
                    MovementStateMachine.ChangeState(MovementStateMachine.moveStartState);
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
            ExitSkillMode();
            MovementStateMachine.ChangeState(MovementStateMachine.idleState);
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
