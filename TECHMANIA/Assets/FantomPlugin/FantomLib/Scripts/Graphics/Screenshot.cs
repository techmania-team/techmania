using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FantomLib
{
    /// <summary>
    /// Create Screenshot (full screen) and save file (png)
    /// </summary>
    public class Screenshot : MonoBehaviour
    {
        //Inspector Settings
        public string filePrefix = "screenshot_";       //Prefix of filename to be saved.
        public bool toExternalStorageOnAndroid = true;  //Save to external storage (for Android)


        //Callbacks
        [Serializable] public class CompleteHandler : UnityEvent<string> { }    //path
        public CompleteHandler OnComplete;

        [Serializable] public class ErrorHandler : UnityEvent<string> { }       //error state message
        public ErrorHandler OnError;


#region Properties and Local values Section

        //Busy flag. Ignore while saving.
        public bool IsSaving {
            get; private set;
        }

#endregion

        // Use this for initialization
        private void Start()
        {

        }

        // Update is called once per frame
        //private void Update()
        //{

        //}


        //Run screenshot with automatic path
        public void StartScreenshot()
        {
            string fileName = filePrefix + DateTime.Now.ToString("yyMMdd_HHmmss") + ".png";
            string dir = Application.persistentDataPath;

            //I think that you may add processing for each platform here.
#if UNITY_EDITOR
            dir = Application.dataPath + "/..";
#elif UNITY_ANDROID
            if (toExternalStorageOnAndroid)
            {
                if (!AndroidPlugin.CheckPermission("android.permission.WRITE_EXTERNAL_STORAGE"))
                {
                    if (OnError != null)
                        OnError.Invoke("'WRITE_EXTERNAL_STORAGE' permission denied.");
                    return;
                }

                dir = AndroidPlugin.GetExternalStorageDirectoryPictures();
                if (string.IsNullOrEmpty(dir))
                    dir = AndroidPlugin.GetExternalStorageDirectory();
            }
#endif
            string path = dir + "/" + fileName;
            StartScreenshot(path);
        }
    
        //Run screenshot with specified path
        public void StartScreenshot(string path)
        {
            if (IsSaving)
            {
                if (OnError != null)
                    OnError.Invoke("Screenshot is currently busy.");
                return;     //Ignore while saving.
            }

            StartCoroutine(RunScreenshot(path));
        }


        //Hide the UI and execute the screenshot. If save the screenshot successfully, run MeidaScanner.
        private IEnumerator RunScreenshot(string path)
        {
            IsSaving = true;

            yield return StartCoroutine(SaveScreenshotPng(path));

            if (IsSaving)   //If an error occurs, it is false on 'SaveScreenshotPng()'.
            {
                if (OnComplete != null)
                    OnComplete.Invoke(path);
            }

            IsSaving = false;
        }


        //(*)To write to External Storage on Android, you need permission in the 'AndroidManifest.xml' file.
        //※Android で External Storage に書き込みをするには、「AndroidManifest.xml」にパーミッションが必要。
        //Required: '<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />' in 'AndroidManifest.xml'
        //
        //･The script referred to the following.
        // https://docs.unity3d.com/jp/540/ScriptReference/Texture2D.EncodeToPNG.html
        private IEnumerator SaveScreenshotPng(string path)
        {
	        // We should only read the screen buffer after rendering is complete
	        yield return new WaitForEndOfFrame();

	        // Create a texture the size of the screen, RGB24 format
	        int width = Screen.width;
	        int height = Screen.height;
	        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);

	        // Read screen contents into the texture
	        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
	        tex.Apply();

	        // Encode texture into PNG
	        byte[] bytes = tex.EncodeToPNG();
            DestroyImmediate(tex);

            //For testing purposes, also write to a file in the project folder
            try
            {
                File.WriteAllBytes(path, bytes);
            }
            catch (Exception e)
            {
                IsSaving = false;           //It also serves as an error flag
                if (OnError != null)
                    OnError.Invoke(e.ToString());
            }

            yield return new WaitForEndOfFrame();
        }
    }
}
