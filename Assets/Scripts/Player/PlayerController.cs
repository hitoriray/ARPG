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

        public float WalkSpeed => anbiConfig.WalkSpeed;

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
        public void PlayAnimation(string animationClipName, float fixedTime = 0.25f)
        {
            playerView.AnimationController.PlayAnimation(anbiConfig.GetAnimationClipByName(animationClipName), fixedTime);
        }
    }
}
