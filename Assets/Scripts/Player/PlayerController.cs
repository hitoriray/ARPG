using System;
using Config.GameScene;
using Data;
using JKFrame;
using Player.State;
using UnityEngine;

namespace Player
{
    public class PlayerController : SingletonMono<PlayerController>, IStateMachineOwner
    {
        [SerializeField] private PlayerView playerView;
        
        private StateMachine stateMachine;
        private PlayerState currentState;

        private AnbiConfig anbiConfig;

        public Transform ModelTransform => playerView.transform;
        public float WalkSpeed => anbiConfig.WalkSpeed;
        public float RotateSpeed => anbiConfig.RotateSpeed;

        public void Init()
        {
            anbiConfig = ResManager.LoadAsset<AnbiConfig>("AnbiConfig");
            
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
            currentState = newState;
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
        public void PlayAnimation(string clipName, float speed = 1, bool refreshAnimation = false, float transitionFixedTime = 0.25f)
        {
            playerView.AnimationController.PlayAnimation(anbiConfig.GetAnimationClipByName(clipName), speed, refreshAnimation, transitionFixedTime);
        }
        
        public void PlayBlendAnimation(string clip1Name, string clip2Name, float speed = 1, float transitionFixedTime = 0.25f)
        {
            var clip1 = anbiConfig.GetAnimationClipByName(clip1Name);
            var clip2 = anbiConfig.GetAnimationClipByName(clip2Name);
            playerView.AnimationController.PlayBlendAnimation(clip1, clip2, speed, transitionFixedTime);
        }
        
        public void SetBlendAnimationWeight(float clip1Weight)
        {
            playerView.AnimationController.SetBlendWeight(clip1Weight);
        }
    }
}
