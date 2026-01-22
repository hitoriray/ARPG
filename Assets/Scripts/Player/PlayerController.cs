using System;
using Config;
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

        public SkillBrainBase SkillBrain => skillBrain;
        public CharacterController CharacterController => characterController;
        public AnimationController AnimationController => playerView.AnimationController;
        public Transform ModelTransform => playerView.transform;
        public float WalkSpeed => characterConfig.WalkSpeed;
        public float RunSpeed => characterConfig.RunSpeed;
        public float RotateSpeed => characterConfig.RotateSpeed;

        private StateMachine stateMachine;
        private PlayerState currentState;
        private CharacterConfig characterConfig;
        public CharacterConfig CharacterConfig => characterConfig;
        
        public void Init()
        {
            characterConfig = ResSystem.LoadAsset<CharacterConfig>("AnbiConfig");
            
            playerView = GetComponentInChildren<PlayerView>();
            playerView?.Init();
            // playerView?.InitOnGame(DataManager.CustomCharacterData);
            
            skillBrain.Init(this);
            // 初始化状态机
            stateMachine = ResSystem.GetOrNew<StateMachine>();
            stateMachine.Init(this);
            // 默认待机
            ChangeState(PlayerState.Idle);
        }

        /// <summary>
        /// 修改玩家状态
        /// </summary>
        /// <param name="newState"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void ChangeState(PlayerState newState)
        {
            var prevState = currentState;
            currentState = newState;
            // Debug.Log($"[Player] State change: {(prevState == default ? "<none>" : prevState)} -> {currentState}");
            
            switch (currentState)
            {
                case PlayerState.Idle:
                    stateMachine.ChangeState<PlayerIdleState>();
                    break;
                case PlayerState.Move:
                    stateMachine.ChangeState<PlayerMoveState>();
                    break;
                case PlayerState.Skill:
                    stateMachine.ChangeState<PlayerSkillState>();
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
    }
}
