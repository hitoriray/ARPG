using System;
using System.Collections.Generic;
using Battle.ECS;
using Config;
using Data;
using JKFrame;
using Player.Animation;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// 玩家视图
    /// </summary>
    public class PlayerView : MonoBehaviour, ICharacterView
    {
        [SerializeField] private AnimationController animationController;
        public AnimationController AnimationController => animationController;
        
        private GameData gameData;                            // 玩家定义的角色数据，用于存档

        public void Init()
        {
            animationController.Init();
        }
        
        public void Init(GameData data)
        {
            animationController.Init();
            gameData = data;
        }

        private void OnDestroy()
        {
            // 释放全部资源
        }

        public void SyncPosition(Vector3 position)
        {
            transform.position = position;
        }

        public void SyncRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }

        public void PlayAnimation(string animName)
        {
            // TODO: 由外部传入动画资源或在PlayerController中转发
        }
    }
}
