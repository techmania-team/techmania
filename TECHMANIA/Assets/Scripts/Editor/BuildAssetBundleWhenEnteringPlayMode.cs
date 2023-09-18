using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public static class BuildAssetBundleWhenEnteringPlayMode
{
    static BuildAssetBundleWhenEnteringPlayMode()
    {
        EditorApplication.playModeStateChanged +=
            (PlayModeStateChange state) =>
        {
            if (state != PlayModeStateChange.ExitingEditMode)
            {
                return;
            }
            try
            {
                BuildAssetBundleWindow.BuildForDefaultPlatform();
            }
            catch (System.Exception ex)
            {
                Debug.Log("An exception occurred when building asset bundle. Will not enter play mode. See next log for exception.");
                Debug.LogException(ex);
                EditorApplication.ExitPlaymode();
            }
        };
    }
}
