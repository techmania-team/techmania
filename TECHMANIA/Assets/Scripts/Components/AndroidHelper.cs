using UnityEngine;
using MoonSharp.Interpreter;
using FantomLib;

public class AndroidHelper : MonoBehaviour
{
#if UNITY_ANDROID
    public delegate void SelectCompleteCallback(string path);
    
    private SelectCompleteCallback selectCompleteCallback;

    public void OnSelectFolder(SelectCompleteCallback completeCallback)
    {
        selectCompleteCallback = completeCallback;
        AndroidPlugin.OpenStorageFolder(gameObject.name, "OnFolderSelected", "", true);
    }
    public void OnFolderSelected(string result)
    {
        // result is a JSON string with the following format:
        // {
        //     "path":"/storage/3864-3562/TECHMANIA/Tracks",
        //     "uri":"content://com.android.externalstorage.documents/tree/3864-3562%3ATECHMANIA%2FTracks",
        //     "decodedUri":"content://com.android.externalstorage.documents/tree/3864-3562:TECHMANIA/Tracks",
        //     "fileUri":"",
        //     "name":"Tracks"
        // }
        if (result[0] != '{')   return;

        StartCoroutine(AndroidUtility.AskForPermissions(
            callback: () =>
            {
                // Turn off custom data location and reset skins
                // if user denied permission.
                // Otherwise, there will be an error while loading skins.
                if (!AndroidUtility.HasStoragePermissions())
                {
                    Options.instance.ResetCustomDataLocation();
                }
                ContentInfo info = JsonUtility.FromJson<ContentInfo>(result);
                selectCompleteCallback?.Invoke(info.path);
            }));
    }
    public void OnSelectFile(SelectCompleteCallback completeCallback)
    {
        selectCompleteCallback = completeCallback;
        AndroidPlugin.OpenStorageFile(gameObject.name, "OnFileSelected", "", true);
    }
    public void OnFileSelected(string result)
    {
        // result is a JSON string with the following format:
        // {
        //     "uri":"content://com.android.externalstorage.documents/document/primary%3ATECHMANIA%2FFile.png",
        //     "decodedUri":"content://com.android.externalstorage.documents/document/primary:TECHMANIA/File.png",
        //     "path":"/storage/emulated/0/TECHMANIA/File.png",
        //     "fileUri":"content://media/external/file/123456789",
        //     "size":13501,
        //     "name":"File.png",
        //     "mimeType":"image/png"
        // }

        if (result[0] != '{')   return;

        StartCoroutine(AndroidUtility.AskForPermissions(
            callback: () =>
            {
                // Turn off custom data location and reset skins
                // if user denied permission.
                // Otherwise, there will be an error while loading files.
                if (!AndroidUtility.HasStoragePermissions())
                {
                    Options.instance.ResetCustomDataLocation();
                }
                ContentInfo info = JsonUtility.FromJson<ContentInfo>(result);
                selectCompleteCallback?.Invoke(info.path);
            }));
    }
#endif
}
