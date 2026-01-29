using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.LowLevel;
using Battle.ECS.Core.Helper;
using Battle.ECS.Core.Interfaces;

namespace Battle.ECS.Component
{
    /// <summary>
    /// 实体的Buff列表
    /// </summary>
    public struct BuffList : IRelease
    {
        public UnsafeList<Entity> Value;
        public bool Destroyed { get; private set; }

        // 事件计数
        public int HurtEvent;
        public int DealDamageEvent;
        public int HurtModifierEvent;
        public int DealDamageModifierEvent;
        public int OnCastEvent;
        public int HealedEvent;
        public int AddShieldEvent;
        public int TargetDeathEvent;

        public BuffList(int capacity)
        {
            if (capacity <= 0) capacity = 1;
            Value = new UnsafeList<Entity>(capacity);
            HurtEvent = 0;
            DealDamageEvent = 0;
            HurtModifierEvent = 0;
            DealDamageModifierEvent = 0;
            OnCastEvent = 0;
            HealedEvent = 0;
            AddShieldEvent = 0;
            TargetDeathEvent = 0;
            Destroyed = false;
        }

        public void Destroy()
        {
            Value.Clear();
            Destroyed = true;
        }

        public void Release()
        {
            Value.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Entity GetBuff(int buffId)
        {
            foreach (var buffEntity in Value)
            {
                ref var buff = ref buffEntity.Get<Buff>();
                if (buff.ID == buffId)
                    return buffEntity;
            }
            return Entity.Null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddBuff(Entity buffEntity)
        {
            if (Destroyed) ThrowHelper.Throw($"BuffList has been destroyed and cannot add new buff.");
            Value.Add(buffEntity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Remove(in Entity buffEntity)
        {
            return Value.Remove(buffEntity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasBuff(int buffId)
        {
            foreach (var buffEntity in Value)
            {
                ref var buff = ref buffEntity.Get<Buff>();
                if (buff.Config.buffId == buffId)
                    return true;
            }
            return false;
        }
    }
}