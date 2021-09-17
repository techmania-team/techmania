using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Gallery Pick Controller
    /// 
    /// Open the default gallery application and get the image and movie file information (path, width, height and other).
    ///･If there are two or more application, select with the launcher.
    ///(*) Recommended since API 19 (Android 4.4). In the API Level before that, file information may not be get correctly according to the specification of the default folder or ContentProvider.
    ///(*) When reading from external storage (SD card etc.), permission is necessary ('READ_EXTERNAL_STORAGE' or 'WRITE_EXTERNAL_STORAGE').
    ///(*) The URI returned by the application you use is different, so be careful as the information you can get is different.
    ///   (The format like "content://media/external/images/media/(ID)" is the best (Standard application), in the case of application specific URI, information may be restricted)
    ///(*) There is a possibility that width, height and other information can not be acquired depending on the saved state of the media (0 or empty when it can not be acquired).
    ///(*) Sometimes it can not be get correctly depending on the authority (security) or the folder in which it is placed.
    ///(*) Callback from Android to Unity is received under 'GameObject.name'. That is, it is unique within the hierarchy. 
    ///
    /// 
    /// デフォルトのギャラリーアプリを開いて、画像・動画ファイル情報（パスと幅・高さ 等）を取得する。
    ///・アプリが複数ある場合、ランチャーで選択する。
    ///※API 19 (Android 4.4) 以降推奨。それより前の API Level ではデフォルトのフォルダやコンテンツプロバイダの仕様により、正しくファイル情報が取得できない可能性あり。
    ///※External Storage（SDカード等）から読み込む場合にはパーミッションが必要（'READ_EXTERNAL_STORAGE' または 'WRITE_EXTERNAL_STORAGE'）。
    ///※利用するアプリによって返される URI は異なり、取得できる情報が違うので注意
    ///（"content://media/external/images/media/(ID)" のような書式が一番良い（標準アプリ）。アプリ特有の URI の場合、情報が制限される可能性あり）。
    ///※メディアの保存状態によっては、幅や高さ、その他の情報が取得できない可能性がある（取得できなかったときは 0 または空になる）。
    ///※権限（セキュリティ）や配置しているフォルダなどによっても正しく取得できないことがある。
    ///※Android から Unity へコールバック受信は「GameObject 名」で行われるため、ヒエラルキー上ではユニークにしておく必要がある。
    /// </summary>
    public class GalleryPickController : MonoBehaviour
    {
        //Inspector Settings
        public enum PickType { Image, Video }
        public PickType pickType;


        //Callbacks
        //Callback when get file information is successful.     //ファイル情報の取得が成功したときのコールバック
        [Serializable] public class ResultHandler : UnityEvent<string, int, int> { }    //path, width, height
        public ResultHandler OnResult;

        //For image information
        [Serializable] public class ResultInfoHandler : UnityEvent<ImageInfo> { }   //{path, uri, width, height, size, mimeType}
        public ResultInfoHandler OnResultInfo;

        //For video information
        [Serializable] public class ResultVideoInfoHandler : UnityEvent<VideoInfo> { }   //{path, uri, width, height, size, mimeType, ...}
        public ResultVideoInfoHandler OnResultVideoInfo;

        //Callback when get file information is fail.           //ファイル情報の取得が失敗したときのコールバック
        [Serializable] public class ErrorHandler : UnityEvent<string> { }       //error state message
        public ErrorHandler OnError;            //Callback when fail to get path or other error



        // Use this for initialization
        private void Start()
        {

        }

        // Update is called once per frame
        //private void Update()
        //{

        //}

        

        //Call the default gallery application. If there are two or more, select with the launcher.
        public void Show()
        {
#if UNITY_EDITOR
            Debug.Log("GalleryPickController.Show called");
#elif UNITY_ANDROID
            switch (pickType)
            {
                case PickType.Image:
                    AndroidPlugin.OpenGallery(gameObject.name, "ReceiveResult", "ReceiveError");
                    break;
                case PickType.Video:
                    AndroidPlugin.OpenGalleryVideo(gameObject.name, "ReceiveResult", "ReceiveError");
                    break;
            }
#endif
        }


        //Callback handler when receive result
        private void ReceiveResult(string result)
        {
            if (result[0] == '{')   //When Json, success.  //Json のとき、取得成功
            {
                switch (pickType)
                {
                    case PickType.Image:
                    {
                        ImageInfo info = JsonUtility.FromJson<ImageInfo>(result);
                        if (OnResult != null)
                            OnResult.Invoke(info.path, info.width, info.height);
                        if (OnResultInfo != null)
                            OnResultInfo.Invoke(info);
                    }
                    break;

                    case PickType.Video:
                    {
                        VideoInfo info = JsonUtility.FromJson<VideoInfo>(result);
                        if (OnResult != null)
                            OnResult.Invoke(info.path, info.width, info.height);
                        if (OnResultVideoInfo != null)
                            OnResultVideoInfo.Invoke(info);
                    }
                    break;
                }
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
