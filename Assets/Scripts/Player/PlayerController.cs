using System;
using Config.GameScene;
using JKFrame;
using Player.Animation;
using Player.State;
using UnityEngine;

namespace Player
{
    public class PlayerController : SingletonMono<PlayerController>, IStateMachineOwner
    {
        [SerializeField] private PlayerView playerView;
        
        private StateMachine stateMachine;
        private PlayerState currentState;

        private CharacterConfig characterConfig;
        public CharacterConfig CharacterConfig => characterConfig;
        public AnimationController AnimationController => playerView.AnimationController;

        public Transform ModelTransform => playerView.transform;
        public float WalkSpeed => characterConfig.WalkSpeed;
        public float RunSpeed => characterConfig.RunSpeed;
        public float RotateSpeed => characterConfig.RotateSpeed;
        
        public void Init()
        {
            characterConfig = ResManager.LoadAsset<CharacterConfig>("AnbiConfig");
            
            playerView = GetComponentInChildren<PlayerView>();
            playerView?.Init();
            // playerView?.InitOnGame(DataManager.CustomCharacterData);
            
            // 初始化状态机
            stateMachine = PoolManager.Instance.GetObject<StateMachine>();
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
            Debug.Log($"[Player] State change: {(prevState == default ? "<none>" : prevState)} -> {currentState}");
            
            switch (currentState)
            {
                case PlayerState.Idle:
                    stateMachine.ChangeState<PlayerIdleState>((int)currentState);
                    break;
                case PlayerState.Move:
                    stateMachine.ChangeState<PlayerMoveState>((int)currentState);
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
