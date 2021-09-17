using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FantomLib
{
    [CustomEditor(typeof(StorageLoadTextController))]
    public class StorageLoadTextControllerEditor : Editor {

        SerializedProperty targetText;
        GUIContent targetTextLabel = new GUIContent("Target Text");
        SerializedProperty mimeTypes;
        GUIContent mimeTypesLabel = new GUIContent("Mime Types");

        SerializedProperty OnResult;
        SerializedProperty OnError;

        private void OnEnable()
        {
            targetText = serializedObject.FindProperty("targetText");
            mimeTypes = serializedObject.FindProperty("mimeTypes");
            OnResult = serializedObject.FindProperty("OnResult");
            OnError = serializedObject.FindProperty("OnError");
        }

        public override void OnInspectorGUI()
        {
            //var obj = target as SendTextController;
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target) , typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(targetText, targetTextLabel, true);
            EditorGUILayout.PropertyField(mimeTypes, mimeTypesLabel, true);

            EditorGUILayout.PropertyField(OnResult, true);
            EditorGUILayout.PropertyField(OnError, true);

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
