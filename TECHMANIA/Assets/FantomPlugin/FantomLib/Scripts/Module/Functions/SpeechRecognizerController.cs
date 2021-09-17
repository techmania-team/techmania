using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Speech Recognizer Controller (Without dialog)
    ///(*) Callback from Android to Unity is received under 'GameObject.name'. That is, it is unique within the hierarchy. 
    ///
    /// (Locale)
    ///･Format：BCP47 (e.g. "en-US", "ja-JP") [* This plug-in can also be "en_US", "ja_JP" (under bar)]
    /// https://developer.android.com/reference/android/speech/RecognizerIntent.html#EXTRA_LANGUAGE
    /// https://tools.ietf.org/html/bcp47
    /// (Locale list)
    /// http://fantom1x.blog130.fc2.com/blog-entry-295.html
    /// </summary>
    public class SpeechRecognizerController : MonoBehaviour
    {
        //Inspector Settings
        [SerializeField] private string locale = "";        //Locale (e.g. "en", "en-US", "ja", "ja-JP") / empty = system default

        //Save PlayerPrefs Settings
        public bool saveSetting = false;                    //Whether to save the settings (Also local value is always overwritten).
        [SerializeField] private string saveKey = "";       //When specifying the PlayerPrefs key for settings.

        //Callbacks
        public UnityEvent OnReady;              //Callback when microphone standby.
        public UnityEvent OnBegin;              //Callback when microphone begin speech recognization.

        [Serializable] public class ResultHandler : UnityEvent<string[]> { }    //recognization words
        public ResultHandler OnResult;          //Callback when recognization success

        [Serializable] public class ErrorHandler : UnityEvent<string> { }       //error state message
        public ErrorHandler OnError;            //Callback when recognization fail

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
                    StopRecognizer();
                    locale = (value == AndroidLocale.Default) ? "" : value;

                    if (saveSetting)
                        SavePrefs();
                }
            }
        }


        //Local Values
        private bool canceled = false;  //Interrupted recognizer flag (With the time lag of message reception, prevents callback events from occurring)

#endregion

        // Use this for initialization
        private void Awake()
        {
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

        
        //Start Speech Recognizer
        public void StartRecognizer()
        {
            if (!IsSupportedRecognizer || !IsPermissionGranted)
                return;

            canceled = false;
#if UNITY_EDITOR
            Debug.Log("SpeechRecognizerController.StartRecognizer called");
#elif UNITY_ANDROID
            if (string.IsNullOrEmpty(locale))
                AndroidPlugin.StartSpeechRecognizer(
                    gameObject.name, "ReceiveResult", "ReceiveError", "ReceiveReady", "ReceiveBegin");
            else 
                AndroidPlugin.StartSpeechRecognizer(locale,
                    gameObject.name, "ReceiveResult", "ReceiveError", "ReceiveReady", "ReceiveBegin");
#endif
        }


        //Microphone standby state
        private void ReceiveReady(string message)
        {
            if (canceled)
                return;

            if (OnReady != null)
                OnReady.Invoke();
        }

        //Microphone begin speech recognization state
        private void ReceiveBegin(string message)
        {
            if (canceled)
                return;

            if (OnBegin != null)
                OnBegin.Invoke();
        }

        //Receive the result when speech recognition succeed.
        private void ReceiveResult(string message)
        {
            if (canceled)
                return;

            if (string.IsNullOrEmpty(message))
                return;

            if (OnResult != null)
                OnResult.Invoke(message.Split('\n'));
        }

        //Receive the error when speech recognition fail.
        private void ReceiveError(string message)
        {
            if (canceled)
                return;

            if (OnError != null)
                OnError.Invoke(message);
        }


        //Interrupt speech recognition
        public void StopRecognizer()
        {
            canceled = true;
#if UNITY_EDITOR
            Debug.Log("SpeechRecognizerController.StopRecognizer called");
#elif UNITY_ANDROID
            AndroidPlugin.ReleaseSpeechRecognizer();
#endif
        }
    }
}
