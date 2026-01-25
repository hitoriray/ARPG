using System.Collections.Generic;
using Config;
using JKFrame;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Buff
{
    public class BuffController : MonoBehaviour
    {
        [ShowInInspector] private Dictionary<BuffConfig, Buff> buffDict = new();
        [SerializeField] private BuffEffectResolverBase buffEffectResolver;
        private List<Buff> destroyBuffs = new();

        private void Update()
        {
            foreach (var buff in buffDict.Values)
            {
                buff.OnUpdate();
                if (buff.destroyTimer <= 0)
                {
                    destroyBuffs.Add(buff);
                }
            }
            foreach (var buff in destroyBuffs)
            {
                buffDict.Remove(buff.config);
                buff.Stop();
            }
            destroyBuffs.Clear();
        }
        
        [Button]
        public Buff AddBuff(BuffConfig buffConfig, int layer = -1)
        {
            if (buffDict.TryGetValue(buffConfig, out var buff))
            {
                buff.AddLayer(layer);
            }
            else
            {
                buff = ResSystem.GetOrNew<Buff>();
                buff.Init(buffConfig, OnBuffStart, OnBuffPeriodic, OnBuffEnd);
                buff.Start();
                buffDict.Add(buffConfig, buff);
            }

            return buff;
        }

        public void ClearBuff()
        {
            foreach (var buff in buffDict.Values)
            {
                buff.Stop();
            }
            buffDict.Clear();
        }
        
        private void OnBuffStart(Buff buff)
        {
            buffEffectResolver.Resolve(buff, buff.config.startEffect);
        }

        private void OnBuffPeriodic(Buff buff)
        {
            buffEffectResolver.Resolve(buff, buff.config.periodicEffect);
        }

        private void OnBuffEnd(Buff buff)
        {
            buffEffectResolver.Resolve(buff, buff.config.endEffect);
        }
    }
}