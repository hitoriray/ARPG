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
                case CharacterPartType.Hair:
                    configName = "HairConfig_";
                    break;
                case CharacterPartType.Face:
                    configName = "FaceConfig_";
                    break;
                case CharacterPartType.Cloth:
                    configName = "ClothConfig_";
                    break;
            }
            
            configName += index.ToString();
            return ResManager.LoadAsset<CharacterPartConfigBase>(configName);
        }
    }
}