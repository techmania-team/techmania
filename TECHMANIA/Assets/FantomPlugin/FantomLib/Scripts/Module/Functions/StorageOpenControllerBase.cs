using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Storage Open Controller Base
    ///(*) API 19 [Android 4.4] or higher
    /// 
    /// Get a file path with the Storage Access Framework (API 19 [Android 4.4] or higher).
    ///·Select a file with something like a system explorer and return the path.
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
    /// ストレージアクセス機能でファイルのパスを取得する (API 19 [Android 4.4] 以上)
    ///・システムのエクスプローラのようなものでファイルを選択し、パスを返す。
    ///　ローカルストレージ ＞ SDカード ＞ クラウドストレージ の順に取得できる情報が多くなる。
    ///※返される URI によってはパス変換できないので注意（クラウドストレージ等）。
    ///※権限（セキュリティ）や配置しているフォルダなどによっても正しく取得できないことがある。
    ///※Android → Unity への結果コールバックは「GameObject.name」で受信されるため、ヒエラルキー上ではユニークが良い。
    /// </summary>
    public abstract class StorageOpenControllerBase : MonoBehaviour
    {
        //Inspector Settings
        [SerializeField] protected string[] mimeTypes;

        //Callbacks
        //Callback when success.                //成功時のコールバック
        [Serializable] public class ResultHandler : UnityEvent<string> { }    //path (or text)
        public ResultHandler OnResult;

        //Callback when error.                  //エラー時のコールバック
        [Serializable] public class ErrorHandler : UnityEvent<string> { }    //error message
        public ErrorHandler OnError;

#region Properties and Local values Section

        protected virtual string DefaultMimeType {
            get { return AndroidMimeType.File.All; }
        }

        //Multiple MIME type specifications.
        //･For example, "text/comma-separated-values" and "text/csv" are used for csv file.
        //(*) Note that valid MIME type by provider (storage) is different and may not apply.
        //
        //複数の MIME type 指定。
        //・例えば、csv ファイルは "text/comma-separated-values" と "text/csv" が使われる。
        //※プロバイダ（ストレージ）によって有効な MIME type は異なり、適用されない場合もあるので注意。
        public virtual string[] MimeTypes {
            get {
                if (mimeTypes == null || mimeTypes.Length == 0)
                    mimeTypes = new string[] { DefaultMimeType };

                return mimeTypes;
            }
            set {
                if (value != null && value.Length > 0)
                    mimeTypes = value;
                else
                    mimeTypes = new string[] { DefaultMimeType };
            }
        }

        //Set up as a single MIME type. However, it is internally managed by array.
        //(*) Acquisition returns [0]. When multiple MIME types are set, it is desirable that [0] is the main one.
        //(*) Note that valid MIME type by provider (storage) is different and may not apply.
        //
        //単一で MIME type 設定。ただし、内部では配列で管理される。
        //※取得は [0] が返される。複数の MIME type がセットされてる場合、[0] が主要なものであることが望ましい。
        //※プロバイダ（ストレージ）によって有効な MIME type は異なり、適用されない場合もあるので注意。
        public virtual string MimeType {
            get {
                return MimeTypes[0];
            }
            set {
                string mimeType = value;
                if (string.IsNullOrEmpty(mimeType))
                    mimeType = DefaultMimeType;

                if (mimeTypes != null && mimeTypes.Length == 1)
                    mimeTypes[0] = mimeType;
                else
                    mimeTypes = new string[]{ mimeType };
            }
        }

#endregion

        // Use this for initialization
        protected void Awake()
        {
            if (mimeTypes == null || mimeTypes.Length == 0)
                MimeType = DefaultMimeType;
        }

        protected void Start()
        {

        }

        // Update is called once per frame
        //protected void Update()
        //{

        //}


        //Call the Storage Access Framework (API 19 [Android 4.4] or higher).
        public virtual void Show()
        {
#if UNITY_EDITOR
            Debug.Log(name + " Show called");
#elif UNITY_ANDROID
            AndroidPlugin.OpenStorageFile(MimeTypes, gameObject.name, "ReceiveResult", "ReceiveError", true);    //Always Json
#endif
        }


        protected virtual string ErrorMessage {
            get { return "Failed to get path."; }
        }

        protected ContentInfo info;

        //Callback handler when receive result
        protected virtual void ReceiveResult(string result)
        {
            if (result[0] == '{')   //Json
            {
                info = JsonUtility.FromJson<ContentInfo>(result);
                OnResultInvokeOrError(info);
            }
            else
                ReceiveError(result);
        }

        //If 'OnResult' is not empty, invoke or error.
        //OnResult が空でないとき、コールバックまたはエラーが発生する
        protected virtual void OnResultInvokeOrError(ContentInfo info)
        {
            if (!IsNullOrEmpty(OnResult))
            {
                if (!string.IsNullOrEmpty(info.path))
                    OnResult.Invoke(info.path);
                else
                    ReceiveError(ErrorMessage);
            }
        }

        //Callback handler when receive error
        protected virtual void ReceiveError(string message)
        {
            if (OnError != null)
                OnError.Invoke(message);
        }

#region Static method etc. Section

        // Returns whether or not callback is empty to the UnityEvent of the inspector.
        //(*) Note that those registered with 'UnityEvent.AddListener()' are ignored.
        //
        // インスペクタの UnityEvent にコールバック空か否かを返す。
        //※UnityEvent.AddListener() で登録したものは無視されるので注意。
        protected static bool IsNullOrEmpty<T0> (UnityEvent<T0> obj)
        {
            if (obj != null)
            {
                int count = obj.GetPersistentEventCount();  //for inspector only
                for (int i = 0; i < count; i++)
                {
                    if (!string.IsNullOrEmpty(obj.GetPersistentMethodName(i)))  //for inspector only
                        return false;
                }
            }
            return true;
        }

#endregion
    }
}
