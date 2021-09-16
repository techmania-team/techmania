using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using IDValidStatus = FantomLib.LocalizeStringResource.IDValidStatus;

namespace FantomLib
{
    [CustomEditor(typeof(LocalizeStringResource))]
    public class LocalizeStringResourceEditor : Editor
    {
        SerializedProperty items;
        GUIContent itemsLabel = new GUIContent("Items");

        int insertIndex = 0;
        int removeIndex = -1;
        string searchID = "";

        //Check for ID errors
        IDValidStatus idValidStatus = new IDValidStatus();
        string emptyIndexError = "";
        string duplicateIDError = "";

        private void OnEnable()
        {
            items = serializedObject.FindProperty("items");
        }

        public override void OnInspectorGUI()
        {
            var obj = target as LocalizeStringResource;
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target) , typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();

            bool edited = false;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(items, itemsLabel, true);
            edited |= EditorGUI.EndChangeCheck();

            GUILayout.Space(15);

            if (items.arraySize > 0) {
                if (!string.IsNullOrEmpty(emptyIndexError))
                    EditorGUILayout.HelpBox("There is empty ID (index) : " + emptyIndexError, MessageType.Error);
                if (!string.IsNullOrEmpty(duplicateIDError))
                    EditorGUILayout.HelpBox("There is duplicate ID : " + duplicateIDError, MessageType.Error);
            }

            if (!Application.isPlaying)
            {
                if (obj.EditExecuting)
                    return;

                GUILayout.Space(15);
                //GUILayout.Box("Editor Tools", GUILayout.ExpandWidth(true), GUILayout.Height(20));

                insertIndex = EditorGUILayout.IntField("Insert Index", insertIndex);
                if (GUILayout.Button("Insert New Item"))
                {
                    if (obj.IsValidIndex(insertIndex))
                        Undo.RecordObject(target, "Insert New LocalizeString Item");
                    edited |= obj.InsetItem(insertIndex);
                }

                removeIndex = EditorGUILayout.IntField("Remove Index", removeIndex);
                if (GUILayout.Button("Remove Item"))
                {
                    if (obj.IsValidIndex(removeIndex))
                        Undo.RecordObject(target, "Remove LocalizeString Item");
                    edited |= obj.RemoveItem(removeIndex);
                }

                EditorGUI.BeginChangeCheck();
                searchID = EditorGUILayout.TextField("Search ID", searchID);
                if (EditorGUI.EndChangeCheck())
                {
                    if (!string.IsNullOrEmpty(searchID))
                        insertIndex = removeIndex = obj.FindIndex(searchID, true);
                    else
                        insertIndex = removeIndex = -1;
                }

                if (GUILayout.Button("Get Index from ID  (Whole match)"))
                {
                    insertIndex = removeIndex = obj.FindIndex(searchID);
                }
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);

            //Check for ID errors
            if (edited)
                CheckValidity();
        }

        private void CheckValidity()
        {
            var obj = target as LocalizeStringResource;
            obj.CheckIDValidity(ref idValidStatus);
            emptyIndexError = idValidStatus.GetEmptyError();
            duplicateIDError = idValidStatus.GetDuplicateError();
        }
    }
}
