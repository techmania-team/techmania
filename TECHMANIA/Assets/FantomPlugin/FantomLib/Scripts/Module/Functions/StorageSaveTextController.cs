using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FantomLib
{
    /// <summary>
    /// Storage Save Text Controller
    ///(*) API 19 [Android4.4] or higher
    /// 
    /// Write a text file with the Storage Access Framework (API 19 [Android4.4] or higher).
    ///·Select a text file with something like a system explorer and return the loaded text.
    ///·Write fails in cloud storage (It seems that security can not be saved directly).
    ///(*) Note that depending on the returned URI, path conversion is impossible (Cloud storage, etc.).
    ///(*) Sometimes it can not be get correctly depending on the authority (security) or the folder in which it is placed.
    ///(*) Callback from Android to Unity is received under 'GameObject.name'. That is, it is unique within the hierarchy. 
    ///(*) When using value save (saveValue), it is better to give a specific save name (saveKey) individually.
    ///
    /// (Available storage)
    /// Local        | ○ | ok
    /// SD card      | ○ | ok
    /// Google Drive | × | ERROR_CREATE_DOCUMENT_WRITE_ACCESS_DENIED
    /// One Drive    | ─ | --
    /// 
    /// ストレージアクセス機能でテキストファイルを読み込む (API 19 [Android4.4] 以上)
    ///・システムのエクスプローラのようなものでテキストファイルを選択し、ロードしたテキストを返す。
    ///・クラウドストレージには保存に失敗する（セキュリティ上、直接保存できないと思われる）。
    ///※返される URI によってはパス変換できないので注意（クラウドストレージ等）。
    ///※権限（セキュリティ）や配置しているフォルダなどによっても正しく取得できないことがある。
    ///※Android → Unity への結果コールバックは「GameObject.name」で受信されるため、ヒエラルキー上ではユニークが良い。
    ///※値の保存（saveValue）を利用するときは、特定の保存名（saveKey）を個々に与えた方が良い。
    /// </summary>
    public class StorageSaveTextController : MonoBehaviour
    {
        //Inspector settings
        public Text targetText;                         //UI Text

        public string fileName = DEFAULT_FILENAME;      //FileName to save (not include directory path). //保存するファイル名（ディレクトリパスは含まない）
        [SerializeField] private string[] mimeTypes = { AndroidMimeType.File.txt };
        public bool syncExtension = true;               //Add extension to fileName by MiME type automatically

        //Save PlayerPrefs Settings
        public bool saveValue = false;                  //Whether to save the last fileName (Also local value is always overwritten).
        [SerializeField] private string saveKey = "";   //When specifying the PlayerPrefs key.      //特定の保存名を付けるとき


        //Callbacks
        //Callback when saved text (UTF-8).             //セーブ成功時のコールバック
        [Serializable] public class ResultHandler : UnityEvent<string> { }    //saved file name (not include directory path)
        public ResultHandler OnResult;

        //Callback when error.                          //エラー時のコールバック
        [Serializable] public class ErrorHandler : UnityEvent<string> { }    //error message
        public ErrorHandler OnError;

#region PlayerPrefs Section

        //Defalut PlayerPrefs Key (It is used only when saveKey is empty)
        const string FILE_PREF = "_file";

        //Saved key in PlayerPrefs (Default key is "gameObject.name + '_file'")
        public string SaveKey {
            get { return string.IsNullOrEmpty(saveKey) ? gameObject.name + FILE_PREF : saveKey; }
        }

        //Load local values manually.
        public void LoadPrefs()
        {
            fileName = PlayerPrefs.GetString(SaveKey, fileName);
        }

        //Save local values manually.
        public void SavePrefs()
        {
            PlayerPrefs.SetString(SaveKey, fileName);
            PlayerPrefs.Save();
        }

        //Delete PlayerPrefs key.
        //Note: Local values are not initialized at this time.
        public void DeletePrefs()
        {
            PlayerPrefs.DeleteKey(SaveKey);
        }

        //Returns true if the saved value exists.
        public bool HasPrefs {
            get { return PlayerPrefs.HasKey(SaveKey); }
        }

#endregion

#region Properties and Local values Section

        const string DEFAULT_MIME_TYPE = AndroidMimeType.File.txt;
        const string DEFAULT_FILENAME = "NewDocument.txt";

        //The currently (default) fileName.
        //･If saveValue is true, it will be automatically saved.
        public string CurrentValue {
            get { return FileName; }
            set { FileName = value; }
        }

        //File name to save
        //･If saveValue is true, it will be automatically saved.
        public string FileName {
            get { return fileName; }
            set {
                fileName = string.IsNullOrEmpty(value) ? DEFAULT_FILENAME : value;
                if (saveValue)
                    SavePrefs();
            }
        }

        //Fit extension to MIME type.
        //(*) When there is no corresponding extension (including "*"), it is ignored (= as it is at present).
        //(*) If there are multiple corresponding extensions, [0] is used.
        //
        //拡張子を MIME type に合わせる。
        //※対応する拡張子がないとき（"*"も含む）は無視される（＝現状のまま）。
        //※複数の対応拡張子がある場合、[0] が使われる。
        private void SyncFileNameExtension()
        {
            if (string.IsNullOrEmpty(FileName))
                return;

            var ext = AndroidMimeType.GetExtension(MimeTypes[0]);    //nothing or all(*) = null
            if (ext != null && ext.Length > 0)     //Ignore if not found (It will stay at the current setting)
                FileName = Path.ChangeExtension(fileName, ext[0]);     //'.' can be either with or without it
        }


        //Multiple MIME type specifications.
        //･For example, "text/comma-separated-values" and "text/csv" are used for csv file.
        //(*) Note that valid MIME type by provider is different and may not apply.
        //
        //複数の MIME type 指定。
        //・例えば、csv ファイルは "text/comma-separated-values" と "text/csv" が使われる。
        //※プロバイダによって有効な MIME type は異なり、適用されない場合もあるので注意。
        public string[] MimeTypes {
            get {
                if (mimeTypes == null || mimeTypes.Length == 0)
                    mimeTypes = new string[] { DEFAULT_MIME_TYPE };

                return mimeTypes;
            }
            set {
                if (value != null && value.Length > 0)
                    mimeTypes = value;
                else
                    mimeTypes = new string[] { DEFAULT_MIME_TYPE };

                if (syncExtension)
                    SyncFileNameExtension();
            }
        }

        //Set up as a single MIME type. However, it is internally managed by array.
        //(*) Acquisition returns [0]. When multiple MIME types are set, it is desirable that [0] is the main one.
        //(*) Note that valid MIME type by provider is different and may not apply.
        //
        //単一で MIME type 設定。ただし、内部では配列で管理される。
        //※取得は [0] が返される。複数の MIME type がセットされてる場合、[0] が主要なものであることが望ましい。
        //※プロバイダによって有効な MIME type は異なり、適用されない場合もあるので注意。
        public string MimeType {
            get {
                return MimeTypes[0];
            }
            set {
                string mimeType = value;
                if (string.IsNullOrEmpty(mimeType))
                    mimeType = DEFAULT_MIME_TYPE;

                if (mimeTypes != null && mimeTypes.Length == 1)
                    mimeTypes[0] = mimeType;
                else
                    mimeTypes = new string[]{ mimeType };

                if (syncExtension)
                    SyncFileNameExtension();
            }
        }

#endregion

        // Use this for initialization
        private void Awake()
        {
            if (saveValue)
                LoadPrefs();
        }

        private void Start()
        {

        }

        // Update is called once per frame
        //private void Update()
        //{

        //}

        
        const string ENCODING = "UTF-8";

        //Overload to save the contents of UI-Text
        //UI-Textの内容を保存するオーバーロード
        public void Show()
        {
            if (targetText != null)
                Show(targetText.text);
        }

        //Call the default storage access framework (like explorer).
        public void Show(string text)
        {
#if UNITY_EDITOR
            Debug.Log("StorageSaveTextController.Show called");
#elif UNITY_ANDROID
            AndroidPlugin.OpenStorageAndSaveText(fileName, text, ENCODING, MimeTypes, gameObject.name, "ReceiveResult", "ReceiveError");
#endif
        }

        //Call the default storage access framework (like explorer).
        //Set fileName dynamically (current value will be overwritten).
        public void Show(string fileName, string text)
        {
            this.fileName = fileName;
            Show(text);
        }


        //Callback handler when save text success
        private void ReceiveResult(string fileName)
        {
            if (saveValue)
            {
                this.fileName = fileName;
                SavePrefs();
            }

            if (OnResult != null)
                OnResult.Invoke(fileName);
        }

        //Callback handler when error
        private void ReceiveError(string message)
        {
            if (OnError != null)
                OnError.Invoke(message);
        }
    }
}
