using System;
using UnityEngine;

namespace Enemy
{
    /// <summary>
    /// 桥接组件：挂在敌人 GameObject 上，监听 ICharacter.OnDeath 并转发给 SpawnPoint 和其他订阅者。
    /// 由 SpawnPoint.SpawnAsync() 动态 AddComponent，不需要手动挂载。
    /// </summary>
    public class EnemyDeathListener : MonoBehaviour
    {
        /// <summary>
        /// 对外公开事件，可有多个订阅者（SpawnPoint 的回调、DropOnDeath 等）。
        /// </summary>
        public event Action OnDied;

        private bool _notified;

        public void Init()
        {
            _notified = false;
        }

        /// <summary>
        /// 由 BossController / EnemyController 的死亡流程调用。
        /// 建议在死亡状态（DeadState）的 OnEnter 里调用：
        ///   GetComponent&lt;EnemyDeathListener&gt;()?.NotifyDied();
        /// </summary>
        public void NotifyDied()
        {
            if (_notified) return;
            _notified = true;
            OnDied?.Invoke();
        }

        // IMPORTANT:
        // Do not infer "died" from OnDestroy.
        // Scene unload / app quit also destroys enemy objects, which would incorrectly trigger drops.
        private void OnDestroy() { }
    }
}
