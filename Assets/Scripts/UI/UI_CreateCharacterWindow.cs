using System;
using System.Collections.Generic;
using Config;
using Data;
using JKFrame;
using Unity.VisualScripting;
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
        // 外观部位文本
        [SerializeField] private Text partNameText;
        
        // 尺寸、高度 进度条
        [SerializeField] private Slider sizeSlider;
        [SerializeField] private Slider heightSlider;
        
        // 颜色按钮
        [SerializeField] private Button color1Button;
        [SerializeField] private Button color2Button;
        
        // 左、右箭头按钮
        [SerializeField] private Button leftArrowButton;
        [SerializeField] private Button rightArrowButton;
        [SerializeField] private AudioClip arrowAudioClip;
        
        // 自定义角色的数据
        private CustomCharacterData customCharacterData;
        
        private ProjectConfig projectConfig;
        //玩家当前每一个部位选择的是projectConfig中的第几个索引的配置
        private Dictionary<int, int> part2ConfigDict;
        
        private void Start()
        {
            Init();
        }
        
        public override void Init()
        {
            base.Init();
            
            // 获取配置
            projectConfig = ConfigManager.Instance.GetConfig<ProjectConfig>(ConfigTool.ProjectConfigName);
            part2ConfigDict = new(3);
            part2ConfigDict.Add((int)CharacterPartType.Hair, 0);
            part2ConfigDict.Add((int)CharacterPartType.Face, 0);
            part2ConfigDict.Add((int)CharacterPartType.Cloth, 0);
            
            // 绑定modelTouchImage的拖拽事件
            modelTouchImage.OnDrag(OnModelTouchImageDrag);
            // 绑定左右箭头按钮的监听事件
            leftArrowButton.onClick.AddListener(OnLeftArrowBtnClicked);
            rightArrowButton.onClick.AddListener(OnRightArrowBtnClicked);
            // 绑定slider事件
            sizeSlider.onValueChanged.AddListener(OnSizeSliderValueChanged);
            heightSlider.onValueChanged.AddListener(OnHeightSliderValueChanged);
            // 绑定颜色按钮事件
            color1Button.onClick.AddListener(OnColor1BtnClicked);
            color2Button.onClick.AddListener(OnColor2BtnClicked);
            
            // 初始化默认数据
            customCharacterData = new CustomCharacterData
            {
                CustomPartDataDict = new()
            };
            customCharacterData.CustomPartDataDict.Add((int)CharacterPartType.Face, new CustomCharacterPartData
            {
                Index = 1, Size = 1, Height = 0,
            });
            customCharacterData.CustomPartDataDict.Add((int)CharacterPartType.Hair, new CustomCharacterPartData
            {
                Index = 1, Color1 = Color.white,
            });
            customCharacterData.CustomPartDataDict.Add((int)CharacterPartType.Cloth, new CustomCharacterPartData
            {
                Index = 1, Color1 = Color.white, Color2 = Color.black,
            });


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
            
            // 应用默认的部位配置
            SetCharacterPart(CharacterPartType.Face, customCharacterData.CustomPartDataDict[(int)CharacterPartType.Face].Index, true, true);
            SetCharacterPart(CharacterPartType.Hair, customCharacterData.CustomPartDataDict[(int)CharacterPartType.Hair].Index, false, true);
            SetCharacterPart(CharacterPartType.Cloth, customCharacterData.CustomPartDataDict[(int)CharacterPartType.Cloth].Index, false, true);
        }

        #region 事件回调
        /// <summary>
        /// 当模型图片交互区域拖拽时的回调
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="args"></param>
        private void OnModelTouchImageDrag(PointerEventData eventData, object[] args)
        {
            float offset = eventData.position.x - lastPosX;
            lastPosX = eventData.position.x;
            CharacterCreator.Instance.RotateCharacter(new Vector3(0, -offset * Time.deltaTime * dragSpeed, 0));
        }

        private void OnLeftArrowBtnClicked()
        {
            OnArrowBtnClicked(-1);
        }

        private void OnRightArrowBtnClicked()
        {
            OnArrowBtnClicked(1);
        }

        private void OnArrowBtnClicked(int increase)
        {
            var currentPartType = currentFacadeMenuButton.CharacterPartType;
            int currentIndex = part2ConfigDict[(int)currentPartType];
            var currentPartConfigIds = projectConfig.CustomCharacterPartConfigIdDict[currentPartType];
            currentIndex += increase;
            if (currentIndex < 0) currentIndex = currentPartConfigIds.Count - 1;
            else if (currentIndex >= currentPartConfigIds.Count) currentIndex = 0;

            // 检查职业有效性
            var currentProfession = currentProfessionButton.Profession;
            while (!ConfigTool.GetCharacterPartConfig(currentPartType,
                       currentPartConfigIds[currentIndex]).ProfessionTypes.Contains(currentProfession))
            {
                currentIndex += increase;
                if (currentIndex < 0) currentIndex = currentPartConfigIds.Count - 1;
                else if (currentIndex >= currentPartConfigIds.Count) currentIndex = 0;
            }
            
            // 说明职业有效
            part2ConfigDict[(int)currentPartType] = currentIndex;
            SetCharacterPart(currentPartType, currentPartConfigIds[currentIndex], true, true);
            AudioManager.Instance.PlayOnShot(arrowAudioClip, Vector3.zero, 1, false);
        }
        
        private void OnSizeSliderValueChanged(float value)
        {
            GetCurrentCharacterPartData().Size = value;
            CharacterCreator.Instance.SetSize(currentFacadeMenuButton.CharacterPartType, value);
        }
        
        private void OnHeightSliderValueChanged(float value)
        {
            GetCurrentCharacterPartData().Height = value;
            CharacterCreator.Instance.SetHeight(currentFacadeMenuButton.CharacterPartType, value);
        }
        
        private void OnColor1BtnClicked()
        {
            // 显示颜色选择器窗口
            UIManager.Instance.Show<UI_ColorSelectorWindow>().Init(OnColor1Selected, color1Button.image.color);
        }
        
        private void OnColor2BtnClicked()
        {
            UIManager.Instance.Show<UI_ColorSelectorWindow>().Init(OnColor2Selected, color2Button.image.color);
        }

        private void OnColor1Selected(Color newColor)
        {
            // 1.存储数据
            var currentPartData = GetCurrentCharacterPartData();
            currentPartData.Color1 = newColor;
            // 2.修改颜色
            var partConfig = GetCurrentCharacterPartConfig();
            CharacterCreator.Instance.SetColor1(partConfig, newColor);
        }

        private void OnColor2Selected(Color newColor)
        {
            // 1.存储数据
            var currentPartData = GetCurrentCharacterPartData();
            currentPartData.Color2 = newColor;
            // 2.修改颜色
            var partConfig = GetCurrentCharacterPartConfig();
            CharacterCreator.Instance.SetColor2(partConfig, newColor);
        }
        
        #endregion
        
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
            // 刷新界面
            var currentPartType = currentFacadeMenuButton.CharacterPartType;
            var currentIndex = part2ConfigDict[(int)currentPartType];
            SetCharacterPart(currentPartType, projectConfig.CustomCharacterPartConfigIdDict[currentPartType][currentIndex], true, false);
        }

        /// <summary>
        /// 设置角色的具体部位
        /// </summary>
        private void SetCharacterPart(CharacterPartType partType, int configIndex, bool updateUIView = false, bool updateCharacterView = false)
        {
            // 获取配置文件
            var partConfig = ConfigTool.GetCharacterPartConfig(partType, configIndex);

            // 更新UI
            if (updateUIView)
            {
                partNameText.text = partConfig.Name;
                switch (partType)
                {
                    case CharacterPartType.Hat:
                        break;
                    case CharacterPartType.Hair:
                        // 隐藏尺寸、高度、Color2
                        sizeSlider.transform.parent.gameObject.SetActive(false);
                        heightSlider.transform.parent.gameObject.SetActive(false);
                        color2Button.gameObject.SetActive(false);
                        
                        if ((partConfig as HairConfig).ColorIndex != -1)
                        {
                            color1Button.gameObject.SetActive(true);
                            // TODO: 让该按钮的图片和当前的颜色配置一样
                        }
                        else
                        {
                            color1Button.gameObject.SetActive(false);
                        }
                        break;
                    case CharacterPartType.Face:
                        // 尺寸
                        sizeSlider.transform.parent.gameObject.SetActive(true);
                        sizeSlider.value = customCharacterData.CustomPartDataDict[(int)CharacterPartType.Face].Size;
                        sizeSlider.minValue = 0.9f;
                        sizeSlider.maxValue = 1.2f;
                        // 高度
                        heightSlider.transform.parent.gameObject.SetActive(true);
                        heightSlider.value = customCharacterData.CustomPartDataDict[(int)CharacterPartType.Face].Height;
                        heightSlider.minValue = 0;
                        heightSlider.maxValue = 0.1f;
                        // 隐藏颜色按钮
                        color1Button.gameObject.SetActive(false);
                        color2Button.gameObject.SetActive(false);
                        break;
                    case CharacterPartType.Cloth:
                        sizeSlider.transform.parent.gameObject.SetActive(false);
                        heightSlider.transform.parent.gameObject.SetActive(false);
                        
                        if ((partConfig as ClothConfig).ColorIndex1 != -1)
                        {
                            color1Button.gameObject.SetActive(true);
                            // TODO: 让该按钮的图片和当前的颜色配置一样
                        }
                        else color1Button.gameObject.SetActive(false);
                        
                        if ((partConfig as ClothConfig).ColorIndex2 != -1)
                        {
                            color2Button.gameObject.SetActive(true);
                            // TODO: 让该按钮的图片和当前的颜色配置一样
                        }
                        else color2Button.gameObject.SetActive(false);
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
                        throw new ArgumentOutOfRangeException(nameof(partType), partType, null);
                }
            }

            // 更新模型
            if (updateCharacterView)
            {
                CharacterCreator.Instance.SetPart(partConfig); 
            }
        }

        #region 辅助方法
        /// <summary>
        /// 获取当前角色部位配置
        /// </summary>
        private CharacterPartConfigBase GetCurrentCharacterPartConfig()
        {
            var currentPartType = currentFacadeMenuButton.CharacterPartType;
            var currentIndex = part2ConfigDict[(int)currentPartType];
            var currentId = projectConfig.CustomCharacterPartConfigIdDict[currentPartType][currentIndex];
            return ConfigTool.GetCharacterPartConfig(currentPartType, currentId);
        }

        /// <summary>
        /// 获取当前角色部位数据
        /// </summary>
        /// <returns></returns>
        private CustomCharacterPartData GetCurrentCharacterPartData()
        {
            var currentPartType = currentFacadeMenuButton.CharacterPartType;
            return customCharacterData.CustomPartDataDict[(int)currentPartType];
        }
        #endregion
    }
}
