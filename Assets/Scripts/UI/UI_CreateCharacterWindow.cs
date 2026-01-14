using Config;
using JKFrame;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    [UIElement(false, "UI_CreateCharacterWindow", 2)]
    public class UI_CreateCharacterWindow : UI_WindowBase
    {
        // 模型交互Image
        [SerializeField] Image modelTouchImage;

        #region 模型交互 - 拖拽
        [SerializeField] public float dragSpeed = 60f;
        private float lastPosX = 0;
        #endregion
        
        // 职业按钮相关
        [SerializeField] private UI_ProfessionButton[] professionButtons; // 所有的职业按钮
        private UI_ProfessionButton currentProfessionButton; // 当前选中的职业按钮

        // 外观部位按钮相关
        [SerializeField] private UI_FacadeMenuButton[] facadeMenuButtons;
        private UI_FacadeMenuButton currentFacadeMenuButton;
        
        public override void Init()
        {
            base.Init();
            // 绑定modelTouchImage的拖拽事件
            modelTouchImage.OnDrag(ModelTouchImageDrag);
            
            // 初始化职业按钮
            for (int i = 0; i < professionButtons.Length; i++)
            {
                professionButtons[i].Init(this, (ProfessionType)i); // 需要确保是按枚举顺序填入的四个职业
            }
            // 默认选择第一个职业（战士）
            SelectProfessionButton(professionButtons[0]);
            
            // 初始化外观部位按钮
            facadeMenuButtons[0].Init(this, CharacterPartType.Face);
            facadeMenuButtons[1].Init(this, CharacterPartType.Hair);
            facadeMenuButtons[2].Init(this, CharacterPartType.Cloth);
            SelectFacadeMenuButton(facadeMenuButtons[0]);
        }

        /// <summary>
        /// 当模型图片交互区域拖拽时的回调
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="args"></param>
        private void ModelTouchImageDrag(PointerEventData eventData, object[] args)
        {
            float offset = eventData.position.x - lastPosX;
            lastPosX = eventData.position.x;
            CharacterCreator.Instance.RotateCharacter(new Vector3(0, -offset * Time.deltaTime * dragSpeed, 0));
        }

        private void Start()
        {
            Init();
            
            #region 测试逻辑

            var faceConfigIDs = ConfigManager.Instance.GetConfig<ProjectConfig>(ConfigTool.ProjectConfigName).CustomCharacterPartConfigIdDict[CharacterPartType.Face];
            foreach (var id in faceConfigIDs)
            {
                CharacterPartConfigBase config = ConfigTool.GetCharacterPartConfig(CharacterPartType.Face, id);
                Debug.Log(config.Name);
            }
            
            #endregion
        }

        /// <summary>
        /// 选择职业按钮
        /// </summary>
        public void SelectProfessionButton(UI_ProfessionButton newProfessionButton)
        {
            if (currentProfessionButton == newProfessionButton)
                return;
            
            if (currentProfessionButton != null)
            {
                currentProfessionButton.Unselect();
            }
            
            newProfessionButton.Select();
            currentProfessionButton = newProfessionButton;
            SelectProfession(newProfessionButton.Profession);
        }

        /// <summary>
        /// 选择职业
        /// </summary>
        private void SelectProfession(ProfessionType newProfession)
        {
            CharacterCreator.Instance.SetProfession(newProfession);
            
            // TODO:检查服装是否与职业相符
        }

        /// <summary>
        /// 选择外观部位
        /// </summary>
        public void SelectFacadeMenuButton(UI_FacadeMenuButton newFacadeMenuButton)
        {
            if (currentFacadeMenuButton != null)
            {
                currentFacadeMenuButton.Unselect();
            }
            
            newFacadeMenuButton.Select();
            currentFacadeMenuButton = newFacadeMenuButton;
            // TODO：刷新界面
        }
        
    }
}
