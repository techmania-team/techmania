using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FantomLib
{
    [CustomEditor(typeof(SelectDialogController))]
    public class SelectDialogControllerEditor : Editor
    {
        SerializedProperty title;
        GUIContent titleLabel = new GUIContent("Title");
        SerializedProperty items;
        GUIContent itemsLabel = new GUIContent("Items");
        SerializedProperty resultType;
        GUIContent resultTypeLabel = new GUIContent("Result Type");
        SerializedProperty style;
        GUIContent styleLabel = new GUIContent("Style");
        SerializedProperty localize;
        GUIContent localizeLabel = new GUIContent("Localize");

        SerializedProperty OnResult;
        SerializedProperty OnResultIndex;

        private void OnEnable()
        {
            title = serializedObject.FindProperty("title");
            items = serializedObject.FindProperty("items");
            resultType = serializedObject.FindProperty("resultType");
            style = serializedObject.FindProperty("style");
            localize = serializedObject.FindProperty("localize");
            OnResult = serializedObject.FindProperty("OnResult");
            OnResultIndex = serializedObject.FindProperty("OnResultIndex");
        }

        public override void OnInspectorGUI()
        {
            var obj = target as SelectDialogController;
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target) , typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();

            //obj.title = EditorGUILayout.TextField("Title", obj.title);
            EditorGUILayout.PropertyField(title, titleLabel, true);

            EditorGUILayout.PropertyField(items, itemsLabel, true);

            //obj.resultType = (SelectDialogController.ResultType)EditorGUILayout.EnumPopup("Result Type", obj.resultType);
            EditorGUILayout.PropertyField(resultType, resultTypeLabel, true);

            //obj.style = EditorGUILayout.TextField("Style", obj.style);
            EditorGUILayout.PropertyField(style, styleLabel, true);

            EditorGUILayout.PropertyField(localize, localizeLabel, true);

            switch (obj.resultType)
            {
                case SelectDialogController.ResultType.Index:
                    EditorGUILayout.PropertyField(OnResultIndex, true);
                    break;
                case SelectDialogController.ResultType.Value:
                case SelectDialogController.ResultType.Text:
                    EditorGUILayout.PropertyField(OnResult, true);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
