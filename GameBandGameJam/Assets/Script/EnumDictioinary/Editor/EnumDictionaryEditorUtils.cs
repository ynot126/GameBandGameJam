using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

public static class EnumDictionaryEditorUtils
{
    private const string allKeysFieldName = nameof(EnumDictionary<DummyKey, int>.allKeys);
    public const string pairsPath = nameof(EnumDictionary<DummyKey, int>.pairs);
    private const string keyPath = nameof(EnumDictionary<DummyKey, int>.KeyValuePairStruct.key);
    public const string valuePath = nameof(EnumDictionary<DummyKey, int>.KeyValuePairStruct.value);

    internal static Array RectifyPropertyIfNeeded(FieldInfo fieldInfo, SerializedProperty property)
    {
        // Returns a list of item pairs according to the keys in the dictionary.

        // First we need to resolve the actual enum dictionary type.
        var fieldType = fieldInfo.FieldType;
        if (fieldType.IsArray)
            fieldType = fieldType.GetElementType()!;
        else if (property.propertyPath.EndsWith("]") && fieldType.IsGenericType)
            // Use the last generic type as the type?
            fieldType = fieldType.GetGenericArguments().Last();

        var allKeysField = fieldType.GetField(allKeysFieldName);
        var allKeys = (Array)allKeysField.GetValue(null);

        // Now go through the property.
        var pairsProperty = property.FindPropertyRelative(pairsPath);
        var numPairs = pairsProperty.arraySize;

        var hasChanged = false;

        // We want to ensure that the dictionary is well ordered, not having anything that is wrong or bad.
        // To do that, we will traverse the pairs and "sort" them. The ordering will be following just the
        // intValue, which should align with the standard enum ordering reported by allKeys.
        // After sorting the existing entries, we should also fill in any gap there may be.

        // To sort the pairs, we need to go through them from left to right. If the current pair really goes
        // behind all previous pairs, we can leave it alone. Otherwise, we should move it to its rightful
        // positions among all its predecessors. This is basically insertion sort. Unfortunately, there is
        // really no good data structure in c# that allow us to do this efficiently. No O(log n) lower_bound
        // or anything similar in the standard library. For that, we revert to using a stupid list and just
        // move element one at a time. The rationale is that this should be a very rare operation, and that
        // the moveArrayElement function is unlikely to be logarithmic any way.

        var sortedKeys = new List<int>();
        for (var i = 0; i < numPairs;)
        {
            var pairProperty = pairsProperty.GetArrayElementAtIndex(i);
            var key = pairProperty.FindPropertyRelative(keyPath).enumValueIndex;
            // Find the rightful index of the key.
            var targetIndex = sortedKeys.Count;
            while (targetIndex > 0 && sortedKeys[targetIndex - 1] > key) --targetIndex;
            // targetIndex is where we should put this pair. However, there is actually a catch.
            // If there is a duplicate key, we should remove the current pair.
            if (targetIndex > 0 && sortedKeys[targetIndex - 1] == key)
            {
                hasChanged = true;
                pairsProperty.DeleteArrayElementAtIndex(i);
                --numPairs;
            }
            else
            {
                // Let's see if we need to swap things around.
                sortedKeys.Insert(targetIndex, key);
                if (targetIndex != i)
                {
                    pairsProperty.MoveArrayElement(i, targetIndex);
                    hasChanged = true;
                }

                ++i;
            }
        }

        // Let's see if there are extra keys or missing ones.
        var allKeyIndex = 0;
        var numAvailableKeys = allKeys.Length;
        var pairIndex = 0;
        while (pairIndex < numPairs && allKeyIndex < numAvailableKeys)
        {
            var pairProperty = pairsProperty.GetArrayElementAtIndex(pairIndex);
            var currKey = pairProperty.FindPropertyRelative(keyPath).intValue;
            var expectedKey = (int)allKeys.GetValue(allKeyIndex);
            if (currKey == expectedKey)
            {
                ++pairIndex;
                ++allKeyIndex;
            }
            else if (currKey < expectedKey)
            {
                // The currKey shouldn't exist.
                pairsProperty.DeleteArrayElementAtIndex(pairIndex);
                --numPairs;
                hasChanged = true;
            }
            else
            {
                // The currKey is too big.
                pairsProperty.InsertArrayElementAtIndex(pairIndex);
                ++numPairs;
                var newPairProperty = pairsProperty.GetArrayElementAtIndex(pairIndex);
                newPairProperty.FindPropertyRelative(keyPath).intValue = expectedKey;
                // The value will just be a default.
                ++allKeyIndex;
                hasChanged = true;
            }
        }

        while (pairIndex < numPairs)
        {
            // These must all be extra.
            // We are popping from the back because that's probably the fastest way to remove something from a list.
            pairsProperty.DeleteArrayElementAtIndex(--numPairs);
            hasChanged = true;
        }

        while (allKeyIndex < numAvailableKeys)
        {
            pairsProperty.InsertArrayElementAtIndex(numPairs);
            var newPairProperty = pairsProperty.GetArrayElementAtIndex(numPairs++);
            newPairProperty.FindPropertyRelative(keyPath).intValue = (int)allKeys.GetValue(allKeyIndex++);
            // The value will just be a default.
            hasChanged = true;
        }

        if (hasChanged) property.serializedObject.ApplyModifiedProperties();

        return allKeys;
    }

