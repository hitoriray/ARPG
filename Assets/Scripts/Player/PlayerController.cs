using System;
using Animancer;
using Arch.Core;
using Attribute;
using Battle.ECS;
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
    public class PlayerController : SingletonMono<PlayerController>, IStateMachineOwner, ICharacter, IGOAPOwner
    {
        public Entity PlayerEntity { get; private set; }
        [SerializeField] private PlayerSkillBrainBase skillBrain;
        [SerializeField] private PlayerView playerView;
        [SerializeField] private CharacterController characterController;
        // [SerializeField] private BuffController buffController;
        [SerializeField] private CharacterAttribute characterAttribute;
        [SerializeField] private WeaponSlotManager weaponSlotManager;
        [SerializeField] private CameraController cameraController;
        
        public PlayerSkillBrainBase SkillBrain => skillBrain;
        public CharacterController CharacterController => characterController;
        public AnimationController AnimationController => playerView.AnimationController;
        public Transform ModelTransform => playerView.transform;
        public CharacterAttribute CharacterAttribute => characterAttribute;

        public float WalkSpeed => characterConfig.WalkSpeed;
        public float RunSpeed => characterConfig.RunSpeed;
        public float RotateSpeed => characterConfig.RotateSpeed;

        private StateMachine stateMachine;
        private PlayerState currentState;
        private AnimancerState currentSkillState;
        private CharacterConfig characterConfig;
        public CharacterConfig CharacterConfig => characterConfig;
        
        public void Init(CharacterConfig characterConfig, GameData gameData)
        {
            this.characterConfig = characterConfig;
            
            playerView = GetComponentInChildren<PlayerView>();
            playerView?.Init();
            // playerView?.InitOnGame(gameData);
            
            // agent.Init(this);
            
            characterAttribute.Init(characterConfig, characterConfig.hpBaseValue, characterConfig.mpBaseValue);
            skillBrain.Init(this, DataManager.GetCurrentCharacterSkills());
            // 初始化状态机
            stateMachine = ResSystem.GetOrNew<StateMachine>();
            stateMachine.Init(this);
            // 默认待机
            ChangeState(PlayerState.Idle);
            // 刷新武器槽位索引
            weaponSlotManager.RefreshSlots();
            // 让Cinemachine看向这个player
            cameraController.GetComponent<CinemachineVirtualCamera>().Follow = playerView.LookAt;
            cameraController.GetComponent<CinemachineVirtualCamera>().LookAt = playerView.LookAt;

            var context = BattleEcsRunner.Instance.Context;
            if (context != null)
            {
                PlayerEntity = BattleEcsRunner.Instance.RegisterPlayer(this);
                RayDebug.Log($"ECS实体已创建: Entity ID = {PlayerEntity.Id}");
            }
        }

        private void Update()
        {
            // agent.OnUpdate();
        }

        /// <summary>
        /// 修改玩家状态
        /// </summary>
        public void ChangeState(PlayerState newState, bool reCurrstate = false)
        {
            var prevState = currentState;
            currentState = newState;
            RayDebug.Trace($"State change: {(prevState == default ? "<none>" : prevState)} -> {currentState}");
            
            switch (currentState)
            {
                case PlayerState.Idle:
                    stateMachine.ChangeState<RayPlayerState.PlayerIdleState>(reCurrstate);
                    RestoreMovementControl();
                    break;
                case PlayerState.Move:
                    stateMachine.ChangeState<PlayerMoveState>(reCurrstate);
                    RestoreMovementControl();
                    break;
                case PlayerState.Skill:
                    stateMachine.ChangeState<PlayerSkillState>(reCurrstate);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 恢复移动层的 Animancer 控制权
        /// </summary>
        private void RestoreMovementControl()
        {
            if (currentSkillState != null)
            {
                currentSkillState.Stop();
                currentSkillState = null;
            }

            // Player.cs 的状态机会自动接管
            RayDebug.Log("已归还 Animancer 控制权给移动层");
        }

        /// <summary>
        /// 播放动画
        /// </summary>
        public void PlayAnimation(string clipName, Action<Vector3, Quaternion> rootMotionAction = null, float speed = 1, bool refreshAnimation = false, float transitionFixedTime = 0.25f)
        {
            if (rootMotionAction != null)
            {
                playerView.AnimationController?.SetRootMotionAction(rootMotionAction);
            }
            playerView.AnimationController?.PlaySingleAnimation(characterConfig.GetAnimationClipByName(clipName), speed, refreshAnimation, transitionFixedTime);
        }
        
        public void PlayBlendAnimation(string clip1Name, string clip2Name, Action<Vector3, Quaternion> rootMotionAction = null, float speed = 1, float transitionFixedTime = 0.25f)
        {
            if (rootMotionAction != null)
            {
                playerView.AnimationController.SetRootMotionAction(rootMotionAction);
            }
            var clip1 = characterConfig.GetAnimationClipByName(clip1Name);
            var clip2 = characterConfig.GetAnimationClipByName(clip2Name);
            playerView.AnimationController.PlayBlendAnimation(clip1, clip2, speed, transitionFixedTime);
        }

        public void Rotate(Vector3 inputDir, float rotateSpeed = 0)
        {
            if (rotateSpeed == 0) rotateSpeed = RotateSpeed;
            // 获取相机的旋转值
            float y = Camera.main.transform.rotation.eulerAngles.y;
            // 让input也旋转y角度
            Vector3 moveDir = Quaternion.Euler(0, y, 0) * inputDir;
            // 处理旋转
            ModelTransform.rotation = Quaternion.Slerp(ModelTransform.rotation,
                Quaternion.LookRotation(moveDir), Time.deltaTime * rotateSpeed);
        }

        public void AddBuff(BuffConfig buffConfig, int stack)
        {
            // buffController.AddBuff(buffConfig, stack);
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
            Vector2 moveInput = InputManager.Instance.GetMoveInput();
            if (moveInput.x != 0 || moveInput.y != 0)
            {
                Rotate(new Vector3(moveInput.x, 0, moveInput.y));
            }
        }
        
        public void Change2IdleState()
        {
            ChangeState(PlayerState.Idle);
        }

        public void OnSkillMove(Vector3 deltaPos)
        {
            CharacterController.Move(deltaPos);
        }

        public void OnSkillRotate(Quaternion deltaRot)
        {
            ModelTransform.rotation *= deltaRot;
        }
    }
}
