using System;
using UnityEngine;
using Config;
using JKFrame;

namespace Buff
{
    public class Buff
    {
        public BuffConfig config { get; private set; }
        public float destroyTimer { get; private set;}
        public float periodicTimer { get; private set;}
        public int stack { get; private set;}
        private Action<Buff> onStart;
        private Action<Buff> onPeriodic;
        private Action<Buff> onEnd;

        public void Init(BuffConfig config, Action<Buff> onStart, Action<Buff> onPeriodic, Action<Buff> onEnd)
        {
            this.config = config;
            this.onStart = onStart;
            this.onPeriodic = onPeriodic;
            this.onEnd = onEnd;
        }

        public void Start()
        {
            destroyTimer = config.duration;
            periodicTimer = config.periodicTime;
            stack = 1;
            onStart?.Invoke(this);
            Debug.Log("onStart生效一次");
        }

        public void OnUpdate()
        {
            // 周期性生效
            if (onPeriodic != null)
            {
                periodicTimer -= Time.deltaTime;
                if (periodicTimer <= 0)
                {
                    onPeriodic?.Invoke(this);
                    periodicTimer = config.periodicTime + periodicTimer;
                    Debug.Log("onPeriodic生效一次");
                }
            }

            // 销毁倒计时
            destroyTimer -= Time.deltaTime;
            if (destroyTimer <= 0)
            {
                // Buff结束
                onEnd?.Invoke(this);
                Debug.Log("onEnd生效一次");
            }
        }

        public void Stop()
        {
            config = null;
            onStart = null;
            onPeriodic = null;
            onEnd = null;
            this.ObjectPushPool();
        }

        public void AddLayer(int stack)
        {
            if (config.canStack)
            {
                this.stack = Math.Clamp(this.stack + stack, 0, config.maxStack);
            }
            // 刷新存在时间
            destroyTimer = config.duration;
        }
    }
}