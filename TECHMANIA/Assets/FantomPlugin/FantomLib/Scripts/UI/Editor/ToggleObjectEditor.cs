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
    [CustomEditor(typeof(ToggleObject))]
    public class ToggleObjectEditor : Editor {

        protected SerializedProperty toggleType;
        protected GUIContent toggleTypeLabel = new GUIContent("Toggle Type");
        protected SerializedProperty isOn;
        protected GUIContent isOnLabel = new GUIContent("Is On");
        protected SerializedProperty onObject;
        protected GUIContent onObjectLabel = new GUIContent("On Object");
        protected SerializedProperty offObject;
        protected GUIContent offObjectLabel = new GUIContent("Off Object");

        protected SerializedProperty index;
        protected GUIContent indexLabel = new GUIContent("Index");
        protected SerializedProperty objects;
        protected GUIContent objectsLabel = new GUIContent("Objects");

        protected SerializedProperty OnToggleChanged;
        protected SerializedProperty OnToggleIndexChanged;


        protected void OnEnable()
        {
            toggleType = serializedObject.FindProperty("toggleType");
            isOn = serializedObject.FindProperty("isOn");
            onObject = serializedObject.FindProperty("onObject");
            offObject = serializedObject.FindProperty("offObject");

            index = serializedObject.FindProperty("index");
            objects = serializedObject.FindProperty("objects");

            OnToggleChanged = serializedObject.FindProperty("OnToggleChanged");
            OnToggleIndexChanged = serializedObject.FindProperty("OnToggleIndexChanged");
        }

        public override void OnInspectorGUI()
        {
            //var obj = target as ToggleObject;
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target) , typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(toggleType, toggleTypeLabel, true);

            switch (toggleType.enumValueIndex)
            {
                case (int)ToggleObject.ToggleType.OnOff:
                    EditorGUILayout.PropertyField(isOn, isOnLabel, true);
                    EditorGUILayout.PropertyField(onObject, onObjectLabel, true);
                    EditorGUILayout.PropertyField(offObject, offObjectLabel, true);
                    EditorGUILayout.PropertyField(OnToggleChanged, true);
                    break;

                case (int)ToggleObject.ToggleType.Index:
                    EditorGUILayout.PropertyField(index, indexLabel, true);
                    EditorGUILayout.PropertyField(objects, objectsLabel, true);
                    EditorGUILayout.PropertyField(OnToggleIndexChanged, true);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

    }
}
