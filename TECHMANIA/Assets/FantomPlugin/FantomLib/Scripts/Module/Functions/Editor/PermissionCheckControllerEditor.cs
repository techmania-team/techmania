using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FantomLib
{
    [CustomEditor(typeof(PermissionCheckController))]
    public class PermissionCheckControllerEditor : Editor {

        SerializedProperty permission;
        GUIContent permissionLabel = new GUIContent("Permission");
        SerializedProperty checkOnStart;
        GUIContent checkOnStartLabel = new GUIContent("Check On Start");
        SerializedProperty requestWhenNotGranted;
        GUIContent requestWhenNotGrantedLabel = new GUIContent("Request When Not Granted");

        SerializedProperty title;
        GUIContent titleLabel = new GUIContent("Title");
        SerializedProperty message;
        GUIContent messageLabel = new GUIContent("Message");
        SerializedProperty style;
        GUIContent styleLabel = new GUIContent("Style");
        SerializedProperty localize;
        GUIContent localizeLabel = new GUIContent("Localize");

        SerializedProperty OnResult;
        SerializedProperty OnGranted;
        SerializedProperty OnDenied;
        SerializedProperty OnAllowed;

        private void OnEnable()
        {
            permission = serializedObject.FindProperty("permission");
            checkOnStart = serializedObject.FindProperty("checkOnStart");
            requestWhenNotGranted = serializedObject.FindProperty("requestWhenNotGranted");
            title = serializedObject.FindProperty("title");
            message = serializedObject.FindProperty("message");
            style = serializedObject.FindProperty("style");
            localize = serializedObject.FindProperty("localize");
            OnResult = serializedObject.FindProperty("OnResult");
            OnGranted = serializedObject.FindProperty("OnGranted");
            OnDenied = serializedObject.FindProperty("OnDenied");
            OnAllowed = serializedObject.FindProperty("OnAllowed");
        }

        public override void OnInspectorGUI()
        {
            var obj = target as PermissionCheckController;
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target) , typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(permission, permissionLabel, true);
            EditorGUILayout.PropertyField(checkOnStart, checkOnStartLabel, true);
            EditorGUILayout.PropertyField(requestWhenNotGranted, requestWhenNotGrantedLabel, true);

            if (obj.requestWhenNotGranted)
            {
                EditorGUILayout.PropertyField(title, titleLabel, true);
                EditorGUILayout.PropertyField(message, messageLabel, true);
                EditorGUILayout.PropertyField(style, styleLabel, true);
                EditorGUILayout.PropertyField(localize, localizeLabel, true);
            }

            EditorGUILayout.PropertyField(OnResult, true);
            EditorGUILayout.PropertyField(OnGranted, true);
            EditorGUILayout.PropertyField(OnDenied, true);
            
            if (obj.requestWhenNotGranted)
            {
                EditorGUILayout.PropertyField(OnAllowed, true);
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
