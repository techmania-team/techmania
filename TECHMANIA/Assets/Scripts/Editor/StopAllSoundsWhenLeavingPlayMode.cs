using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class StopAllSoundsWhenLeavingPlayMode
{
    static StopAllSoundsWhenLeavingPlayMode()
    {
        EditorApplication.playModeStateChanged +=
            (PlayModeStateChange state) =>
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                FmodManager.instance.StopAll();
            }
        };
    }
}
