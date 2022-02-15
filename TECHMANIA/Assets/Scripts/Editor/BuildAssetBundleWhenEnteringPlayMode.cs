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
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                BuildAssetBundleWindow.BuildForDefaultPlatform();
            }
        };
    }
}
