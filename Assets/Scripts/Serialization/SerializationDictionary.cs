using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Serialization
{
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ISerializationCallbackReceiver
    {
        [SerializeField] private List<TKey> keys;
        [SerializeField] private List<TValue> values;
        [NonSerialized] private Dictionary<TKey, TValue> dict;

        public SerializableDictionary()
        {
            keys = new List<TKey>();
            values = new List<TValue>();
            dict = new Dictionary<TKey, TValue>();
        }

        public SerializableDictionary(int capacity)
        {
            keys = new List<TKey>(capacity);
            values = new List<TValue>(capacity);
            dict = new Dictionary<TKey, TValue>(capacity);
        }

        #region ISerializationCallbackReceiver
        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();

            keys.Capacity = dict.Count;
            values.Capacity = dict.Count;
            
            foreach (var kvp in dict)
            {
                keys.Add(kvp.Key);
                values.Add(kvp.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            dict.Clear();
            int count = Math.Min(keys.Count, values.Count);
            dict = new Dictionary<TKey, TValue>(count);
            for (int i = 0; i < count; i++)
            {
                if (keys[i] != null && !dict.ContainsKey(keys[i]))
                {
                    dict.Add(keys[i], values[i]);
                }
            }
        }
        #endregion

        #region IDictionary Implementation
        public TValue this[TKey key] { get => dict[key]; set => dict[key] = value; }
        public ICollection<TKey> Keys => dict.Keys;
        public ICollection<TValue> Values => dict.Values;
        public int Count => dict.Count;
        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value) => dict.Add(key, value);
        public void Add(KeyValuePair<TKey, TValue> item) => ((IDictionary<TKey, TValue>)dict).Add(item);
        public void Clear() => dict.Clear();
        public bool Contains(KeyValuePair<TKey, TValue> item) => ((IDictionary<TKey, TValue>)dict).Contains(item);
        public bool ContainsKey(TKey key) => dict.ContainsKey(key);
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((IDictionary<TKey, TValue>)dict).CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dict.GetEnumerator();
        public bool Remove(TKey key) => dict.Remove(key);
        public bool Remove(KeyValuePair<TKey, TValue> item) => ((IDictionary<TKey, TValue>)dict).Remove(item);
        public bool TryGetValue(TKey key, out TValue value) => dict.TryGetValue(key, out value);
        IEnumerator IEnumerable.GetEnumerator() => dict.GetEnumerator();
        #endregion
    }
}