using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FantomLib
{
    [CustomEditor(typeof(GalleryPickController))]
    public class GalleryPickControllerEditor : Editor
    {
        SerializedProperty pickType;
        GUIContent pickTypeLabel = new GUIContent("Pick Type");

        SerializedProperty OnResult;
        SerializedProperty OnResultInfo;
        SerializedProperty OnResultVideoInfo;
        SerializedProperty OnError;

        private void OnEnable()
        {
            pickType = serializedObject.FindProperty("pickType");

            OnResult = serializedObject.FindProperty("OnResult");
            OnResultInfo = serializedObject.FindProperty("OnResultInfo");
            OnResultVideoInfo = serializedObject.FindProperty("OnResultVideoInfo");
            OnError = serializedObject.FindProperty("OnError");
        }

        public override void OnInspectorGUI()
        {
            var obj = target as GalleryPickController;
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target) , typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(pickType, pickTypeLabel, true);

            switch (obj.pickType)
            {
                case GalleryPickController.PickType.Image:
                    EditorGUILayout.PropertyField(OnResult, true);
                    EditorGUILayout.PropertyField(OnResultInfo, true);
                    EditorGUILayout.PropertyField(OnError, true);
                    break;

                case GalleryPickController.PickType.Video:
                    EditorGUILayout.PropertyField(OnResult, true);
                    EditorGUILayout.PropertyField(OnResultVideoInfo, true);
                    EditorGUILayout.PropertyField(OnError, true);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
