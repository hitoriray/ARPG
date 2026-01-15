using System;
using JKFrame;

namespace Config
{
    public static class ConfigTool
    {
        public static readonly string ProjectConfigName = "Project";
        
        /// <summary>
        /// 获取角色部位的配置
        /// </summary>
        public static CharacterPartConfigBase LoadCharacterPartConfig(CharacterPartType characterPartType, int index)
        {
            string configName = "";
            switch (characterPartType)
            {
                case CharacterPartType.Hat:
                    break;
                case CharacterPartType.Hair:
                    configName = "HairConfig_";
                    break;
                case CharacterPartType.Face:
                    configName = "FaceConfig_";
                    break;
                case CharacterPartType.Cloth:
                    configName = "ClothConfig_";
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
                    throw new ArgumentOutOfRangeException(nameof(characterPartType), characterPartType, null);
            }
            
            configName += index.ToString();
            return ResManager.LoadAsset<CharacterPartConfigBase>(configName);
        }
    }
}