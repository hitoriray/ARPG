using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

/// <summary>
/// 可序列化的字典，支持二进制和JsonUtility
/// </summary>
/// <remarks>如果使用URP或HDRP渲染管线，官方提供的序列化字典为<see cref="UnityEngine.Rendering.SerializedDictionary{K,V}"/>"</remarks>
[Serializable]
public class Serialized_Dic<K, V> : ISerializationCallbackReceiver
{
    [SerializeField] private List<K> keyList;
    [SerializeField] private List<V> valueList;

    [NonSerialized] // 不序列化 避免报错
    private Dictionary<K, V> dictionary;

    public Dictionary<K, V> Dictionary => dictionary ??= new Dictionary<K, V>();

    public Serialized_Dic()
    {
        dictionary = new Dictionary<K, V>();
    }

    public Serialized_Dic(Dictionary<K, V> dictionary)
    {
        this.dictionary = dictionary ?? new Dictionary<K, V>();
    }

    // 序列化的时候把字典里面的内容放进list
    [OnSerializing]
    private void OnSerializing(StreamingContext context)
    {
        OnBeforeSerialize();
    }
    
    // 反序列化时候自动完成字典的初始化
    [OnDeserialized]
    private void OnDeserialized(StreamingContext context)
    {
        OnAfterDeserialize();
    }

    /// <summary>
    /// Unity序列化前调用
    /// </summary>
    public void OnBeforeSerialize()
    {
        var source = Dictionary;
        keyList = new List<K>(source.Keys);
        valueList = new List<V>(source.Values);
    }

    /// <summary>
    /// Unity反序列化后调用
    /// </summary>
    public void OnAfterDeserialize()
    {
        dictionary = new Dictionary<K, V>();
        if (keyList == null || valueList == null) return;

        int count = Mathf.Min(keyList.Count, valueList.Count);
        for (int i = 0; i < count; i++)
        {
            K key = keyList[i];
            if (dictionary.ContainsKey(key))
                continue;
            dictionary.Add(key, valueList[i]);
        }
    }
}
