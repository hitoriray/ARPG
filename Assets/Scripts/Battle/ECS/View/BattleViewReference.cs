using UnityEngine;

namespace Battle.ECS.View
{
    /// <summary>
    /// 视图引用
    /// </summary>
    public class BattleViewReference
    {
        public Camera Camera;
        public ScreenAttachmentController ScreenAttachmentController;
        public PlayerStart PlayerStart;
        public Vector3 LastPos;
    }
}