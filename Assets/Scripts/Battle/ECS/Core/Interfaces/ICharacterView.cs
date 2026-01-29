using UnityEngine;

namespace Battle.ECS
{
    /// <summary>
    /// 角色View接口
    /// </summary>
    public interface ICharacterView
    {
        /// <summary>
        /// 同步位置
        /// </summary>
        void SyncPosition(Vector3 position);
        
        /// <summary>
        /// 同步旋转
        /// </summary>
        void SyncRotation(Quaternion rotation);
        
        /// <summary>
        /// 播放动画
        /// </summary>
        void PlayAnimation(string animName);
    }
}