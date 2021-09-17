using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FantomLib
{
    [CustomEditor(typeof(MarketSearchController))]
    public class MarketSearchControllerEditor : Editor
    {
        SerializedProperty searchType;
        GUIContent searchTypeLabel = new GUIContent("Search Type");
        SerializedProperty packageName;
        GUIContent packageNameLabel = new GUIContent("Package Name");
        SerializedProperty keyword;
        GUIContent keywordLabel = new GUIContent("Keyword");

        private void OnEnable()
        {
            searchType = serializedObject.FindProperty("searchType");
            packageName = serializedObject.FindProperty("packageName");
            keyword = serializedObject.FindProperty("keyword");
        }

        public override void OnInspectorGUI()
        {
            var obj = target as MarketSearchController;
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target) , typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.PropertyField(searchType, searchTypeLabel, true);

            switch (obj.searchType)
            {
                case MarketSearchController.SearchType.PackageName:
                    EditorGUILayout.PropertyField(packageName, packageNameLabel, true);
                    break;

                case MarketSearchController.SearchType.Keyword:
                    EditorGUILayout.PropertyField(keyword, keywordLabel, true);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
