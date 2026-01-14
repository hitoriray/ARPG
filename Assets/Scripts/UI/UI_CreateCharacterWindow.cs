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
        // 角色预览
        private Transform characterPreviewTransform;

        [SerializeField] public float dragSpeed = 60f;
        private float lastPosX = 0;
        
        // 职业按钮相关
        [SerializeField] private UI_ProfessionButton[] professionButtons; // 所有的职业按钮
        private UI_ProfessionButton currentProfessionButton; // 当前选中的职业按钮

        public override void Init()
        {
            base.Init();
            // 绑定modelTouchImage的拖拽事件
            modelTouchImage.OnDrag(ModelTouchImageDrag);
            // 绑定角色预览
            characterPreviewTransform = PlayerController.Instance.transform;
            
            // 初始化职业按钮
            for (int i = 0; i < professionButtons.Length; i++)
            {
                professionButtons[i].Init(this, (ProfessionType)i); // 需要确保是按枚举顺序填入的四个职业
            }
            // 默认选择第一个职业（战士）
            SelectProfessionButton(professionButtons[0]);
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
            characterPreviewTransform.Rotate(new Vector3(0, -offset * Time.deltaTime * dragSpeed, 0));
        }

        private void Start()
        {
            Init();
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
            // TODO：处理切换职业的业务逻辑
            Debug.Log($"当前选择的职业是：{newProfession.ToString()}");
        }
    }
}
