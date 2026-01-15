using System;
using System.Collections.Generic;
using Config;
using UnityEngine;

namespace Player
{
    /// <summary>
    /// 玩家视图
    /// </summary>
    public class PlayerView : MonoBehaviour
    {
        [SerializeField] private SkinnedMeshRenderer[] partSkinnedMeshRenderers;    // 部位渲染器
        [SerializeField] private Material[] partMaterials;                          // 部位的材质
        [SerializeField] private Transform neckRootTransform;                       // 头部的根节点，用于修改头部的大小和高度
        
        public void Init()
        {
            // 提前实例化（需要注意对应的index）
            partSkinnedMeshRenderers[(int)CharacterPartType.Hair].material = Instantiate(partMaterials[0]);
            partSkinnedMeshRenderers[(int)CharacterPartType.Face].material = Instantiate(partMaterials[0]);
            partSkinnedMeshRenderers[(int)CharacterPartType.Cloth].material = Instantiate(partMaterials[2]);
        }
        
        public void SetPart(CharacterPartConfigBase partConfig)
        {
            switch (partConfig.CharacterPartType)
            {
                case CharacterPartType.Hat:
                    break;
                case CharacterPartType.Hair:
                    var hairConfig = partConfig as HairConfig;
                    if (hairConfig == null) break;
                    partSkinnedMeshRenderers[(int)partConfig.CharacterPartType].sharedMesh = hairConfig.Mesh1;
                    break;
                case CharacterPartType.Face:
                    var faceConfig = partConfig as FaceConfig;
                    if (faceConfig == null) break;
                    partSkinnedMeshRenderers[(int)partConfig.CharacterPartType].sharedMesh = faceConfig.Mesh1;
                    break;
                case CharacterPartType.Cloth:
                    var clothConfig = partConfig as ClothConfig;
                    if (clothConfig == null) break;
                    partSkinnedMeshRenderers[(int)partConfig.CharacterPartType].sharedMesh = clothConfig.Mesh1;
                    break;
                case CharacterPartType.ShoulderPad:
                    break;
                case CharacterPartType.Belt:
                    break;
                case CharacterPartType.Glove:
                    break;
                case CharacterPartType.Shoe:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        /// <summary>
        /// 设置部位的颜色1
        /// </summary>
        public void SetColor1(CharacterPartConfigBase partConfig, Color color)
        {
            var partType = partConfig.CharacterPartType;
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
        public void SetColor2(CharacterPartConfigBase partConfig, Color color)
        {
            var partType = partConfig.CharacterPartType;
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
    }
}
