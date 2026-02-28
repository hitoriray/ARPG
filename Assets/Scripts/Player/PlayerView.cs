using System;
using System.Collections.Generic;
using Battle.ECS;
using Config;
using Data;
using JKFrame;
using RayAnimation;
using UnityEngine;

namespace RayPlayer
{
    /// <summary>
    /// 玩家视图
    /// </summary>
    public class PlayerView : ICharacterView
    {
        [SerializeField] private Transform lookAt;
        public Transform LookAt => lookAt;

        private GameData gameData; // 玩家定义的角色数据，用于存档

        public void Init()
        {
        }
        
        public void Init(GameData data)
        {
            gameData = data;
        }

        private void OnDestroy()
        {
            // 释放全部资源
        }

        public override void SyncPosition(Vector3 position)
        {
            transform.position = position;
        }

        public override  void SyncRotation(Quaternion rotation)
        {
            transform.rotation = rotation;
        }

        public override  void PlayAnimation(string animName)
        {
        }
    }
}
