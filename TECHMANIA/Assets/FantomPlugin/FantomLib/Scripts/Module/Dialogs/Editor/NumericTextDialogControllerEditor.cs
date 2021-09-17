using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FantomLib
{
    [CustomEditor(typeof(NumericTextDialogController))]
    public class NumericTextDialogControllerEditor : Editor
    {
        SerializedProperty title;
        GUIContent titleLabel = new GUIContent("Title");
        SerializedProperty message;
        GUIContent messageLabel = new GUIContent("Message");
        SerializedProperty defaultValue;
        GUIContent defaultValueLabel = new GUIContent("Default Value");
        SerializedProperty maxLength;
        GUIContent maxLengthLabel = new GUIContent("Max Length");
        SerializedProperty enableDecimal;
        GUIContent enableDecimalLabel = new GUIContent("Enable Decimal");
        SerializedProperty enableSign;
        GUIContent enableSignLabel = new GUIContent("Enable Sign");
        SerializedProperty resultType;
        GUIContent resultTypeLabel = new GUIContent("Result Type");
        SerializedProperty okButton;
        GUIContent okButtonLabel = new GUIContent("Ok Button");
        SerializedProperty cancelButton;
        GUIContent cancelButtonLabel = new GUIContent("Cancel Button");
        SerializedProperty style;
        GUIContent styleLabel = new GUIContent("Style");
        SerializedProperty saveValue;
        GUIContent saveValueLabel = new GUIContent("Save Value");
        SerializedProperty saveKey;
        GUIContent saveKeyLabel = new GUIContent("Save Key");
        SerializedProperty localize;
        GUIContent localizeLabel = new GUIContent("Localize");

        SerializedProperty OnResult;
        SerializedProperty OnResultText;

        private void OnEnable()
        {
            title = serializedObject.FindProperty("title");
            message = serializedObject.FindProperty("message");
            defaultValue = serializedObject.FindProperty("defaultValue");
            maxLength = serializedObject.FindProperty("maxLength");
            enableDecimal = serializedObject.FindProperty("enableDecimal");
            enableSign = serializedObject.FindProperty("enableSign");
            resultType = serializedObject.FindProperty("resultType");
            okButton = serializedObject.FindProperty("okButton");
            cancelButton = serializedObject.FindProperty("cancelButton");
            style = serializedObject.FindProperty("style");
            saveValue = serializedObject.FindProperty("saveValue");
            saveKey = serializedObject.FindProperty("saveKey");
            localize = serializedObject.FindProperty("localize");
            OnResult = serializedObject.FindProperty("OnResult");
            OnResultText = serializedObject.FindProperty("OnResultText");
        }

        public override void OnInspectorGUI()
        {
            var obj = target as NumericTextDialogController;
            serializedObject.Update();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target) , typeof(MonoScript), false);
            EditorGUI.EndDisabledGroup();

            //obj.title = EditorGUILayout.TextField("Title", obj.title);
            EditorGUILayout.PropertyField(title, titleLabel, true);

            //obj.message = EditorGUILayout.TextField("Message", obj.message);
            EditorGUILayout.PropertyField(message, messageLabel, true);

            EditorGUILayout.PropertyField(defaultValue, defaultValueLabel, true);

            //obj.maxLength = EditorGUILayout.IntField("Max Length", obj.maxLength);
            EditorGUILayout.PropertyField(maxLength, maxLengthLabel, true);

            //obj.enableDecimal = EditorGUILayout.Toggle("Enable Decimal", obj.enableDecimal);
            EditorGUILayout.PropertyField(enableDecimal, enableDecimalLabel, true);

            //obj.enableSign = EditorGUILayout.Toggle("Enable Sign", obj.enableSign);
            EditorGUILayout.PropertyField(enableSign, enableSignLabel, true);

            //obj.resultType = (NumericTextDialogController.ResultType)EditorGUILayout.EnumPopup("Result Type", obj.resultType);
            EditorGUILayout.PropertyField(resultType, resultTypeLabel, true);

            //obj.okButton = EditorGUILayout.TextField("Ok Button", obj.okButton);
            EditorGUILayout.PropertyField(okButton, okButtonLabel, true);

            //obj.cancelButton = EditorGUILayout.TextField("Cancel Button", obj.cancelButton);
            EditorGUILayout.PropertyField(cancelButton, cancelButtonLabel, true);

            //obj.style = EditorGUILayout.TextField("Style", obj.style);
            EditorGUILayout.PropertyField(style, styleLabel, true);

            //obj.saveValue = EditorGUILayout.Toggle("Save Value", obj.saveValue);
            EditorGUILayout.PropertyField(saveValue, saveValueLabel, true);

            EditorGUILayout.PropertyField(saveKey, saveKeyLabel, true);

            EditorGUILayout.PropertyField(localize, localizeLabel, true);

            switch (obj.resultType)
            {
                case NumericTextDialogController.ResultType.Value:
                    EditorGUILayout.PropertyField(OnResult, true);
                    break;
                case NumericTextDialogController.ResultType.Text:
                    EditorGUILayout.PropertyField(OnResultText, true);
                    break;
            }

            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }
    }
}
