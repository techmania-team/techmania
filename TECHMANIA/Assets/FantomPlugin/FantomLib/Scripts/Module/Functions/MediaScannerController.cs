using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Media Scanner Controller
    /// 
    ///･Run MediaScanner for single or multiple paths.
    ///･If you save your own files without using the Android system, you need to run MediaScanner to recognize the file.
    ///(*) Callback from Android to Unity is received under 'GameObject.name'. That is, it is unique within the hierarchy. 
    ///
    ///・単一または複数のパスについて、MediaScannerを実行する。
    ///・Androidシステムを使わずに、独自にファイルを保存したときなどには、MediaScannerを実行して、ファイルを認識させる必要がある。
    ///※Android から Unity へコールバック受信は「GameObject 名」で行われるため、ヒエラルキー上ではユニークにしておく必要がある。
    /// </summary>
    public class MediaScannerController : MonoBehaviour
    {

        //Callbacks
        [Serializable] public class CompleteHandler : UnityEvent<string> { }    //path
        public CompleteHandler OnComplete;

        [Serializable] public class CompleteInfoHandler : UnityEvent<ContentInfo> { }    //{path, uri}
        public CompleteInfoHandler OnCompleteInfo;


        // Use this for initialization
        private void Start()
        {

        }

        // Update is called once per frame
        //private void Update()
        //{

        //}


        //Scan (update) a single file.
        public void StartScan(string path)
        {
#if UNITY_EDITOR
            Debug.Log("MediaScannerController.StartScan called");
#elif UNITY_ANDROID
            AndroidPlugin.StartMediaScanner(path, gameObject.name, "ReceiveComplete", true);
#endif
        }

        //Scan (update) multiple files.
        public void StartScan(string[] paths)
        {
#if UNITY_EDITOR
            Debug.Log("MediaScannerController.StartScan called");
#elif UNITY_ANDROID
            AndroidPlugin.StartMediaScanner(paths, gameObject.name, "ReceiveComplete", true);
#endif
        }


        //Callback handler when receive complete
        private void ReceiveComplete(string result)
        {
            if (string.IsNullOrEmpty(result))
                return;

            ContentInfo info;
            if (result[0] == '{') //Json
                info = JsonUtility.FromJson<ContentInfo>(result);
            else
                info = new ContentInfo(result, "");   //no URI infomation

            if (OnComplete != null)
                OnComplete.Invoke(result);

            if (OnCompleteInfo != null)
                OnCompleteInfo.Invoke(info);
        }
    }
}
