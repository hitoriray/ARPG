// using System.Collections.Generic;
// using Config;
// using JKFrame;
// using Sirenix.OdinInspector;
// using UnityEngine;
//
// namespace BuffSystem
// {
//     public class BuffController : MonoBehaviour
//     {
//         [ShowInInspector] protected Dictionary<BuffConfig, Buff> buffDict = new();
//         [SerializeField] protected BuffEffectResolverBase buffEffectResolver;
//         protected List<Buff> destroyBuffs = new();
//
//         private void Update()
//         {
//             foreach (var buff in buffDict.Values)
//             {
//                 buff.OnUpdate();
//                 if (buff.destroyTimer <= 0)
//                 {
//                     destroyBuffs.Add(buff);
//                 }
//             }
//             foreach (var buff in destroyBuffs)
//             {
//                 buffDict.Remove(buff.config);
//                 buff.Stop();
//             }
//             destroyBuffs.Clear();
//         }
//         
//         [Button]
//         public Buff AddBuff(BuffConfig buffConfig, int stack = 1)
//         {
//             if (buffDict.TryGetValue(buffConfig, out var buff))
//             {
//                 buff.AddStack(stack);
//             }
//             else
//             {
//                 buff = ResSystem.GetOrNew<Buff>();
//                 buff.Init(buffConfig, OnBuffStart, OnBuffPeriodic, OnBuffEnd, OnBuffUpdate);
//                 buff.AddStack(stack);
//                 buff.Start();
//                 buffDict.Add(buffConfig, buff);
//             }
//
//             return buff;
//         }
//
//         public void ClearBuff()
//         {
//             foreach (var buff in buffDict.Values)
//             {
//                 buff.Stop();
//             }
//             buffDict.Clear();
//         }
//         
//         protected virtual void OnBuffStart(Buff buff)
//         {
//             if (buff.config.startEffect != null)
//             {
//                 buffEffectResolver.Resolve(buff, buff.config.startEffect);
//             }
//         }
//
//         protected virtual void OnBuffPeriodic(Buff buff)
//         {
//             buffEffectResolver.Resolve(buff, buff.config.periodicEffect);
//         }
//
//         protected virtual void OnBuffEnd(Buff buff)
//         {
//             if (buff.config.endEffect != null)
//             {
//                 buffEffectResolver.Resolve(buff, buff.config.endEffect);
//             }
//         }
//
//         protected virtual void OnBuffUpdate(Buff buff)
//         {
//         }
//     }
// }