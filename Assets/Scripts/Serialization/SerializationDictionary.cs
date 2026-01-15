using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace Serialization
{
    [Serializable]
    public class SerializableDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private List<TKey> keys;
        private List<TValue> values;
        [NonSerialized] private Dictionary<TKey, TValue> dict;
        
        public SerializableDictionary()
        {
            dict = new Dictionary<TKey, TValue>();
        }

        public SerializableDictionary(Dictionary<TKey, TValue> dict)
        {
            this.dict = dict;
        }

        [OnSerializing]
        private void OnSerializing(StreamingContext context)
        {
            keys = new List<TKey>(dict.Count);
            values = new List<TValue>(dict.Count);
            foreach (var item in dict)
            {
                keys.Add(item.Key);
                values.Add(item.Value);
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            dict = new Dictionary<TKey, TValue>(keys.Count);
            for (int i = 0; i < keys.Count; i++)
            {
                dict.Add(keys[i], values[i]);
            }
            keys.Clear();
            values.Clear();
        }

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