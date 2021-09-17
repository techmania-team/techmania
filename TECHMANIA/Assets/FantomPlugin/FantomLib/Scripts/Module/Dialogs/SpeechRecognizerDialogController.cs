using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Speech Recognizer Dialog Controller (Use dialog of Google)
    /// 
    ///(*) Callback from Android to Unity is received under 'GameObject.name'. That is, it is unique within the hierarchy. 
    ///(*) Localization is done only once at startup. It does not apply to dynamically modified character strings (Activated by registering 'LocalizeStringResource' in inspector).
    /// (Locale list)
    /// http://fantom1x.blog130.fc2.com/blog-entry-295.html
    ///
    ///※Android から Unity へコールバック受信は「GameObject 名」で行われるため、ヒエラルキー上ではユニークにしておく必要がある。
    ///※ローカライズは起動時に一度だけ行われる。動的に変更した文字列には適用されないので注意（LocalizeStringResource をインスペクタで登録することで有効になる）。
    /// (Locale 一覧)
    /// http://fantom1x.blog130.fc2.com/blog-entry-295.html
    /// </summary>
    public class SpeechRecognizerDialogController : LocalizableBehaviour, ILocalizable
    {
        //Inspector Settings
        [SerializeField] private string locale = "";        //Locale (e.g. "en", "en-US", "ja", "ja-JP") / empty = system default
        [Multiline] public string message = "";             //Dialog message

        //Save PlayerPrefs Settings
        public bool saveSetting = false;                    //Whether to save the settings (Also local value is always overwritten).
        [SerializeField] private string saveKey = "";       //When specifying the PlayerPrefs key for settings.

        //Localize resource ID data
        [Serializable]
        public class LocalizeData
        {
            public LocalizeStringResource localizeResource;
            public string messageID = "message";
        }
        public LocalizeData localize;

        //Callbacks
        [Serializable] public class ResultHandler : UnityEvent<string[]> { }    //recognization words
        public ResultHandler OnResult;                  //Callback when recognization success

        [Serializable] public class ErrorHandler : UnityEvent<string> { }       //error state message
        public ErrorHandler OnError;

#region PlayerPrefs Section

        //Defalut PlayerPrefs Key (It is used only when saveKey is empty)
        const string SETTING_PREF = "_setting";
        const string LOCALE_KEY = "locale";

        private Param setting = new Param();    //Compatibale Dictionary <string, string>

        //Saved key in PlayerPrefs (Default key is "gameObject.name + '_setting'")
        public string SaveKey {
            get { return string.IsNullOrEmpty(saveKey) ? gameObject.name + SETTING_PREF : saveKey; }
        }

        //Load local values manually.
        public void LoadPrefs()
        {
            setting = Param.GetPlayerPrefs(SaveKey, setting);
            Locale = setting.GetString(LOCALE_KEY, Locale);
        }

        //Save local values manually.
        public void SavePrefs()
        {
            setting.Set(LOCALE_KEY, Locale);
            Param.SetPlayerPrefs(SaveKey, setting);
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

        //Properties
        private static bool isSupportedRecognizer = false;  //Cached supported Speech Recognizer (Because Recognizer shares one, it is static).
        private static bool isSupportedChecked = false;     //Already checked (Because Recognizer shares one, it is static).

        //Whether the device supports Speech Recognizer.
        public bool IsSupportedRecognizer {
            get {
                if (!isSupportedChecked)
                {
#if UNITY_EDITOR
                    isSupportedRecognizer = true;       //For Editor (* You can rewrite it as you like.)
#elif UNITY_ANDROID
                    isSupportedRecognizer = AndroidPlugin.IsSupportedSpeechRecognizer();
#endif
                    isSupportedChecked = true;
                }
                return isSupportedRecognizer;
            }
        }

        //Whether necessary permissions are granted.
        public bool IsPermissionGranted {
            get {
#if UNITY_EDITOR
                return true;    //For Editor (* You can rewrite it as you like.)
#elif UNITY_ANDROID
                return AndroidPlugin.CheckPermission("android.permission.RECORD_AUDIO");
#else
                return false;
#endif
            }
        }

        //Change locale (empty is system default)
        public string Locale {
            get { return locale; }
            set {
                if (locale != value)
                {
                    locale = (value == AndroidLocale.Default) ? "" : value;

                    if (saveSetting)
                        SavePrefs();
                }
            }
        }


        //Initialize localized string
        private void ApplyLocalize()
        {
            if (localize.localizeResource != null)
            {
                message = localize.localizeResource.Text(localize.messageID, message);
            }
        }

        //Specify language and apply (update) localized string
        public override void ApplyLocalize(SystemLanguage language)
        {
            if (localize.localizeResource != null)
            {
                message = localize.localizeResource.Text(localize.messageID, language, message);
            }
        }

#endregion

        // Use this for initialization
        private void Awake()
        {
            ApplyLocalize();

            if (saveSetting)
                LoadPrefs();
        }

        private void Start()
        {
            if (!IsSupportedRecognizer)
            {
                if (OnError != null)
                    OnError.Invoke("Not supported: Speech Recognizer");
            }
            if (!IsPermissionGranted)
            {
                if (OnError != null)
                    OnError.Invoke("Permission denied: RECORD_AUDIO");
            }
        }

        // Update is called once per frame
        //private void Update()
        //{

        //}


        //Show dialog
        public void Show()
        {
            if (!IsSupportedRecognizer || !IsPermissionGranted)
                return;
#if UNITY_EDITOR
            Debug.Log("ShowSpeechRecognizer called");
#elif UNITY_ANDROID
            if (string.IsNullOrEmpty(locale))
                AndroidPlugin.ShowSpeechRecognizer(
                    gameObject.name, "ReceiveResult", message);
            else
                AndroidPlugin.ShowSpeechRecognizer(locale,
                    gameObject.name, "ReceiveResult", message);
#endif
        }

        //Set message dynamically and show (current message will be overwritten)
        public void Show(string message)
        {
            this.message = message;
            Show();
        }

        //(*) LocalizeString overload
        public void Show(LocalizeString message)
        {
            if (message != null)
                Show(message.Text);
        }


        //Returns value when speech recognition succeeds.
        private void ReceiveResult(string result)
        {
            if (string.IsNullOrEmpty(result))
                return;

            if (OnResult != null)
                OnResult.Invoke(result.Split('\n'));
        }
    }
}
