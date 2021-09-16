using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace FantomLib
{
    [CustomEditor(typeof(VibratorController))]
    public class VibratorControllerEditor : Editor
    {
        SerializedProperty vibratorType;
        GUIContent vibratorTypeLabel = new GUIContent("Vibrator Type");
        SerializedProperty pattern;
        GUIContent patternLabel = new GUIContent("Pattern");
        SerializedProperty duration;
        GUIContent durationLabel = new GUIContent("Duration");
        SerializedProperty OnError;

        private void OnEnable()
        {
            vibratorType = serializedObject.FindProperty("vibratorType");
            pattern = serializedObject.FindProperty("pattern");
            duration = serializedObject.FindProperty("duration");
            OnError = serializedObject.FindProperty("OnError");
        }

        public override void OnInspectorGUI()
        {
            var obj = target as VibratorController;
            serializedObject.Update();

            EditorGUILayout.PropertyField(vibratorType, vibratorTypeLabel, true);

            switch (obj.vibratorType)
            {
                case VibratorController.VibratorType.OneShot:
                    EditorGUILayout.PropertyField(duration, durationLabel, true);
                    break;
                case VibratorController.VibratorType.Pattern:
                case VibratorController.VibratorType.PatternLoop:
                    EditorGUILayout.PropertyField(pattern, patternLabel, true);
                    break;
            }

            EditorGUILayout.PropertyField(OnError, true);

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
