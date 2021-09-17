using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Storage Open Image Controller
    ///(*) API 19 [Android 4.4] or higher
    /// 
    /// Get a file path and image information with the Storage Access Framework (API 19 [Android 4.4] or higher).
    ///·Select a file with something like a system explorer and return the path and image information.
    ///·Information that can be acquired in order of Local storage > SD card > Cloud storage becomes more.
    ///(*) Note that depending on the returned URI, path conversion is impossible (Cloud storage, etc.).
    ///(*) Sometimes it can not be get correctly depending on the authority (security) or the folder in which it is placed.
    ///(*) Callback from Android to Unity is received under 'GameObject.name'. That is, it is unique within the hierarchy. 
    ///
    /// (Available storage)
    /// Local        | ○ | ok
    /// SD card      | ○ | ok
    /// Google Drive | ▲ | Some information can not be acquired ('path' is always empty).
    /// One Drive    | ▲ | Some information can not be acquired ('path' is always empty).
    /// 
    /// ストレージアクセス機能でファイルのパスと画像情報を取得する (API 19 [Android 4.4] 以上)
    ///・システムのエクスプローラのようなものでファイルを選択し、パスと画像情報を返す。
    ///・ローカルストレージ ＞ SDカード ＞ クラウドストレージ の順に取得できる情報が多くなる。
    ///※返される URI によってはパス変換できないので注意（クラウドストレージ等）。
    ///※権限（セキュリティ）や配置しているフォルダなどによっても正しく取得できないことがある。
    ///※Android → Unity への結果コールバックは「GameObject.name」で受信されるため、ヒエラルキー上ではユニークが良い。
    /// </summary>
    public class StorageOpenImageController : StorageOpenControllerBase
    {
        //Callbacks
        [Serializable] public class ResultInfoHandler : UnityEvent<ImageInfo> { }    //{path, uri, etc...}
        public ResultInfoHandler OnResultInfo;

#region Properties and Local values Section

        protected override string DefaultMimeType {
            get { return AndroidMimeType.Image.All; }
        }

#endregion


        //Call the Storage Access Framework (API 19 [Android 4.4] or higher).
        public override void Show()
        {
#if UNITY_EDITOR
            Debug.Log("StorageOpenImageController.Show called");
#elif UNITY_ANDROID
            AndroidPlugin.OpenStorageImage(MimeTypes, gameObject.name, "ReceiveResult", "ReceiveError");    //Always json
#endif
        }


        //Callback handler when receive result
        protected override void ReceiveResult(string result)
        {
            if (result[0] == '{')   //Json
            {
                ImageInfo info = JsonUtility.FromJson<ImageInfo>(result);

                OnResultInvokeOrError(info);

                if (OnResultInfo != null)
                    OnResultInfo.Invoke(info);  //It is also possible when the path is empty.   //パスが空のときも可
            }
            else
                ReceiveError(result);
        }
    }
}
