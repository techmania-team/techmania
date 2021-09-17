using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FantomLib
{
    [CustomEditor(typeof(LocalizeText))]
    public class LocalizeTextEditor : Editor
    {
        SerializedProperty targetText;
        GUIContent targetTextLabel = new GUIContent("Target Text");
        SerializedProperty resourceType;
        GUIContent resourceTypeLabel = new GUIContent("Resource Type");
        SerializedProperty localize;
        SerializedProperty localizeData;
        GUIContent localizeLabel = new GUIContent("Localize");

        private void OnEnable()
        {
            targetText = serializedObject.FindProperty("targetText");
            resourceType = serializedObject.FindProperty("resourceType");
            localize = serializedObject.FindProperty("localize");
            localizeData = serializedObject.FindProperty("localizeData");
        }

        public override void OnInspectorGUI()
        {
            var obj = target as LocalizeText;
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target) , typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(targetText, targetTextLabel, true);

            EditorGUILayout.PropertyField(resourceType, resourceTypeLabel, true);
            switch (obj.resourceType)
            {
                case LocalizeText.ResourceType.Local:
                    EditorGUILayout.PropertyField(localize, localizeLabel, true);
                    break;
                case LocalizeText.ResourceType.Resource:
                    EditorGUILayout.PropertyField(localizeData, localizeLabel, true);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
