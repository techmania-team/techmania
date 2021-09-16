using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FantomLib
{
    [CustomEditor(typeof(SpeechRecognizerController))]
    public class SpeechRecognizerControllerEditor : Editor
    {
        SerializedProperty locale;
        GUIContent localeLabel = new GUIContent("Locale");
        SerializedProperty saveSetting;
        GUIContent saveSettingLabel = new GUIContent("Save Setting");
        SerializedProperty saveKey;
        GUIContent saveKeyLabel = new GUIContent("Save Key");

        SerializedProperty OnReady;
        SerializedProperty OnBegin;
        SerializedProperty OnResult;
        SerializedProperty OnError;

        private void OnEnable()
        {
            locale = serializedObject.FindProperty("locale");
            saveSetting = serializedObject.FindProperty("saveSetting");
            saveKey = serializedObject.FindProperty("saveKey");
            OnReady = serializedObject.FindProperty("OnReady");
            OnBegin = serializedObject.FindProperty("OnBegin");
            OnResult = serializedObject.FindProperty("OnResult");
            OnError = serializedObject.FindProperty("OnError");
        }

        int localeIndex = 0;

        public override void OnInspectorGUI()
        {
            //var obj = target as SpeechRecognizerController;
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

            EditorGUILayout.PropertyField(saveSetting, saveSettingLabel, true);
            EditorGUILayout.PropertyField(saveKey, saveKeyLabel, true);

            EditorGUILayout.PropertyField(OnReady, true);
            EditorGUILayout.PropertyField(OnBegin, true);
            EditorGUILayout.PropertyField(OnResult, true);
            EditorGUILayout.PropertyField(OnError, true);


            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
