using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FantomLib
{
    [CustomEditor(typeof(SendTextController))]
    public class SendTextControllerEditor : Editor
    {
        SerializedProperty targetText;
        GUIContent targetTextLabel = new GUIContent("Target Text");
        SerializedProperty selectionType;
        GUIContent selectionTypeLabel = new GUIContent("Selection Type");
        SerializedProperty chooserTitle;
        GUIContent chooserTitleLabel = new GUIContent("Chooser Title");
        SerializedProperty localize;
        GUIContent localizeLabel = new GUIContent("Localize");

        private void OnEnable()
        {
            targetText = serializedObject.FindProperty("targetText");
            selectionType = serializedObject.FindProperty("selectionType");
            chooserTitle = serializedObject.FindProperty("chooserTitle");
            localize = serializedObject.FindProperty("localize");
        }

        public override void OnInspectorGUI()
        {
            var obj = target as SendTextController;
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target) , typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(targetText, targetTextLabel, true);

            EditorGUILayout.PropertyField(selectionType, selectionTypeLabel, true);
            switch (obj.selectionType)
            {
                case SendTextController.SelectionType.Implicit:
                    //
                    break;
                case SendTextController.SelectionType.Chooser:
                    EditorGUILayout.PropertyField(chooserTitle, chooserTitleLabel, true);
                    EditorGUILayout.PropertyField(localize, localizeLabel, true);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
