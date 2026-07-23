using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(EnumDictionaryBase), true)]
public class EnumDictionaryDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var lineHeight = EditorGUIUtility.singleLineHeight;
        if (!property.isExpanded)
            // This will just be a foldout line.
            return lineHeight;
        // In this case, we will be showing a foldout and a list of key value pairs.
        // Rectify first so that we will have something that properly.
        EnumDictionaryEditorUtils.RectifyPropertyIfNeeded(fieldInfo, property);
        var ret = lineHeight;
        var pairsProperties = property.FindPropertyRelative(EnumDictionaryEditorUtils.pairsPath);
        var numPairs = pairsProperties.arraySize;
        for (var i = 0; i < numPairs; ++i)
            ret += EditorGUI.GetPropertyHeight(pairsProperties.GetArrayElementAtIndex(i)
                .FindPropertyRelative(EnumDictionaryEditorUtils.valuePath));

        return ret;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        label = EditorGUI.BeginProperty(position, label, property);

        var allKeys = EnumDictionaryEditorUtils.RectifyPropertyIfNeeded(fieldInfo, property);
        var indentedRect = EditorGUI.IndentedRect(position);
        var lineHeight = EditorGUIUtility.singleLineHeight;
        // For a reason why we need to reset indent level:
        // https://answers.unity.com/questions/1268850/how-to-properly-deal-with-editorguiindentlevel-in.html
        var oldIndentLevel = EditorGUI.indentLevel;
        var oldHierarchyMode = EditorGUIUtility.hierarchyMode;
        EditorGUI.BeginChangeCheck();
        EditorGUI.indentLevel = 0;
        EditorGUIUtility.hierarchyMode = true;
        var foldoutRect = new Rect(indentedRect.x, indentedRect.y, indentedRect.width, lineHeight);
        var shouldShowPairs = EditorGUI.Foldout(foldoutRect, property.isExpanded, label);
        EditorGUIUtility.hierarchyMode = oldHierarchyMode;
        EditorGUI.indentLevel = oldIndentLevel;
        if (EditorGUI.EndChangeCheck())
        {
            property.isExpanded = shouldShowPairs;
            EditorUtility.SetDirty(property.serializedObject.targetObject);
        }

        if (!shouldShowPairs) return;

        // Let's create all the pairs.
        // We will just use 1/3 of the width to be the key. The remaining for value.
        var y = indentedRect.y + lineHeight;
        var pairsProperty = property.FindPropertyRelative(EnumDictionaryEditorUtils.pairsPath);
        var numPairs = pairsProperty.arraySize;
        for (var i = 0; i < numPairs; ++i)
        {
            // The key is not editable, so we will just use a label.
            var pairProperty = pairsProperty.GetArrayElementAtIndex(i);
            var valueProperty = pairProperty.FindPropertyRelative(EnumDictionaryEditorUtils.valuePath);
            var height = EditorGUI.GetPropertyHeight(valueProperty);
            var valueRect = new Rect(indentedRect.x, y, indentedRect.width, height);
            EditorGUIUtility.hierarchyMode = false;
            EditorGUI.PropertyField(valueRect, valueProperty, new GUIContent($"{allKeys.GetValue(i)}"), true);
            EditorGUIUtility.hierarchyMode = oldHierarchyMode;
            y += height;
        }

        EditorGUI.EndProperty();
    }
}