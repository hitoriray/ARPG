using System;
using System.Collections.Generic;
using Config;
using Data;
using JKFrame;
using Serialization;
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
        private float lastPosX;
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
        
        // 返回、确认按钮
        [SerializeField] private Button backButton;
        [SerializeField] private Button sureButton;
        
        // 自定义角色的数据
        private static CustomCharacterData CustomCharacterData => DataManager.CustomCharacterData;
        
        // 全局项目配置
        private ProjectConfig projectConfig;
        // 玩家当前每一个部位选择的是projectConfig中的第几个索引的配置
        private Dictionary<int, int> part2ConfigDict;
        
        public CharacterPartType CurrentCharacterPartType => currentFacadeMenuButton.CharacterPartType;
        public ProfessionType CurrentProfessionType => currentProfessionButton.Profession;
        
        #region UI_WindowBase Implementation
        public override void Init()
        {
            base.Init();
            
            // 获取配置
            projectConfig = ConfigManager.Instance.GetConfig<ProjectConfig>(ConfigTool.ProjectConfigName);
            part2ConfigDict = new(3)
            {
                { (int)CharacterPartType.Hair, 0 },
                { (int)CharacterPartType.Face, 0 },
                { (int)CharacterPartType.Cloth, 0 }
            };

            BindEvents();

            // 初始化外观部位按钮
            facadeMenuButtons[0].Init(this, CharacterPartType.Face);
            facadeMenuButtons[1].Init(this, CharacterPartType.Hair);
            facadeMenuButtons[2].Init(this, CharacterPartType.Cloth);
            SelectFacadeMenuButton(facadeMenuButtons[0]);
            // 应用默认的部位配置
            SetCharacterPart(CharacterPartType.Face, CustomCharacterData.CustomPartDataDict[(int)CharacterPartType.Face].Index, true, true);
            SetCharacterPart(CharacterPartType.Hair, CustomCharacterData.CustomPartDataDict[(int)CharacterPartType.Hair].Index, false, true);
            SetCharacterPart(CharacterPartType.Cloth, CustomCharacterData.CustomPartDataDict[(int)CharacterPartType.Cloth].Index, false, true);

            // 初始化职业按钮
            for (int i = 0; i < professionButtons.Length; i++)
            {
                professionButtons[i].Init(this, (ProfessionType)i); // 需要确保是按枚举顺序填入的四个职业
            }
            // 默认选择第一个职业（战士）
            SelectProfessionButton(professionButtons[0]);
        }
        
        public override void OnClose()
        {
            base.OnClose();
            // 释放自身资源
            ResManager.ReleaseInstance(gameObject);
        }
        
        #endregion

        private void BindEvents()
        {
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
            // 绑定返回、确认按钮
            backButton.onClick.AddListener(OnBackBtnClicked);
            sureButton.onClick.AddListener(OnSureBtnClicked);
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
            SetNextCharacterPart(-1, CurrentCharacterPartType);
        }

        private void OnRightArrowBtnClicked()
        {
            SetNextCharacterPart(1, CurrentCharacterPartType);
        }
        
        private void OnSizeSliderValueChanged(float value)
        {
            GetCharacterPartData(CurrentCharacterPartType).Size = value;
            CharacterCreator.Instance.SetSize(CurrentCharacterPartType, value);
        }
        
        private void OnHeightSliderValueChanged(float value)
        {
            GetCharacterPartData(CurrentCharacterPartType).Height = value;
            CharacterCreator.Instance.SetHeight(CurrentCharacterPartType, value);
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
            var currentPartData = GetCharacterPartData(CurrentCharacterPartType);
            currentPartData.Color1 = newColor.ConvertToSerializationColor();
            // 2.修改颜色
            CharacterCreator.Instance.SetColor1(CurrentCharacterPartType, newColor);
            // 3.修改自身按钮颜色
            color1Button.image.color = new Color(newColor.r, newColor.g, newColor.b, 0.6f);
        }

        private void OnColor2Selected(Color newColor)
        {
            // 1.存储数据
            var currentPartData = GetCharacterPartData(CurrentCharacterPartType);
            currentPartData.Color2 = newColor.ConvertToSerializationColor();
            // 2.修改颜色
            CharacterCreator.Instance.SetColor2(CurrentCharacterPartType, newColor);
            // 3.修改自身按钮颜色
            color2Button.image.color = new Color(newColor.r, newColor.g, newColor.b, 0.6f);
        }

        private void OnBackBtnClicked()
        {
            Close();
            SceneManager.LoadScene("Menu");
        }

        private void OnSureBtnClicked()
        {
            Close();
            // 保存数据
            DataManager.SaveCustomCharacterData();
            // 进入游戏场景
            SceneManager.LoadScene("Game");
        }
        
        #endregion
        
        #region 业务逻辑
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
            
            // 检查服装是否与职业相符
            var config = GetCharacterPartConfig(CharacterPartType.Hair);
            if (!config.ProfessionTypes.Contains(newProfession))
            {
                SetNextCharacterPart(1, CharacterPartType.Hair, CurrentCharacterPartType == CharacterPartType.Hair);
            }
            
            config = GetCharacterPartConfig(CharacterPartType.Face);
            if (!config.ProfessionTypes.Contains(newProfession))
            {
                SetNextCharacterPart(1, CharacterPartType.Face, CurrentCharacterPartType == CharacterPartType.Face);
            }
            
            config = GetCharacterPartConfig(CharacterPartType.Cloth);
            if (!config.ProfessionTypes.Contains(newProfession))
            {
                SetNextCharacterPart(1, CharacterPartType.Cloth, CurrentCharacterPartType == CharacterPartType.Cloth);
            }
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
            var currentIndex = part2ConfigDict[(int)CurrentCharacterPartType];
            SetCharacterPart(CurrentCharacterPartType, projectConfig.CustomCharacterPartConfigIdDict[CurrentCharacterPartType][currentIndex], true, false);
        }

        /// <summary>
        /// 设置角色的具体部位
        /// </summary>
        private void SetCharacterPart(CharacterPartType partType, int configIndex, bool updateUIView = false, bool updateCharacterView = false)
        {
            // 获取配置文件（这个配置文件的资源释放时机由PlayerView来决定）
            var partConfig = ConfigTool.LoadCharacterPartConfig(partType, configIndex);

            // 更新UI
            if (updateUIView)
            {
                partNameText.text = partConfig.Name;
                switch (partType)
                {
                    case CharacterPartType.Hair:
                        // 隐藏尺寸、高度、Color2
                        sizeSlider.transform.parent.gameObject.SetActive(false);
                        heightSlider.transform.parent.gameObject.SetActive(false);
                        color2Button.gameObject.SetActive(false);
                        
                        if (((HairConfig)partConfig).ColorIndex != -1)
                        {
                            color1Button.gameObject.SetActive(true);
                            // 让该按钮的图片和当前的颜色配置一样
                            var color = CustomCharacterData.CustomPartDataDict[(int)partType].Color1;
                            color1Button.image.color = new Color(color.r, color.g, color.b, 0.6f);
                        }
                        else
                        {
                            color1Button.gameObject.SetActive(false);
                        }
                        break;
                    case CharacterPartType.Face:
                        // 尺寸
                        sizeSlider.transform.parent.gameObject.SetActive(true);
                        sizeSlider.value = CustomCharacterData.CustomPartDataDict[(int)partType].Size;
                        sizeSlider.minValue = 0.9f;
                        sizeSlider.maxValue = 1.2f;
                        // 高度
                        heightSlider.transform.parent.gameObject.SetActive(true);
                        heightSlider.value = CustomCharacterData.CustomPartDataDict[(int)partType].Height;
                        heightSlider.minValue = 0;
                        heightSlider.maxValue = 0.1f;
                        // 隐藏颜色按钮
                        color1Button.gameObject.SetActive(false);
                        color2Button.gameObject.SetActive(false);
                        break;
                    case CharacterPartType.Cloth:
                        sizeSlider.transform.parent.gameObject.SetActive(false);
                        heightSlider.transform.parent.gameObject.SetActive(false);
                        
                        if (((ClothConfig)partConfig).ColorIndex1 != -1)
                        {
                            color1Button.gameObject.SetActive(true);
                            // 让该按钮的图片和当前的颜色配置一样
                            var color = CustomCharacterData.CustomPartDataDict[(int)partType].Color1;
                            color1Button.image.color = new Color(color.r, color.g, color.b, 0.6f);
                        }
                        else color1Button.gameObject.SetActive(false);
                        
                        if (((ClothConfig)partConfig).ColorIndex2 != -1)
                        {
                            color2Button.gameObject.SetActive(true);
                            // 让该按钮的图片和当前的颜色配置一样
                            var color = CustomCharacterData.CustomPartDataDict[(int)partType].Color2;
                            color2Button.image.color = new Color(color.r, color.g, color.b, 0.6f);
                        }
                        else color2Button.gameObject.SetActive(false);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(partType), partType, null);
                }
            }

            // 更新角色模型
            CharacterCreator.Instance.SetPart(partConfig, updateCharacterView);
            
            // 保存数据
            CustomCharacterData.CustomPartDataDict[(int)partType].Index = configIndex;
        }
        
        /// <summary>
        /// 设置下一个角色部位
        /// </summary>
        /// <param name="increase">增量（-1表示切换到左边，1表示切换到右边）</param>
        /// <param name="currentPartType"></param>
        /// <param name="updateUI"></param>
        private void SetNextCharacterPart(int increase, CharacterPartType currentPartType, bool updateUI = true)
        {
            int currentIndex = part2ConfigDict[(int)currentPartType];
            var currentPartConfigIds = projectConfig.CustomCharacterPartConfigIdDict[currentPartType];
            currentIndex += increase;
            if (currentIndex < 0) currentIndex = currentPartConfigIds.Count - 1;
            else if (currentIndex >= currentPartConfigIds.Count) currentIndex = 0;

            // 检查职业有效性
            var currentProfession = CurrentProfessionType;
            var partConfig = ConfigTool.LoadCharacterPartConfig(currentPartType, currentPartConfigIds[currentIndex]);
            while (!partConfig.ProfessionTypes.Contains(currentProfession))
            {
                currentIndex += increase;
                if (currentIndex < 0) currentIndex = currentPartConfigIds.Count - 1;
                else if (currentIndex >= currentPartConfigIds.Count) currentIndex = 0;
                
                // 释放资源并且重新获取新资源
                ResManager.Release(partConfig);
                partConfig = ConfigTool.LoadCharacterPartConfig(currentPartType, currentPartConfigIds[currentIndex]);
            }
            // 释放最后一次的资源
            ResManager.Release(partConfig);
            
            // 说明职业有效
            part2ConfigDict[(int)currentPartType] = currentIndex;
            SetCharacterPart(currentPartType, currentPartConfigIds[currentIndex], updateUI, true);
            AudioManager.Instance.PlayOnShot(arrowAudioClip, Vector3.zero, 1, false);
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 获取角色部位配置
        /// </summary>
        private CharacterPartConfigBase GetCharacterPartConfig(CharacterPartType partType)
        {
            return CharacterCreator.Instance.GetCharacterPartConfig(partType);
        }

        /// <summary>
        /// 获取角色部位数据
        /// </summary>
        private CustomCharacterPartData GetCharacterPartData(CharacterPartType partType)
        {
            return CustomCharacterData.CustomPartDataDict[(int)partType];
        }
        #endregion
    }
}
