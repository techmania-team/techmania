using UnityEngine;
using MoonSharp.Interpreter;
using FantomLib;

public class AndroidHelper : MonoBehaviour
{
#if UNITY_ANDROID
    public void OnSelectFolder()
    {
        AndroidPlugin.OpenStorageFolder(gameObject.name, "OnFolderSelected", "", true);
    }
    public void OnFolderSelected(string result)
    {
        if (result[0] == '{')
        {
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
                    ThemeApi.Techmania.OnAndroidFolderSelected(info.path);
                }));
        }
    }
    public void OnSelectFile()
    {
        AndroidPlugin.OpenStorageFile(gameObject.name, "OnFileSelected", "", true);
    }
    public void OnFileSelected(string result)
    {
        if (result[0] == '{')
        {
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
                    ThemeApi.Techmania.OnAndroidFileSelected(info.path);
                }));
        }
    }
#endif
}
