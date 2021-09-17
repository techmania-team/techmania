using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FantomLib
{
    [CustomEditor(typeof(TextToSpeechController))]
    public class TextToSpeechControllerEditor : Editor
    {
        SerializedProperty locale;
        GUIContent localeLabel = new GUIContent("Locale");

        SerializedProperty speed;
        GUIContent speedLabel = new GUIContent("Speed");
        SerializedProperty speedStep;
        GUIContent speedStepLabel = new GUIContent("Speed Step");
        SerializedProperty pitch;
        GUIContent pitchLabel = new GUIContent("Pitch");
        SerializedProperty pitchStep;
        GUIContent pitchStepLabel = new GUIContent("Pitch Step");
        SerializedProperty saveSetting;
        GUIContent saveSettingLabel = new GUIContent("Save Setting");
        SerializedProperty saveKey;
        GUIContent saveKeyLabel = new GUIContent("Save Key");

        SerializedProperty OnStart;
        SerializedProperty OnDone;
        SerializedProperty OnStop;
        SerializedProperty OnStatus;
        SerializedProperty OnSpeedChanged;
        SerializedProperty OnPitchChanged;

        private void OnEnable()
        {
            locale = serializedObject.FindProperty("locale");
            speed = serializedObject.FindProperty("speed");
            speedStep = serializedObject.FindProperty("speedStep");
            pitch = serializedObject.FindProperty("pitch");
            pitchStep = serializedObject.FindProperty("pitchStep");
            saveSetting = serializedObject.FindProperty("saveSetting");
            saveKey = serializedObject.FindProperty("saveKey");
            OnStart = serializedObject.FindProperty("OnStart");
            OnDone = serializedObject.FindProperty("OnDone");
            OnStop = serializedObject.FindProperty("OnStop");
            OnStatus = serializedObject.FindProperty("OnStatus");
            OnSpeedChanged = serializedObject.FindProperty("OnSpeedChanged");
            OnPitchChanged = serializedObject.FindProperty("OnPitchChanged");
        }

        int localeIndex = 0;

        public override void OnInspectorGUI()
        {
            //var obj = target as TextToSpeechController;
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

            EditorGUILayout.PropertyField(speed, speedLabel, true);
            EditorGUILayout.PropertyField(speedStep, speedStepLabel, true);
            EditorGUILayout.PropertyField(pitch, pitchLabel, true);
            EditorGUILayout.PropertyField(pitchStep, pitchStepLabel, true);
            EditorGUILayout.PropertyField(saveSetting, saveSettingLabel, true);
            EditorGUILayout.PropertyField(saveKey, saveKeyLabel, true);

            EditorGUILayout.PropertyField(OnStart, true);
            EditorGUILayout.PropertyField(OnDone, true);
            EditorGUILayout.PropertyField(OnStop, true);
            EditorGUILayout.PropertyField(OnStatus, true);
            EditorGUILayout.PropertyField(OnSpeedChanged, true);
            EditorGUILayout.PropertyField(OnPitchChanged, true);


            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}

