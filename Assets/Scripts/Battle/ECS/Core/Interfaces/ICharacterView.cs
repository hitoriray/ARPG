using UnityEngine;

namespace Battle.ECS
{
    /// <summary>
    /// 角色View接口
    /// </summary>
    public abstract class ICharacterView : MonoBehaviour
    {
        /// <summary>
        /// 同步位置
        /// </summary>
        public virtual void SyncPosition(Vector3 position)
        {
        }

        /// <summary>
        /// 同步旋转
        /// </summary>
        public virtual void SyncRotation(Quaternion rotation)
        {
        }

        /// <summary>
        /// 播放动画
        /// </summary>
        public virtual void PlayAnimation(string animName)
        {
        }
    }
}