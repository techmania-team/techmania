using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Storage Save File Controller
    ///(*) API 19 [Android 4.4] or higher
    ///
    /// Get a file path for save with the Storage Access Framework (API 19 [Android 4.4] or higher).
    ///·Gives the file a name with something like a system explorer and return the path.
    ///·After execution, 0 byte file is created, so you can save data etc. by overwriting as it is.
    ///·Basically from Unity you can save directly to Local storage only (security reason, external storage write is limited).
    ///·When saved from Unity, the Android system does not automatically recognize it, so it will be able to recognize by running Media Scanner.
    ///(*) Note that depending on the returned URI, path conversion is impossible (Cloud storage, etc.).
    ///(*) Sometimes it can not be get correctly depending on the authority (security) or the folder in which it is placed.
    ///(*) Callback from Android to Unity is received under 'GameObject.name'. That is, it is unique within the hierarchy. 
    ///(*) When using value save (saveValue), it is better to give a specific save name (saveKey) individually.
    /// 
    /// (Available storage)
    /// Local        | ○ | ok
    /// SD card      | × | ERROR_CREATE_DOCUMENT_WRITE_ACCESS_DENIED
    /// Google Drive | × | ERROR_CREATE_DOCUMENT_WRITE_ACCESS_DENIED
    /// One Drive    | ─ | --
    /// 
    /// ストレージアクセス機能で保存用のファイルのパスを取得する (API 19 [Android 4.4] 以上)
    ///・システムのエクスプローラのようなものでファイルに名前を付けてパスを返す。
    ///・実行後は０バイトのファイルが作成されるので、そのまま上書きすればデータなどを保存できる。
    ///・基本的に Unity からはローカルストレージにのみ直接保存できる（セキュリティ上、外部ストレージ書き込みは制限される）。
    ///・Unity から保存した場合、Android システムは自動では認識しないので、Media Scanner を走らせることで認識できるようになる。
    ///※返される URI によってはパス変換できないので注意（クラウドストレージ等）。
    ///※権限（セキュリティ）や配置しているフォルダなどによっても正しく取得できないことがある。
    ///※Android → Unity への結果コールバックは「GameObject.name」で受信されるため、ヒエラルキー上ではユニークが良い。
    ///※値の保存（saveValue）を利用するときは、特定の保存名（saveKey）を個々に与えた方が良い。
    /// </summary>
    public class StorageSaveFileController : MonoBehaviour
    {
        //Inspector settings
        [SerializeField] private string fileName = DEFAULT_FILENAME;      //FileName to save (not include directory path). //保存するファイル名（ディレクトリパスは含まない）
        [SerializeField] private string[] mimeTypes = { AndroidMimeType.File.All };
        public bool syncExtension = true;               //Add extension to fileName by MiME type automatically

        //Save PlayerPrefs Settings
        public bool saveValue = false;                  //Whether to save the last result path.
        [SerializeField] private string saveKey = "";   //When specifying the PlayerPrefs key.     //特定の保存名を付けるとき


        //Callbacks
        //Callback when specific file path for save.    //結果のコールバック
        [Serializable] public class ResultHandler : UnityEvent<string> { }    //absolute path
        public ResultHandler OnResult;

        [Serializable] public class ResultInfoHandler : UnityEvent<ContentInfo> { }    //file information {path, uri, ...}
        public ResultInfoHandler OnResultInfo;

        //Callback when error.                          //エラー時のコールバック
        [Serializable] public class ErrorHandler : UnityEvent<string> { }    //error message
        public ErrorHandler OnError;

#region PlayerPrefs Section

        //Defalut PlayerPrefs Key (It is used only when saveKey is empty)
        const string PATH_PREF = "_path";

        //Saved key in PlayerPrefs (Default key is "gameObject.name + '_path'")
        public string SaveKey {
            get { return string.IsNullOrEmpty(saveKey) ? gameObject.name + PATH_PREF : saveKey; }
        }

        //Load local values manually.
        public void LoadPrefs()
        {
            path = PlayerPrefs.GetString(SaveKey, path);    //Last saved path, but actually using file name only
            if (!string.IsNullOrEmpty(path))
                fileName = Path.GetFileName(path);
        }

        //Save local values manually.
        public void SavePrefs()
        {
            PlayerPrefs.SetString(SaveKey, path);           //Last saved path, but actually using file name only
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

        const string DEFAULT_MIME_TYPE = AndroidMimeType.File.All;
        const string DEFAULT_FILENAME = "NewDocument.txt";
        private string path = "";       //Last result path (only fileName is actually used)

        //DEFAULT_FILENAME synchronized extension by MIME type
        string DefaultFileName {
            get {
                if (syncExtension)
                {
                    var ext = AndroidMimeType.GetExtension(MimeType);    //nothing or all("*") = null
                    if (ext != null && ext.Length > 0)
                        return Path.ChangeExtension(DEFAULT_FILENAME, ext[0]);  //'.' can be either with or without it
                }
                return DEFAULT_FILENAME;
            }
        }

        //The currently (last result) path.
        //(*) Since it returns the result after execution, it is always empty in the initial state (or saved path).
        //※実行後の結果を返すため、初期状態では常に空になる（または保存されているパス）。
        public string CurrentValue {
            get { return path; }
        }

        //File name to save
        //(*) Note that it will be different from CurrentValue (path) if set.
        //※セットした場合、CurrentValue(path)とは異なる値になるので注意。
        public string FileName {
            get { return fileName; }
            set {
                fileName = string.IsNullOrEmpty(value) ? DefaultFileName : value;
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

            var ext = AndroidMimeType.GetExtension(MimeTypes[0]);       //nothing or all("*") = null
            if (ext != null && ext.Length > 0)     //Ignore if not found (It will stay at the current setting)
                FileName = Path.ChangeExtension(fileName, ext[0]);      //'.' can be either with or without it
        }

        //Multiple MIME type specifications.
        //･For example, "text/comma-separated-values" and "text/csv" are used for csv file.
        //(*) Note that valid MIME type by provider (storage) is different and may not apply.
        //
        //複数の MIME type 指定。
        //・例えば、csv ファイルは "text/comma-separated-values" と "text/csv" が使われる。
        //※プロバイダ（ストレージ）によって有効な MIME type は異なり、適用されない場合もあるので注意。
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
            fileName = DefaultFileName;     //If syncExtension is true, add ext

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

        

        //Call the default storage access framework (like explorer).
        //Set fileName dynamically (current value will be overwritten).
        public void Show()
        {
#if UNITY_EDITOR
            Debug.Log("StorageSaveFileController.Show called");
#elif UNITY_ANDROID
            AndroidPlugin.OpenStorageForSave(fileName, MimeTypes, gameObject.name, "ReceiveResult", "ReceiveError", true);
#endif
        }

        //Call the default storage access framework (like explorer).
        //Set fileName dynamically (current value will be overwritten).
        public void Show(string fileName)
        {
            this.fileName = fileName;
            Show();
        }


        //Callback handler when save text success
        private void ReceiveResult(string result)
        {
            if (result[0] == '{')   //Json
            {
                ContentInfo info = JsonUtility.FromJson<ContentInfo>(result);
                path = info.path;
                if (!string.IsNullOrEmpty(info.name))
                {
                    fileName = info.name;
                    if (string.IsNullOrEmpty(fileName) && !string.IsNullOrEmpty(path))
                        fileName = Path.GetFileName(path);
                }

                if (saveValue)
                    SavePrefs();

                if (OnResult != null)
                    OnResult.Invoke(info.path);

                if (OnResultInfo != null)
                    OnResultInfo.Invoke(info);
            }
            else
                ReceiveError(result);
        }

        //Callback handler when error
        private void ReceiveError(string message)
        {
            if (OnError != null)
                OnError.Invoke(message);
        }
    }
}
