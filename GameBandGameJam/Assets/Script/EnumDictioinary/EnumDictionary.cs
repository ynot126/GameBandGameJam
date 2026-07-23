using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


// This base class is just to make CustomPropertyDrawer work.
[Serializable]
public abstract class EnumDictionaryBase
{
}

[Serializable]
public class EnumDictionary<TKey, TValue> : EnumDictionaryBase, ISerializationCallbackReceiver,
    IReadOnlyDictionary<TKey, TValue>
    where TKey : Enum
{
    // This should create a stable set of keys.
    public static readonly TKey[] allKeys = Enum.GetValues(typeof(TKey)).OfType<TKey>().OrderBy(x => x).ToArray();

    [SerializeField]
    internal List<KeyValuePairStruct> pairs; // We are using a list here so that manipulating it would be easy.

    private Dictionary<TKey, TValue> dict = new();

    public EnumDictionary()
    {
        InitializeDefaults();
    }

    public EnumDictionary(IDictionary<TKey, TValue> source)
    {
        InitializeDefaults();
        if (source == null) return;

        foreach (var pair in source)
            this[pair.Key] = pair.Value;
    }

    public EnumDictionary(IEnumerable<KeyValuePair<TKey, TValue>> source)
    {
        InitializeDefaults();
        if (source == null) return;

        foreach (var pair in source)
            this[pair.Key] = pair.Value;
    }

    private void InitializeDefaults()
    {
        foreach (var key in allKeys)
            dict[key] = default;
    }

    // We do not support setting for now, since it is a ReadOnly interface.
    public TValue this[TKey key]
    {
        get => dict[key];
        set => dict[key] = value;
    }

    public void OnBeforeSerialize()
    {
        pairs = allKeys.Select(key => new KeyValuePairStruct
        {
            key = key,
            value = dict.TryGetValue(key, out var value) ? value : default
        }).ToList();
    }

    public void OnAfterDeserialize()
    {
        // Clearing the dict first, since the UnityEditor may reload this class over and over again.
        dict.Clear();
        // We will do a merge between pairs and known keys, and only keep those that are valid.
        var allIndex = 0;
        foreach (var pair in pairs)
        {
            while (allIndex < allKeys.Length && Comparer<TKey>.Default.Compare(allKeys[allIndex], pair.key) < 0)
                dict.Add(allKeys[allIndex++], default);

            if (allIndex >= allKeys.Length || !EqualityComparer<TKey>.Default.Equals(allKeys[allIndex], pair.key))
            {
                Debug.LogError($"EnumDictionary: Encountered undefined key {pair.key} with value {pair.value}");
                continue;
            }

            ++allIndex;
            dict.Add(pair.key, pair.value);
        }
    }

    [Serializable]
    internal struct KeyValuePairStruct
    {
        public TKey key;
        public TValue value;
    }

    #region IReadOnlyDictionary

    public bool ContainsKey(TKey key)
    {
        return dict.ContainsKey(key);
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        return dict.TryGetValue(key, out value);
    }

    public IEnumerable<TKey> Keys => dict.Keys;
    public IEnumerable<TValue> Values => dict.Values;

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return dict.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public int Count => dict.Count;

    #endregion
}