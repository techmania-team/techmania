using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FantomLib
{
    [CustomEditor(typeof(AndroidActionController))]
    public class AndroidActionControllerEditor : Editor
    {
        SerializedProperty action;
        GUIContent actionLabel = new GUIContent("Action");
        SerializedProperty actionType;
        GUIContent actionTypeLabel = new GUIContent("Action Type");
        SerializedProperty title;
        GUIContent titleLabel = new GUIContent("Title");
        SerializedProperty uri;
        GUIContent uriLabel = new GUIContent("URI");
        SerializedProperty extra;
        GUIContent extraLabel = new GUIContent("Extra");
        SerializedProperty query;
        GUIContent queryLabel = new GUIContent("Query");
        SerializedProperty mimetype;
        GUIContent mimetypeLabel = new GUIContent("MIME Type");

        SerializedProperty addExtras;
        GUIContent addExtrasLabel = new GUIContent("Add Extras");

        private void OnEnable()
        {
            action = serializedObject.FindProperty("action");
            actionType = serializedObject.FindProperty("actionType");
            title = serializedObject.FindProperty("title");
            uri = serializedObject.FindProperty("uri");
            extra = serializedObject.FindProperty("extra");
            query = serializedObject.FindProperty("query");
            mimetype = serializedObject.FindProperty("mimetype");
            addExtras = serializedObject.FindProperty("addExtras");
        }

        int actionStringIndex = 0;

        public override void OnInspectorGUI()
        {
            var obj = target as AndroidActionController;
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target) , typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();


            //'Action' input support
            EditorGUI.BeginChangeCheck();
            actionStringIndex = EditorGUILayout.Popup("(Action Input Support)", actionStringIndex, ActionString.ConstantValues);
            if (EditorGUI.EndChangeCheck())
            {
                if (0 < actionStringIndex && actionStringIndex < ActionString.ConstantValues.Length)
                    obj.action = ActionString.ConstantValues[actionStringIndex];
            }

            EditorGUILayout.PropertyField(action, actionLabel, true);
            EditorGUILayout.PropertyField(actionType, actionTypeLabel, true);


            switch (obj.actionType)
            {
                case AndroidActionController.ActionType.ActionOnly:
                    EditorGUILayout.PropertyField(addExtras, addExtrasLabel, true);
                    break;

                case AndroidActionController.ActionType.URI:
                    EditorGUILayout.PropertyField(uri, uriLabel, true);
                    EditorGUILayout.PropertyField(addExtras, addExtrasLabel, true);
                    break;

                case AndroidActionController.ActionType.ExtraQuery:
                    EditorGUILayout.PropertyField(extra, extraLabel, true);
                    EditorGUILayout.PropertyField(query, queryLabel, true);
                    EditorGUILayout.PropertyField(addExtras, addExtrasLabel, true);
                    break;

                case AndroidActionController.ActionType.Chooser:
                    EditorGUILayout.PropertyField(extra, extraLabel, true);
                    EditorGUILayout.PropertyField(query, queryLabel, true);
                    EditorGUILayout.PropertyField(mimetype, mimetypeLabel, true);
                    EditorGUILayout.PropertyField(title, titleLabel, true);
                    EditorGUILayout.PropertyField(addExtras, addExtrasLabel, true);
                    break;

                case AndroidActionController.ActionType.UriWithMimeType:
                    EditorGUILayout.PropertyField(uri, uriLabel, true);
                    EditorGUILayout.PropertyField(mimetype, mimetypeLabel, true);
                    break;
            }


            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
