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


        public void SetPartColor(CharacterPartType partType, Color color, int colorIndex)
        {
            
        }

        public void SetFaceSize(float size)
        {
            
        }

        public void SetFaceHeight(float height)
        {
            
        }
    }
}
