using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Android;

public class AndroidUtility
{
    public static bool isAndroidR;
    public static void CheckVersion ()
    {
        isAndroidR = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT") >= 30;
    }
    // Check whether Android R has permission to read non media files (.tech, .json, etc) or not.
    public static bool IsExternalStorageManager ()
    {
        return new AndroidJavaClass("android.os.Environment").CallStatic<bool>("isExternalStorageManager");
    }

    // Permission.RequestUserPermission works asynchroniously on Android.
    // so we use IEnumerator and yield to wait for the user to accept the permission.
    public static IEnumerator AskForPermissions(Action callback)
    {
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite) ||
            !Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead))
        {
            // Request manage external access to read non media files (.tech, .json, etc) on Android R.
            if (isAndroidR && !IsExternalStorageManager())
            {
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent", "android.settings.MANAGE_ALL_FILES_ACCESS_PERMISSION");
                activity.Call("startActivity", intent);
                yield return new WaitForEndOfFrame();
            }

            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
            yield return new WaitForEndOfFrame();
            Permission.RequestUserPermission(Permission.ExternalStorageRead);
            yield return new WaitForEndOfFrame();
        }

        callback?.Invoke();
    }
    public static bool HasStoragePermissions()
    {
        return isAndroidR ? IsExternalStorageManager() : Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead) && Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite);
    }
}
