using System;
using BuffSystem;
using Config;
using UnityEngine;

namespace Player
{
    public class PlayerBuffEffectResolver : BuffEffectResolverBase
    {
        [SerializeField] private PlayerController player;
        
        public override void Resolve(Buff buff, BuffEffectDataBase effectData)
        {
            if (effectData is SimpleBuffEffectData)
            {
                var simpleEffectData = (SimpleBuffEffectData)effectData;
                switch (simpleEffectData.type)
                {
                    case BuffEffectType.Hp:
                        Debug.Log($"由于{buff.config.name}Buff增加Hp:{simpleEffectData.value * buff.stack}");
                        player.CharacterAttribute.AddHp(simpleEffectData.value);
                        break;
                    case BuffEffectType.AttackFixed:
                        Debug.Log($"由于{buff.config.name}Buff增加攻击力值:{simpleEffectData.value * buff.stack}");
                        player.CharacterAttribute.attack.FixedBonus += simpleEffectData.value;
                        break;
                    case BuffEffectType.AttackMultiplier:
                        Debug.Log($"由于{buff.config.name}Buff增加攻击力系数:{simpleEffectData.value * buff.stack}");
                        player.CharacterAttribute.attack.MultiplierBonus += simpleEffectData.value;
                        break;
                }
            }
        }
    }
}