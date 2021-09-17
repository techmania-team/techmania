using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FantomLib
{
    [CustomEditor(typeof(DialogItemParameter))]
    public class DialogItemParameterEditor : Editor
    {
        SerializedProperty type;
        GUIContent typeLabel = new GUIContent("Item Type");
        SerializedProperty lineHeight;
        GUIContent lineHeightLabel = new GUIContent("Line Height");
        SerializedProperty lineColor;
        GUIContent lineColorLabel = new GUIContent("Line Color");
        SerializedProperty text;
        GUIContent textLabel = new GUIContent("Text");
        SerializedProperty textColor;
        GUIContent textColorLabel = new GUIContent("Text Color");
        SerializedProperty backgroundColor;
        GUIContent backgroundColorLabel = new GUIContent("Background Color");
        SerializedProperty align;
        GUIContent alignLabel = new GUIContent("Align");
        SerializedProperty key;
        GUIContent keyLabel = new GUIContent("Key");
        SerializedProperty defaultChecked;
        GUIContent defaultCheckedLabel = new GUIContent("isOn");
        SerializedProperty value;
        GUIContent valueLabel = new GUIContent("Value");
        SerializedProperty minValue;
        GUIContent minValueLabel = new GUIContent("Min Value");
        SerializedProperty maxValue;
        GUIContent maxValueLabel = new GUIContent("Max Value");
        SerializedProperty digit;
        GUIContent digitLabel = new GUIContent("Digit");
        SerializedProperty toggleItems;
        GUIContent toggleItemsLabel = new GUIContent("Toggle Items");
        SerializedProperty checkedIndex;
        GUIContent checkedIndexLabel = new GUIContent("Selected Index");

        SerializedProperty localize;
        SerializedProperty localizeItems;
        GUIContent localizeLabel = new GUIContent("Localize");

        private void OnEnable()
        {
            type = serializedObject.FindProperty("type");
            lineHeight = serializedObject.FindProperty("lineHeight");
            lineColor = serializedObject.FindProperty("lineColor");
            text = serializedObject.FindProperty("text");
            textColor = serializedObject.FindProperty("textColor");
            backgroundColor = serializedObject.FindProperty("backgroundColor");
            align = serializedObject.FindProperty("align");
            key = serializedObject.FindProperty("key");
            defaultChecked = serializedObject.FindProperty("defaultChecked");
            value = serializedObject.FindProperty("value");
            minValue = serializedObject.FindProperty("minValue");
            maxValue = serializedObject.FindProperty("maxValue");
            digit = serializedObject.FindProperty("digit");
            toggleItems = serializedObject.FindProperty("toggleItems");
            checkedIndex = serializedObject.FindProperty("checkedIndex");

            localize = serializedObject.FindProperty("localize");
            localizeItems = serializedObject.FindProperty("localizeItems");
        }

        public override void OnInspectorGUI()
        {
            var obj = target as DialogItemParameter;
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(type, typeLabel, true);

            switch (obj.type)
            {
                case DialogItemType.Divisor:
                    EditorGUILayout.PropertyField(lineHeight, lineHeightLabel, true);
                    EditorGUILayout.PropertyField(lineColor, lineColorLabel, true);
                    break;

                case DialogItemType.Text:
                    EditorGUILayout.PropertyField(text, textLabel, true);
                    EditorGUILayout.PropertyField(textColor, textColorLabel, true);
                    EditorGUILayout.PropertyField(backgroundColor, backgroundColorLabel, true);
                    EditorGUILayout.PropertyField(align, alignLabel, true);

                    EditorGUILayout.PropertyField(localize, localizeLabel, true);
                    break;

                case DialogItemType.Switch:
                    EditorGUILayout.PropertyField(key, keyLabel, true);
                    EditorGUILayout.PropertyField(text, textLabel, true);
                    EditorGUILayout.PropertyField(defaultChecked, defaultCheckedLabel, true);
                    EditorGUILayout.PropertyField(textColor, textColorLabel, true);

                    EditorGUILayout.PropertyField(localize, localizeLabel, true);
                    break;

                case DialogItemType.Check:
                    EditorGUILayout.PropertyField(key, keyLabel, true);
                    EditorGUILayout.PropertyField(text, textLabel, true);
                    EditorGUILayout.PropertyField(defaultChecked, defaultCheckedLabel, true);
                    EditorGUILayout.PropertyField(textColor, textColorLabel, true);

                    EditorGUILayout.PropertyField(localize, localizeLabel, true);
                    break;

                case DialogItemType.Slider:
                    EditorGUILayout.PropertyField(key, keyLabel, true);
                    EditorGUILayout.PropertyField(text, textLabel, true);

                    EditorGUILayout.PropertyField(value, valueLabel, true);
                    EditorGUILayout.PropertyField(minValue, minValueLabel, true);
                    EditorGUILayout.PropertyField(maxValue, maxValueLabel, true);
                    EditorGUILayout.PropertyField(digit, digitLabel, true);
                    EditorGUILayout.PropertyField(textColor, textColorLabel, true);

                    EditorGUILayout.PropertyField(localize, localizeLabel, true);
                    break;

                case DialogItemType.Toggle:
                    EditorGUILayout.PropertyField(key, keyLabel, true);
                    EditorGUILayout.PropertyField(toggleItems, toggleItemsLabel, true);
                    EditorGUILayout.PropertyField(checkedIndex, checkedIndexLabel, true);
                    EditorGUILayout.PropertyField(textColor, textColorLabel, true);

                    EditorGUILayout.PropertyField(localizeItems, localizeLabel, true);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
