using System.Collections.Generic;
using UnityEngine;

namespace BT
{
    /// <summary>
    /// 简易事件缓冲（临时占位，后续可接入正式事件系统）
    /// </summary>
    public static class BossAIEventBuffer
    {
        private static readonly Dictionary<string, float> eventExpiry = new();

        public static void Raise(string eventKey, float validSeconds = 0.2f)
        {
            if (string.IsNullOrEmpty(eventKey))
                return;

            eventExpiry[eventKey] = Time.time + Mathf.Max(0.01f, validSeconds);
        }

        public static bool Peek(string eventKey)
        {
            if (string.IsNullOrEmpty(eventKey))
                return false;

            if (eventExpiry.TryGetValue(eventKey, out var expireTime))
            {
                if (Time.time <= expireTime)
                    return true;

                eventExpiry.Remove(eventKey);
            }
            return false;
        }

        public static bool Consume(string eventKey)
        {
            if (!Peek(eventKey))
                return false;

            eventExpiry.Remove(eventKey);
            return true;
        }

        public static void Clear(string eventKey)
        {
            if (string.IsNullOrEmpty(eventKey))
                return;

            eventExpiry.Remove(eventKey);
        }
    }
}
