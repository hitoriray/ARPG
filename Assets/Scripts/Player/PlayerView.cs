using System;
using System.Collections.Generic;
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
    public class PlayerView : MonoBehaviour
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
    }
}
