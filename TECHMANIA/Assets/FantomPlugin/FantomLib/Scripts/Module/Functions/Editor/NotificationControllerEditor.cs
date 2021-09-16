using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FantomLib
{
    [CustomEditor(typeof(NotificationController))]
    public class NotificationControllerEditor : Editor
    {
        SerializedProperty title;
        GUIContent titleLabel = new GUIContent("Title");
        SerializedProperty message;
        GUIContent messageLabel = new GUIContent("Message");
        SerializedProperty tapAction;
        GUIContent tapActionLabel = new GUIContent("Tap Action");
        SerializedProperty url;
        GUIContent urlLabel = new GUIContent("URL");
        SerializedProperty iconName;
        GUIContent iconNameLabel = new GUIContent("Icon Name");
        SerializedProperty idTag;
        GUIContent idTagLabel = new GUIContent("ID Tag");
        SerializedProperty showTimestamp;
        GUIContent showTimestampLabel = new GUIContent("Show Timestamp");
        SerializedProperty vibratorType;
        GUIContent vibratorTypeLabel = new GUIContent("Vibrator Type");
        SerializedProperty vibratorPattern;
        GUIContent vibratorPatternLabel = new GUIContent("Vibrator Pattern");
        SerializedProperty vibratorDuration;
        GUIContent vibratorDurationLabel = new GUIContent("Vibrator Duration");
        SerializedProperty localize;
        GUIContent localizeLabel = new GUIContent("Localize");

        private void OnEnable()
        {
            title = serializedObject.FindProperty("title");
            message = serializedObject.FindProperty("message");
            tapAction = serializedObject.FindProperty("tapAction");
            url = serializedObject.FindProperty("url");
            iconName = serializedObject.FindProperty("iconName");
            idTag = serializedObject.FindProperty("idTag");
            showTimestamp = serializedObject.FindProperty("showTimestamp");
            vibratorType = serializedObject.FindProperty("vibratorType");
            vibratorPattern = serializedObject.FindProperty("vibratorPattern");
            vibratorDuration = serializedObject.FindProperty("vibratorDuration");
            localize = serializedObject.FindProperty("localize");
        }

        public override void OnInspectorGUI()
        {
            var obj = target as NotificationController;
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();

            //obj.title = EditorGUILayout.TextField("Title", obj.title);
            EditorGUILayout.PropertyField(title, titleLabel, true);

            //obj.message = EditorGUILayout.TextField("Message", obj.message);
            EditorGUILayout.PropertyField(message, messageLabel, true);

            //obj.tapAction = (NotificationController.TapAction)EditorGUILayout.EnumPopup("Tap Action", obj.tapAction);
            EditorGUILayout.PropertyField(tapAction, tapActionLabel, true);

            switch (obj.tapAction)
            {
                case NotificationController.TapAction.BackToApplication:
                    break;
                case NotificationController.TapAction.OpenURL:
                    //obj.url = EditorGUILayout.TextField("URL", obj.url);
                    EditorGUILayout.PropertyField(url, urlLabel, true);
                    break;
            }

            //obj.iconName = EditorGUILayout.TextField("Icon Name", obj.iconName);
            EditorGUILayout.PropertyField(iconName, iconNameLabel, true);

            //obj.idTag = EditorGUILayout.TextField("ID Tag", obj.idTag);
            EditorGUILayout.PropertyField(idTag, idTagLabel, true);

            //obj.showTimestamp = EditorGUILayout.Toggle("Show Timestamp", obj.showTimestamp);
            EditorGUILayout.PropertyField(showTimestamp, showTimestampLabel, true);

            //obj.vibratorType = (NotificationController.VibratorType)EditorGUILayout.EnumPopup("Vibrator Type", obj.vibratorType);
            EditorGUILayout.PropertyField(vibratorType, vibratorTypeLabel, true);

            switch (obj.vibratorType)
            {
                case NotificationController.VibratorType.OneShot:
                    EditorGUILayout.PropertyField(vibratorDuration, vibratorDurationLabel, true);
                    break;
                case NotificationController.VibratorType.Pattern:
                    EditorGUILayout.PropertyField(vibratorPattern, vibratorPatternLabel, true);
                    break;
            }

            EditorGUILayout.PropertyField(localize, localizeLabel, true);

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}

