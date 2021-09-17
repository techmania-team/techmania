using UnityEngine;
using UnityEditor;

namespace FantomLib
{
    [CustomEditor(typeof(CpuRateBarsView))]
    public class CpuRateBarsViewEditor : Editor {

        SerializedProperty applySettingOnAwake;
        GUIContent applySettingOnAwakeLabel = new GUIContent("Apply Setting On Awake");
        SerializedProperty barType;
        GUIContent barTypeLabel = new GUIContent("Bar Type");
        SerializedProperty userColor;
        GUIContent userBarImageLabel = new GUIContent("User Bar Color");
        SerializedProperty niceColor;
        GUIContent niceBarImageLabel = new GUIContent("Nice Bar Color");
        SerializedProperty systemColor;
        GUIContent systemBarImageLabel = new GUIContent("System Bar Color");
        SerializedProperty idleColor;
        GUIContent idleBarImageLabel = new GUIContent("Idle Bar Color");
        SerializedProperty useGradColor;
        GUIContent useGradColorLabel = new GUIContent("Use Grad Color");
        SerializedProperty cpuRateBars;
        GUIContent cpuRateBarsLabel = new GUIContent("Cpu Rate Bars");

        private void OnEnable()
        {
            applySettingOnAwake = serializedObject.FindProperty("applySettingOnAwake");
            barType = serializedObject.FindProperty("barType");
            userColor = serializedObject.FindProperty("userColor");
            niceColor = serializedObject.FindProperty("niceColor");
            systemColor = serializedObject.FindProperty("systemColor");
            idleColor = serializedObject.FindProperty("idleColor");
            useGradColor = serializedObject.FindProperty("useGradColor");
            cpuRateBars = serializedObject.FindProperty("cpuRateBars");
        }

        public override void OnInspectorGUI()
        {
            var obj = target as CpuRateBarsView;
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target) , typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(applySettingOnAwake, applySettingOnAwakeLabel, true);

            EditorGUILayout.PropertyField(barType, barTypeLabel, true);

            switch (obj.barType)
            {
                case CpuRateBar.BarType.Each:
                    EditorGUILayout.PropertyField(userColor, userBarImageLabel, true);
                    EditorGUILayout.PropertyField(niceColor, niceBarImageLabel, true);
                    EditorGUILayout.PropertyField(systemColor, systemBarImageLabel, true);
                    EditorGUILayout.PropertyField(idleColor, idleBarImageLabel, true);
                    break;

                case CpuRateBar.BarType.UseGrad:
                    EditorGUILayout.PropertyField(useGradColor, useGradColorLabel, true);
                    EditorGUILayout.PropertyField(idleColor, idleBarImageLabel, true);
                    break;
            }

            EditorGUILayout.PropertyField(cpuRateBars, cpuRateBarsLabel, true);

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
