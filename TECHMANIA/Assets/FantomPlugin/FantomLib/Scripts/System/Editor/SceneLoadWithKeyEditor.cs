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
    [CustomEditor(typeof(SceneLoadWithKey))]
    public class SceneLoadWithKeyEditor : Editor {

        SerializedProperty sceneBuildIndex;
        GUIContent sceneBuildIndexLabel = new GUIContent("Scene Build Index");
        //SerializedProperty useName;
        //GUIContent useNameLabel = new GUIContent("Use Name");
        SerializedProperty sceneName;
        GUIContent sceneNameLabel = new GUIContent("Scene Name");
        SerializedProperty isAdditive;
        GUIContent isAdditiveLabel = new GUIContent("Is Additive");
        SerializedProperty enableKey;
        GUIContent enableKeyLabel = new GUIContent("Enable Key");
        SerializedProperty loadKey;
        GUIContent loadKeyLabel = new GUIContent("Load Key");
        SerializedProperty loadDelay;
        GUIContent loadDelayLabel = new GUIContent("Load Delay");

        SerializedProperty OnKeyPressed;
        SerializedProperty OnBeforeDelay;
        SerializedProperty OnBeforeLoad;

        private void OnEnable()
        {
            sceneBuildIndex = serializedObject.FindProperty("sceneBuildIndex");
            //useName = serializedObject.FindProperty("useName");
            sceneName = serializedObject.FindProperty("sceneName");
            isAdditive = serializedObject.FindProperty("isAdditive");
            enableKey = serializedObject.FindProperty("enableKey");
            loadKey = serializedObject.FindProperty("loadKey");
            loadDelay = serializedObject.FindProperty("loadDelay");
            OnKeyPressed = serializedObject.FindProperty("OnKeyPressed");
            OnBeforeDelay = serializedObject.FindProperty("OnBeforeDelay");
            OnBeforeLoad = serializedObject.FindProperty("OnBeforeLoad");
        }

        string[] sceneSpecification = { "Scene Build Index", "Scene Name" };

        public override void OnInspectorGUI()
        {
            var obj = target as SceneLoadWithKey;
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target) , typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();


            EditorGUI.BeginChangeCheck();
            int sceneSpecificationIndex = EditorGUILayout.Popup("Scene Specification", obj.useName ? 1 : 0, sceneSpecification);
            if (EditorGUI.EndChangeCheck())
            {
                if (0 <= sceneSpecificationIndex && sceneSpecificationIndex < sceneSpecification.Length)
                    obj.useName = (sceneSpecificationIndex == 1);
            }

            //EditorGUILayout.PropertyField(useName, useNameLabel, true);
            if (obj.useName)
                EditorGUILayout.PropertyField(sceneName, sceneNameLabel, true);
            else
                EditorGUILayout.PropertyField(sceneBuildIndex, sceneBuildIndexLabel, true);


            EditorGUILayout.PropertyField(isAdditive, isAdditiveLabel, true);

            EditorGUILayout.PropertyField(enableKey, enableKeyLabel, true);
            EditorGUILayout.PropertyField(loadKey, loadKeyLabel, true);
            EditorGUILayout.PropertyField(loadDelay, loadDelayLabel, true);

            EditorGUILayout.PropertyField(OnKeyPressed, true);
            EditorGUILayout.PropertyField(OnBeforeDelay, true);
            EditorGUILayout.PropertyField(OnBeforeLoad, true);

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
