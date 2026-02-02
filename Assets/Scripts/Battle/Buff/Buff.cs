// using System;
// using UnityEngine;
// using Config;
// using JKFrame;
//
// namespace BuffSystem
// {
//     public class Buff
//     {
//         public BuffConfig config { get; private set; }
//         public float destroyTimer { get; private set;}
//         public float periodicTimer { get; private set;}
//         public int stack { get; private set;}
//         private Action<Buff> onStart;
//         private Action<Buff> onPeriodic;
//         private Action<Buff> onEnd;
//         private Action<Buff> onUpdate;
//
//         public void Init(BuffConfig config, Action<Buff> onStart, Action<Buff> onPeriodic, Action<Buff> onEnd, Action<Buff> onUpdate)
//         {
//             this.config = config;
//             this.onStart = onStart;
//             this.onPeriodic = onPeriodic;
//             this.onEnd = onEnd;
//             this.onUpdate = onUpdate;
//         }
//
//         public void Start()
//         {
//             destroyTimer = config.duration;
//             periodicTimer = config.tickInterval;
//             onStart?.Invoke(this);
//             RayDebug.Log("onStart生效一次");
//         }
//
//         public void OnUpdate()
//         {
//             // 周期性生效
//             if (onPeriodic != null && config.periodicEffect != null)
//             {
//                 periodicTimer -= Time.deltaTime;
//                 if (periodicTimer <= 0)
//                 {
//                     onPeriodic.Invoke(this);
//                     periodicTimer = config.tickInterval + periodicTimer;
//                 }
//             }
//
//             // 销毁倒计时
//             destroyTimer -= Time.deltaTime;
//             if (destroyTimer <= 0)
//             {
//                 // 层数下降
//                 if (stack > 1)
//                 {
//                     stack -= 1;
//                     destroyTimer = config.duration;
//                 }
//                 // Buff结束
//                 else
//                 {
//                     onEnd?.Invoke(this);
//                     RayDebug.Log("onEnd生效一次");
//                 }
//             }
//             else
//             {
//                 onUpdate?.Invoke(this);
//             }
//         }
//
//         public void Stop()
//         {
//             config = null;
//             onStart = null;
//             onPeriodic = null;
//             onEnd = null;
//             this.ObjectPushPool();
//         }
//
//         public void AddStack(int stack)
//         {
//             if (config.canStack)
//             {
//                 this.stack = Math.Clamp(this.stack + stack, 0, config.maxStack);
//             }
//             else
//             {
//                 this.stack = 1;
//             }
//             // 刷新存在时间
//             destroyTimer = config.duration;
//         }
//     }
// }