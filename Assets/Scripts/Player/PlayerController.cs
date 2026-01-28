using System;
using Attribute;
using BuffSystem;
using Config;
using Data;
using JKFrame;
using Player.Animation;
using Skill;
using Player.State;
using UnityEngine;

namespace Player
{
    public class PlayerController : SingletonMono<PlayerController>, IStateMachineOwner
    {
        [SerializeField] private SkillBrainBase skillBrain;
        [SerializeField] private PlayerView playerView;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private BuffController buffController;
        [SerializeField] private CharacterAttribute characterAttribute;

        public SkillBrainBase SkillBrain => skillBrain;
        public CharacterController CharacterController => characterController;
        public AnimationController AnimationController => playerView.AnimationController;
        public Transform ModelTransform => playerView.transform;
        public CharacterAttribute CharacterAttribute => characterAttribute;

        public float WalkSpeed => characterConfig.WalkSpeed;
        public float RunSpeed => characterConfig.RunSpeed;
        public float RotateSpeed => characterConfig.RotateSpeed;

        private StateMachine stateMachine;
        private PlayerState currentState;
        private CharacterConfig characterConfig;
        public CharacterConfig CharacterConfig => characterConfig;
        
        public void Init(CharacterConfig characterConfig, GameData gameData)
        {
            this.characterConfig = characterConfig;
            
            playerView = GetComponentInChildren<PlayerView>();
            playerView?.Init();
            // playerView?.InitOnGame(gameData);
            
            characterAttribute.Init(characterConfig);
            skillBrain.Init(this, gameData.SkillLearnedDatas);
            // 初始化状态机
            stateMachine = ResSystem.GetOrNew<StateMachine>();
            stateMachine.Init(this);
            // 默认待机
            ChangeState(PlayerState.Idle);
        }

        /// <summary>
        /// 修改玩家状态
        /// </summary>
        public void ChangeState(PlayerState newState, bool reCurrstate = false)
        {
            var prevState = currentState;
            currentState = newState;
            // Debug.Log($"[Player] State change: {(prevState == default ? "<none>" : prevState)} -> {currentState}");
            
            switch (currentState)
            {
                case PlayerState.Idle:
                    stateMachine.ChangeState<PlayerIdleState>(reCurrstate);
                    break;
                case PlayerState.Move:
                    stateMachine.ChangeState<PlayerMoveState>(reCurrstate);
                    break;
                case PlayerState.Skill:
                    stateMachine.ChangeState<PlayerSkillState>(reCurrstate);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 播放动画
        /// </summary>
        public void PlayAnimation(string clipName, Action<Vector3, Quaternion> rootMotionAction = null, float speed = 1, bool refreshAnimation = false, float transitionFixedTime = 0.25f)
        {
            if (rootMotionAction != null)
            {
                playerView.AnimationController.SetRootMotionAction(rootMotionAction);
            }
            playerView.AnimationController.PlaySingleAnimation(characterConfig.GetAnimationClipByName(clipName), speed, refreshAnimation, transitionFixedTime);
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
            buffController.AddBuff(buffConfig, stack);
        }
    }
}
