using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FantomLib
{
    [CustomEditor(typeof(SpeechRecognizerDialogController))]
    public class SpeechRecognizerDialogControllerEditor : Editor
    {
        SerializedProperty locale;
        GUIContent localeLabel = new GUIContent("Locale");
        SerializedProperty message;
        GUIContent messageLabel = new GUIContent("Message");
        SerializedProperty saveSetting;
        GUIContent saveSettingLabel = new GUIContent("Save Setting");
        SerializedProperty saveKey;
        GUIContent saveKeyLabel = new GUIContent("Save Key");
        SerializedProperty localize;
        GUIContent localizeLabel = new GUIContent("Localize");

        SerializedProperty OnResult;
        SerializedProperty OnError;

        private void OnEnable()
        {
            locale = serializedObject.FindProperty("locale");
            message = serializedObject.FindProperty("message");
            saveSetting = serializedObject.FindProperty("saveSetting");
            saveKey = serializedObject.FindProperty("saveKey");
            localize = serializedObject.FindProperty("localize");
            OnResult = serializedObject.FindProperty("OnResult");
            OnError = serializedObject.FindProperty("OnError");
        }

        int localeIndex = 0;

        public override void OnInspectorGUI()
        {
            //var obj = target as SpeechRecognizerDialogController;
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target) , typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();


            //'Locale' input support
            EditorGUI.BeginChangeCheck();
            localeIndex = EditorGUILayout.Popup("(Locale Input Support)", localeIndex, AndroidLocale.ConstantValues);
            if (EditorGUI.EndChangeCheck())
            {
                if (0 < localeIndex && localeIndex < AndroidLocale.ConstantValues.Length)
                    locale.stringValue = AndroidLocale.ConstantValues[localeIndex];
            }
            EditorGUILayout.PropertyField(locale, localeLabel, true);

            EditorGUILayout.PropertyField(message, messageLabel, true);

            EditorGUILayout.PropertyField(saveSetting, saveSettingLabel, true);
            EditorGUILayout.PropertyField(saveKey, saveKeyLabel, true);

            EditorGUILayout.PropertyField(localize, localizeLabel, true);

            EditorGUILayout.PropertyField(OnResult, true);
            EditorGUILayout.PropertyField(OnError, true);


            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
