using Arch.Core;
using Arch.Core.Extensions;
using Attribute;
using Battle.ECS.Component;
using Battle.ECS.Core.Helper;
using Config;
using FixMath;
using UnityEngine;

namespace Battle.ECS.Core.Process
{
    public class BuffProcess : ProcessBase, IDeathProcess, ITickProcess
    {
        public BuffProcess(BattleContext context) : base()
        {
            Init(context);
        }

        /// <summary>
        /// Buff创建时回调
        /// </summary>
        public void OnCreate(in Entity buffEntity)
        {
            if (buffEntity.IsAlive() == false) return;
            ref var buff = ref buffEntity.TryGetRef<Buff>(out var hasBuff);
            if (hasBuff == false)
                return;
            
            var config = buff.Config;
            if (config.speedPctModifier != 0)
            {
                ModifierHelper.CreateSpeedPctModifier(Context, nameof(BuffProcess), buffEntity, buff.Target, (FP)config.speedPctModifier);
            }

            RayDebug.Log($"Buff创建: {config.buffName} (ID:{config.buffId})");
        }

        /// <summary>
        /// Buff堆叠添加时回调
        /// </summary>
        public void OnStackAdded(in Entity buffEntity, int count)
        {
            if (!buffEntity.IsAlive()) return;

            ref var buff = ref buffEntity.Get<Buff>();
            RayDebug.Log($"Buff叠加: {buff.Config.buffName} +{count}层");
        }

        /// <summary>
        /// Buff堆叠移除时回调
        /// </summary>
        public void OnStackRemoved(in Entity buffEntity, int count)
        {
            if (!buffEntity.IsAlive()) return;

            ref var buff = ref buffEntity.Get<Buff>();
            RayDebug.Log($"Buff减少: {buff.Config.buffName} -{count}层");
        }

        /// <summary>
        /// Buff Tick时回调（周期性效果）
        /// </summary>
        public void OnTick(in Entity buffEntity)
        {
            if (!buffEntity.IsAlive()) return;

            ref var buff = ref buffEntity.Get<Buff>();
            ref var buffStack = ref buffEntity.Get<BuffStack>();
            var config = buff.Config;

            BuffHelper.ApplyPeriodicAttrModifiers(buffEntity, buff.Target, buffStack.Value.Count);
            RayDebug.Log($"{config.buffName} x{buffStack.Value.Count}层");
        }

        /// <summary>
        /// Buff死亡时回调（堆叠为0或目标死亡）
        /// </summary>
        public void OnDeath(in Entity buffEntity)
        {
            if (!buffEntity.IsAlive()) return;

            ref var buff = ref buffEntity.Get<Buff>();
            var config = buff.Config;

            // 注意：StartAttrModifiers 已在 RemoveStack 中逐层撤销
            // 这里只需处理: 移除BuffList引用、销毁Modifier实体、清理资源
            
            // 清理以该Buff为Source的Modifier实体（如SpeedPctModifier）
            ModifierHelper.RemoveAllModifiersBySource(Context, buffEntity);

            // 从目标的BuffList中移除
            ref var buffList = ref buff.Target.TryGetRef<BuffList>(out var hasBuffList);
            if (buff.Target.IsAlive() && hasBuffList)
            {
                buffList.Remove(buffEntity);
            }

            // 标记销毁
            if (!buffEntity.Has<Destroy>())
            {
                buffEntity.Add(new Destroy());
            }

            RayDebug.Log($"Buff销毁: {config.buffName}");
        }
    }
}