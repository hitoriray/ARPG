using UnityEngine;

namespace Battle.ECS.View
{
    public class ScreenAttachmentController : MonoBehaviour
    {
        [SerializeField] private Camera _camera;
        private static readonly float _halfSize = 16.24f / 2f;

        private void Update()
        {
            var scale = _camera.orthographicSize / _halfSize;
            transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}