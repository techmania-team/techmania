using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FantomLib
{
    /// <summary>
    /// Storage Load Text Controller
    ///(*) API 19 [Android4.4] or higher
    /// 
    /// Read a text file with the Storage Access Framework (API 19 [Android4.4] or higher).
    ///·Select a text file with something like a system explorer and return the loaded text.
    ///(*) Sometimes it can not be get correctly depending on the authority (security) or the folder in which it is placed.
    ///(*) Callback from Android to Unity is received under 'GameObject.name'. That is, it is unique within the hierarchy. 
    ///
    /// (Available storage)
    /// Local        | ○ | ok
    /// SD card      | ○ | ok
    /// Google Drive | ○ | ok
    /// One Drive    | ○ | ok
    /// 
    /// ストレージアクセス機能でテキストファイルを読み込む (API 19 [Android4.4] 以上)
    ///・システムのエクスプローラのようなものでテキストファイルを選択し、ロードしたテキストを返す。
    ///※権限（セキュリティ）や配置しているフォルダなどによっても正しく取得できないことがある。
    ///※Android → Unity への結果コールバックは「GameObject.name」で受信されるため、ヒエラルキー上ではユニークが良い。
    /// </summary>
    public class StorageLoadTextController : StorageOpenControllerBase
    {
        //Inspector Settings
        public Text targetText;         //UI Text

#region Properties and Local values Section

        protected override string DefaultMimeType {
            get { return AndroidMimeType.File.txt; }
        }

#endregion


        const string ENCODING = "UTF-8";

        //Call the default storage access framework (like explorer).
        public override void Show()
        {
#if UNITY_EDITOR
            Debug.Log("StorageLoadTextController.Show called");
#elif UNITY_ANDROID
            AndroidPlugin.OpenStorageAndLoadText(ENCODING, MimeTypes, gameObject.name, "ReceiveResult", "ReceiveError");
#endif
        }


        //Callback handler when load text success
        protected override void ReceiveResult(string text)
        {
            if (targetText != null)
                targetText.text = text;

            if (OnResult != null)
                OnResult.Invoke(text);
        }
    }
}
