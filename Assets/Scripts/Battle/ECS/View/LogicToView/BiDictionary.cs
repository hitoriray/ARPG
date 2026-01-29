using System;
using System.Collections.Generic;

namespace Battle.ECS.View.LogicToView
{
    /// <summary>
    /// 双向字典 
    /// </summary>
    public class BiDictionary<TKey, TValue>
    {
        private Dictionary<TKey, TValue> _keyToValue;
        private Dictionary<TValue, TKey> _valueToKey;
        
        public BiDictionary(int capacity = 16)
        {
            _keyToValue = new Dictionary<TKey, TValue>(capacity);
            _valueToKey = new Dictionary<TValue, TKey>(capacity);
        }

        // 添加键值对（确保键和值均唯一）
        public void Add(TKey key, TValue value)
        {
            if (_keyToValue.ContainsKey(key))
                throw new ArgumentException($"键 {key} 已存在");
            if (_valueToKey.ContainsValue(key))
                throw new ArgumentException($"值 {value} 已存在");

            _keyToValue.Add(key, value);
            _valueToKey.Add(value, key);
        }

        // 通过键找值
        public bool TryGetByKey(TKey key, out TValue value)
            => _keyToValue.TryGetValue(key, out value);

        // 通过值找键
        public bool TryGetByValue(TValue value, out TKey key)
            => _valueToKey.TryGetValue(value, out key);

        // 移除键值对（同步更新两个字典）
        public bool TryRemoveByKey(TKey key)
        {
            if (_keyToValue.Remove(key, out TValue value))
            {
                _valueToKey.Remove(value);
                return true;
            }
            return false;
        }
        
        // 移除键值对（同步更新两个字典）
        public bool TryRemoveByValue(TValue value)
        {
            if (_valueToKey.Remove(value, out TKey key))
            {
                _keyToValue.Remove(key);
                return true;
            }
            return false;
        }
        
        public void Clear()
        {
            _keyToValue.Clear();
            _valueToKey.Clear();
        }
    }
}