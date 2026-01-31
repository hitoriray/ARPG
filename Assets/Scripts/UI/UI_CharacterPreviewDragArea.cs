using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    public class UI_CharacterPreviewDragArea : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private UI_CharacterPreviewStage previewStage;
        [SerializeField] private float dragSpeed = 0.5f;

        private float lastX;
        private bool dragging;

        public void OnBeginDrag(PointerEventData eventData)
        {
            dragging = true;
            lastX = eventData.position.x;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!dragging || previewStage == null)
            {
                return;
            }

            float delta = eventData.position.x - lastX;
            lastX = eventData.position.x;
            previewStage.RotateBy(delta * dragSpeed);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            dragging = false;
        }
    }
}
