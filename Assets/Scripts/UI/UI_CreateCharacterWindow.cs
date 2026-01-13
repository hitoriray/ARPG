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

        public override void Init()
        {
            base.Init();
            // 绑定modelTouchImage的拖拽事件
            modelTouchImage.OnDrag(ModelTouchImageDrag);
            // 绑定角色预览
            characterPreviewTransform = PlayerController.Instance.transform;
        }

        /// <summary>
        /// 当模型图片交互区域拖拽时的回调
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="args"></param>
        private void ModelTouchImageDrag(PointerEventData eventData, object[] args)
        {
            //print(eventData.position);
            float offset = eventData.position.x - lastPosX;
            lastPosX = eventData.position.x;
            characterPreviewTransform.Rotate(new Vector3(0, -offset * Time.deltaTime * dragSpeed, 0));
        }

        private void Start()
        {
            Init();
        }

    }
}
