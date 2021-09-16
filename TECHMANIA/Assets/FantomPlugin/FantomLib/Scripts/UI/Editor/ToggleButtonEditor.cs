using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEditor;

namespace FantomLib
{
    [CustomEditor(typeof(ToggleButton))]
    public class ToggleButtonEditor : ToggleObjectEditor {

        SerializedProperty targetButton;
        GUIContent targetButtonLabel = new GUIContent("Target Button");

        SerializedProperty onImage;
        GUIContent onImageLabel = new GUIContent("On Image");
        SerializedProperty offImage;
        GUIContent offImageLabel = new GUIContent("Off Image");

        SerializedProperty images;
        GUIContent imagesLabel = new GUIContent("Images");


        protected new void OnEnable()
        {
            base.OnEnable();

            targetButton = serializedObject.FindProperty("targetButton");
            onImage = serializedObject.FindProperty("onImage");
            offImage = serializedObject.FindProperty("offImage");
            images = serializedObject.FindProperty("images");
        }

        public override void OnInspectorGUI()
        {
            //var obj = target as ToggleObject;
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target) , typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(targetButton, targetButtonLabel, true);

            EditorGUILayout.PropertyField(toggleType, toggleTypeLabel, true);

            switch (toggleType.enumValueIndex)
            {
                case (int)ToggleObject.ToggleType.OnOff:
                    EditorGUILayout.PropertyField(isOn, isOnLabel, true);
                    EditorGUILayout.PropertyField(onObject, onObjectLabel, true);
                    EditorGUILayout.PropertyField(onImage, onImageLabel, true);
                    EditorGUILayout.PropertyField(offObject, offObjectLabel, true);
                    EditorGUILayout.PropertyField(offImage, offImageLabel, true);
                    EditorGUILayout.PropertyField(OnToggleChanged, true);
                    break;

                case (int)ToggleObject.ToggleType.Index:
                    EditorGUILayout.PropertyField(index, indexLabel, true);
                    EditorGUILayout.PropertyField(objects, objectsLabel, true);
                    EditorGUILayout.PropertyField(images, imagesLabel, true);
                    EditorGUILayout.PropertyField(OnToggleIndexChanged, true);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

    }
}