    public static SerializedProperty GetValueProperty(SerializedProperty property, int targetKeyValue)
    {
        // We need to figure out the field info for this particular property.
        var fieldInfo = DeriveFieldInfo(property);
        RectifyPropertyIfNeeded(fieldInfo, property);
        // Now we go through the pairs and find the entry with intKeyValue, or insert one if there is not one.
        var pairsProperty = property.FindPropertyRelative(pairsPath);
        var numPairs = pairsProperty.arraySize;
        for (var index = 0; index < numPairs; ++index)
        {
            var pairProperty = pairsProperty.GetArrayElementAtIndex(index);
            var keyValue = pairProperty.FindPropertyRelative(keyPath).intValue;
            if (keyValue == targetKeyValue) return pairProperty.FindPropertyRelative(valuePath);
            if (keyValue < targetKeyValue) continue;
            pairsProperty.InsertArrayElementAtIndex(index);
            pairProperty = pairsProperty.GetArrayElementAtIndex(index);
            pairProperty.FindPropertyRelative(keyPath).intValue = targetKeyValue;
            return pairProperty.FindPropertyRelative(valuePath);
        }

        // Insert one at the end.
        pairsProperty.InsertArrayElementAtIndex(numPairs);
        var finalPairProperty = pairsProperty.GetArrayElementAtIndex(numPairs);
        finalPairProperty.FindPropertyRelative(keyPath).intValue = targetKeyValue;
        return finalPairProperty.FindPropertyRelative(valuePath);
    }

    private static FieldInfo DeriveFieldInfo(SerializedProperty property)
    {
        var rootType = property.serializedObject.targetObject.GetType()!;
        var components = property.propertyPath.Split('.');
        var currType = rootType;
        var componentIndex = 0;
        const BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        while (componentIndex + 1 < components.Length)
        {
            var fieldName = RectifyFieldName(components[componentIndex++]);
            currType = currType.GetField(fieldName, bindingFlags)!.FieldType;
        }

        return currType.GetField(RectifyFieldName(components[componentIndex]), bindingFlags)!;
    }

    private static string RectifyFieldName(string fieldName)
    {
        // Extract the part before '[', if any.
        var bracketIndex = fieldName.IndexOf('[');
        if (bracketIndex != -1) fieldName = fieldName[..bracketIndex];
        return fieldName;
    }

    private enum DummyKey
    {
    }
}