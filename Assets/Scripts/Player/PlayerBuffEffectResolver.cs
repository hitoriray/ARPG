using System;
using Buff;
using Config;
using UnityEngine;

namespace Player
{
    public class PlayerBuffEffectResolver : BuffEffectResolverBase
    {
        [SerializeField] private PlayerController player;
        
        public override void Resolve(Buff.Buff buff, BuffEffectDataBase effectData)
        {
            if (effectData is SimpleBuffEffectData)
            {
                var simpleEffectData = (SimpleBuffEffectData)effectData;
                switch (simpleEffectData.type)
                {
                    case BuffEffectType.Hp:
                        Debug.Log($"由于{buff.config.name}Buff增加Hp:{simpleEffectData.value * buff.stack}");
                        break;
                }
            }
        }
    }
}