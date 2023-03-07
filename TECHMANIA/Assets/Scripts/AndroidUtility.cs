using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Android;

public class AndroidUtility
{
    // Android 11 (Android R) introduces new privacy restrictions.
    // We have to check user's device version and use different methods to request permissions and access files.
    // https://developer.android.com/about/versions/11/privacy/permissions
    // https://developer.android.com/about/versions/11/privacy/storage
    //
    // Non-media files (like txt, pdf, csv, ...) can be accessed on Android 11 if and only if:
    // - The text files are in the → ASD (App Specific Directory) in the → Private dir (i.e. were saved there), OR
    // - The text files are in one of the Shared folders /Documents or /Download AND these files were created by the app itself, OR
    // - SAF (Storage Access Framework) is used, OR
    // - MANAGE_EXTERNAL_STORAGE permission is requested and granted.
    // https://community.appinventor.mit.edu/t/how-to-access-non-media-media-files-on-android-11/54828
    //
    // SAF on Android 11 also has some limitations:
    // https://developer.android.com/training/data-storage/shared/documents-files#document-tree-access-restrictions
    // 
    // So we have to request MANAGE_EXTERNAL_STORAGE permission on Android 11 to access data from wherever user wants.
    public static bool isAndroidR;

    public static void CheckVersion ()
    {
        isAndroidR = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT") >= 30;
    }

    // Check whether Android R has permission to read non media files (.tech, .json, etc) or not.
    private static bool IsExternalStorageManager ()
    {
        return new AndroidJavaClass("android.os.Environment").CallStatic<bool>("isExternalStorageManager");
    }

    // Permission.RequestUserPermission works asynchronously on Android.
    // so we use IEnumerator and yield to wait for the user to accept the permission.
    public static IEnumerator AskForPermissions(Action callback)
    {
        if (!HasStoragePermissions())
        {
            if (isAndroidR)
            {
                // Request manage external access to read non media files (.tech, .json, etc) on Android R.
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent",
                    "android.settings.MANAGE_ALL_FILES_ACCESS_PERMISSION");
                activity.Call("startActivity", intent);
                yield return null;
            }
            else
            {
                Permission.RequestUserPermission(Permission.ExternalStorageWrite);
                yield return null;
                Permission.RequestUserPermission(Permission.ExternalStorageRead);
                yield return null;
            }
        }

        callback?.Invoke();
    }

    public static bool HasStoragePermissions()
    {
        return isAndroidR ? IsExternalStorageManager() :
            Permission.HasUserAuthorizedPermission(Permission.ExternalStorageRead) &&
            Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite);
    }
}
