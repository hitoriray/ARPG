using System;
using System.Collections.Generic;
using Config;
using Data;
using JKFrame;
using Player.Animation;
using Serialization;
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
        
        [SerializeField] private SkinnedMeshRenderer[] partSkinnedMeshRenderers;    // 部位渲染器
        [SerializeField] private Material[] partMaterials;                          // 部位的材质
        [SerializeField] private Transform neckRootTransform;                       // 头部的根节点，用于修改头部的大小和高度
        private CustomCharacterData customCharacterData;                            // 玩家定义的角色数据，用于存档
        private readonly Dictionary<int, CharacterPartConfigBase> characterPartDict = new (); // 角色部位字典

        public void Init()
        {
            animationController.Init();
        }
        
        public void Init(CustomCharacterData data)
        {
            animationController.Init();
            
            // 提前实例化（需要注意对应的index）
            if (partSkinnedMeshRenderers.Length >= 3)
            {
                partSkinnedMeshRenderers[(int)CharacterPartType.Hair].material = Instantiate(partMaterials[0]);
                partSkinnedMeshRenderers[(int)CharacterPartType.Face].material = Instantiate(partMaterials[0]);
                partSkinnedMeshRenderers[(int)CharacterPartType.Cloth].material = Instantiate(partMaterials[2]);
            }

            customCharacterData = data;
        }

        public void InitOnGame(CustomCharacterData data)
        {
            Init(data);
            
            // 基于数据设置当前部位
            var hairConfig = ConfigTool.LoadCharacterPartConfig(CharacterPartType.Hair, customCharacterData.CustomPartDataDict[(int)CharacterPartType.Hair].Index);
            var faceConfig = ConfigTool.LoadCharacterPartConfig(CharacterPartType.Face, customCharacterData.CustomPartDataDict[(int)CharacterPartType.Face].Index);
            var clothConfig = ConfigTool.LoadCharacterPartConfig(CharacterPartType.Cloth, customCharacterData.CustomPartDataDict[(int)CharacterPartType.Cloth].Index);
            var facePartData = customCharacterData.CustomPartDataDict[(int)CharacterPartType.Face];

            SetPart(hairConfig, true);
            SetPart(faceConfig, true);
            SetPart(clothConfig, true);
            
            SetSize(CharacterPartType.Face, facePartData.Size);
            SetHeight(CharacterPartType.Face, facePartData.Height);
        }

        // 获取角色部位配置
        public CharacterPartConfigBase GetCharacterPartConfig(CharacterPartType partType)
        {
            return characterPartDict.GetValueOrDefault((int)partType);
        }
        
        public void SetPart(CharacterPartConfigBase partConfig, bool updateCharacterView = true)
        {
            var partType = partConfig.CharacterPartType;
            if (characterPartDict.TryGetValue((int)partType, out var currentPartConfig))
            {
                // 释放旧的资源并且设置新资源
                ResManager.Release(currentPartConfig);
                characterPartDict[(int)partType] = partConfig;
            }
            else
            {
                // 这个部位之前是空的，所以不存在资源释放问题
                characterPartDict.Add((int)partType, partConfig);
            }
            
            // 不更新实际的画面
            if (!updateCharacterView)
                return;
            
            switch (partType)
            {
                case CharacterPartType.Hair:
                    var hairConfig = partConfig as HairConfig;
                    if (hairConfig == null) break;
                    partSkinnedMeshRenderers[(int)partType].sharedMesh = hairConfig.Mesh1;
                    SetColor1(partType, customCharacterData.CustomPartDataDict[(int)partType].Color1.ConvertToUnityColor());
                    break;
                case CharacterPartType.Face:
                    var faceConfig = partConfig as FaceConfig;
                    if (faceConfig == null) break;
                    partSkinnedMeshRenderers[(int)partType].sharedMesh = faceConfig.Mesh1;
                    break;
                case CharacterPartType.Cloth:
                    var clothConfig = partConfig as ClothConfig;
                    if (clothConfig == null) break;
                    partSkinnedMeshRenderers[(int)partType].sharedMesh = clothConfig.Mesh1;
                    SetColor1(partType, customCharacterData.CustomPartDataDict[(int)partType].Color1.ConvertToUnityColor());
                    SetColor2(partType, customCharacterData.CustomPartDataDict[(int)partType].Color2.ConvertToUnityColor());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        /// <summary>
        /// 设置部位的颜色1
        /// </summary>
        public void SetColor1(CharacterPartType partType, Color color)
        {
            var partConfig = GetCharacterPartConfig(partType);
            // 根据不同的部位类型，确定到具体要修改材质球中的哪一个颜色
            switch (partType)
            {
                case CharacterPartType.Hair:
                    var hairConfig = partConfig as HairConfig;
                    if (hairConfig == null || hairConfig.ColorIndex < 0) break;
                    partSkinnedMeshRenderers[(int)partType].sharedMaterial.SetColor("_Color0" + (hairConfig.ColorIndex + 1), color);
                    break;
                case CharacterPartType.Cloth:
                    var clothConfig = partConfig as ClothConfig;
                    if (clothConfig == null || clothConfig.ColorIndex1 < 0) break;
                    partSkinnedMeshRenderers[(int)partType].sharedMaterial.SetColor($"_Color0{(clothConfig.ColorIndex1 + 1).ToString()}", color);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        /// <summary>
        /// 设置部位的颜色2
        /// </summary>
        public void SetColor2(CharacterPartType partType, Color color)
        {
            var partConfig = GetCharacterPartConfig(partType);
            switch (partType)
            {
                case CharacterPartType.Cloth:
                    var clothConfig = partConfig as ClothConfig;
                    if (clothConfig == null || clothConfig.ColorIndex2 < 0) break;
                    partSkinnedMeshRenderers[(int)partType].sharedMaterial.SetColor($"_Color0{(clothConfig.ColorIndex2 + 1).ToString()}", color);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 设置部位的尺寸
        /// </summary>
        public void SetSize(CharacterPartType partType, float value)
        {
            // 目前只支持修改Face
            if (partType == CharacterPartType.Face)
            {
                neckRootTransform.localScale = Vector3.one * value;
            }
        }

        /// <summary>
        /// 设置部位的高度
        /// </summary>
        public void SetHeight(CharacterPartType partType, float value)
        {
            // 目前只支持修改Face
            if (partType == CharacterPartType.Face)
            {
                neckRootTransform.localPosition = new Vector3(-value, 0, 0);
            }
        }

        private void OnDestroy()
        {
            // 释放全部资源
            foreach (var item in characterPartDict)
            {
                ResManager.Release(item.Value);
            }
        }
    }
}
