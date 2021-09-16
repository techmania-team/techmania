using UnityEngine;
using UnityEditor;

namespace FantomLib
{
    [CustomEditor(typeof(CpuRateBar))]
    public class CpuRateBarEditor : Editor {

        SerializedProperty barType;
        GUIContent barTypeLabel = new GUIContent("Bar Type");
        SerializedProperty nameText;
        GUIContent nameTextLabel = new GUIContent("Name Text");
        SerializedProperty userBarImage;
        GUIContent userBarImageLabel = new GUIContent("User Bar Image");
        SerializedProperty niceBarImage;
        GUIContent niceBarImageLabel = new GUIContent("Nice Bar Image");
        SerializedProperty systemBarImage;
        GUIContent systemBarImageLabel = new GUIContent("System Bar Image");
        SerializedProperty idleBarImage;
        GUIContent idleBarImageLabel = new GUIContent("Idle Bar Image");
        SerializedProperty useGradColor;
        GUIContent useGradColorLabel = new GUIContent("Use Grad Color");

        private void OnEnable()
        {
            barType = serializedObject.FindProperty("barType");
            nameText = serializedObject.FindProperty("nameText");
            userBarImage = serializedObject.FindProperty("userBarImage");
            niceBarImage = serializedObject.FindProperty("niceBarImage");
            systemBarImage = serializedObject.FindProperty("systemBarImage");
            idleBarImage = serializedObject.FindProperty("idleBarImage");
            useGradColor = serializedObject.FindProperty("useGradColor");
        }

        public override void OnInspectorGUI()
        {
            var obj = target as CpuRateBar;
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target) , typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(barType, barTypeLabel, true);

            switch (obj.barType)
            {
                case CpuRateBar.BarType.Each:
                    break;

                case CpuRateBar.BarType.UseGrad:
                    EditorGUILayout.PropertyField(useGradColor, useGradColorLabel, true);
                    break;
            }

            EditorGUILayout.PropertyField(nameText, nameTextLabel, true);
            EditorGUILayout.PropertyField(userBarImage, userBarImageLabel, true);
            EditorGUILayout.PropertyField(niceBarImage, niceBarImageLabel, true);
            EditorGUILayout.PropertyField(systemBarImage, systemBarImageLabel, true);
            EditorGUILayout.PropertyField(idleBarImage, idleBarImageLabel, true);

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
