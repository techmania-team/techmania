using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Storage Open Folder Controller
    ///(*) API 21 [Android 5.0] or higher
    /// 
    /// Get a foler path with the Storage Access Framework (API 21 [Android 5.0] or higher).
    ///·Select a folder with something like a system explorer and return the path.
    ///(*) Note that depending on the returned URI, path conversion is impossible (Google Drive and other cloud storage, etc.).
    ///(*) Sometimes it can not be get correctly depending on the authority (security) or the folder in which it is placed.
    ///(*) Callback from Android to Unity is received under 'GameObject.name'. That is, it is unique within the hierarchy. 
    ///
    /// (Available storage)
    /// Local        | ○ | ok
    /// SD card      | ○ | ok
    /// Google Drive | ─ | --
    /// One Drive    | ─ | --
    /// 
    /// ストレージアクセス機能でフォルダのパスを取得する (API 21 [Android 5.0] 以上)
    ///・システムのエクスプローラのようなものでフォルダを選択し、パスを返す。
    ///※返される URI によってはパス変換できないので注意（Google Drive やその他クラウドストレージ等）。
    ///※権限（セキュリティ）や配置しているフォルダなどによっても正しく取得できないことがある。
    ///※Android → Unity への結果コールバックは「GameObject.name」で受信されるため、ヒエラルキー上ではユニークが良い。
    /// </summary>
    public class StorageOpenFolderController : MonoBehaviour
    {
        //Callbacks
        //Callback when success.                //成功時のコールバック
        [Serializable] public class ResultHandler : UnityEvent<string> { }    //path
        public ResultHandler OnResult;

        [Serializable] public class ResultInfoHandler : UnityEvent<ContentInfo> { }    //{path, uri}
        public ResultInfoHandler OnResultInfo;

        //Callback when error.                  //エラー時のコールバック
        [Serializable] public class ErrorHandler : UnityEvent<string> { }    //error message
        public ErrorHandler OnError;



        // Use this for initialization
        private void Start()
        {

        }

        // Update is called once per frame
        //private void Update()
        //{

        //}


        //Call the Storage Access Framework (API 19 [Android 4.4] or higher).
        public void Show()
        {
#if UNITY_EDITOR
            Debug.Log("StorageOpenFolderController.Show called");
#elif UNITY_ANDROID
            AndroidPlugin.OpenStorageFolder(gameObject.name, "ReceiveResult", "ReceiveError", true);    //Always resultIsJson = true
#endif
        }


        private string ErrorMessage {
            get { return "Failed to get path."; }
        }

        //Callback handler when receive result
        private void ReceiveResult(string result)
        {
            if (result[0] == '{')   //When Json, success.  //Json のとき、取得成功
            {
                ContentInfo info = JsonUtility.FromJson<ContentInfo>(result);

                if (OnResult != null)
                {
                    if (!string.IsNullOrEmpty(info.path))
                        OnResult.Invoke(info.path);
                    else
                        ReceiveError(ErrorMessage);
                }
                if (OnResultInfo != null)
                    OnResultInfo.Invoke(info);  //It is also possible when the path is empty.   //パスが空のときも可
            }
            else
                ReceiveError(result);
        }

        //Callback handler when receive error
        private void ReceiveError(string message)
        {
            if (OnError != null)
                OnError.Invoke(message);
        }
    }
}
