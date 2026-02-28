using System;
using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// 桥接组件：挂在敌人 GameObject 上，监听 ICharacter.OnDeath 并转发给 SpawnPoint。
    /// 由 SpawnPoint.SpawnAsync() 动态 AddComponent，不需要手动挂载。
    /// </summary>
    public class EnemyDeathListener : MonoBehaviour
    {
        private Action _onDied;
        private bool _notified;

        public void Init(ICharacter character, Action onDied)
        {
            _onDied = onDied;
            _notified = false;
            // ICharacter 没有事件字段，使用轮询 isAlive 方案
            // 如果后续 ICharacter 增加 OnDeath 委托，在此处订阅即可
        }

        /// <summary>
        /// 由 BossController / EnemyController 的死亡流程调用。
        /// 建议在死亡状态（DeadState）的 OnEnter 里调用 GetComponent<EnemyDeathListener>()?.NotifyDied()。
        /// </summary>
        public void NotifyDied()
        {
            if (_notified) return;
            _notified = true;
            _onDied?.Invoke();
        }

        // 降级方案：GameObject 被销毁时也触发（以防忘记调用 NotifyDied）
        private void OnDestroy()
        {
            if (!_notified)
            {
                _notified = true;
                _onDied?.Invoke();
            }
        }
    }
}
