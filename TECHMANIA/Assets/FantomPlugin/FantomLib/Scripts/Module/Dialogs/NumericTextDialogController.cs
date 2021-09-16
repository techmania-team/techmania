using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Numeric Text Dialog Controller
    /// 
    ///(*) Callback from Android to Unity is received under 'GameObject.name'. That is, it is unique within the hierarchy. 
    ///(*) When using value save (saveValue), it is better to give a specific save name (saveKey) individually.
    ///    (By default it is saved as GameObject.name [*using PlayerPrefs], so the same name across the scene, it will be overwritten).
    ///(*) Localization is done only once at startup. It does not apply to dynamically modified character strings (Activated by registering 'LocalizeStringResource' in inspector).
    /// (Theme[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// 
    /// 
    ///※Android から Unity へコールバック受信は「GameObject 名」で行われるため、ヒエラルキー上ではユニークにしておく必要がある。
    ///※値の保存（saveValue）をするときは、なるべく固有の保存名（saveKey）を設定した方が良い
    ///（デフォルトではGameObject名で保存されるため[※PlayerPrefs を利用]、シーンをまたがって同じ名前があると上書きされてしまう）。
    ///※ローカライズは起動時に一度だけ行われる。動的に変更した文字列には適用されないので注意（LocalizeStringResource をインスペクタで登録することで有効になる）。
    /// (テーマ[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// </summary>
    public class NumericTextDialogController : LocalizableBehaviour, ILocalizable
    {
        //Inspector Settings
        public string title = "Title";                  //Dialog title
        [Multiline] public string message = "Message";  //Dialog message
        [SerializeField] private float defaultValue = 0;//Default value
        public int maxLength = 0;                       //Limit on number of characters (When it is 0 there is no limit. Including decimal and sign).
        public bool enableDecimal = false;              //Set to true when using decimal.
        public bool enableSign = false;                 //Set to true when using sign.

        //Callback value type
        [Serializable]
        public enum ResultType
        {
            Value,      //float type
            Text,       //string type
        }
        public ResultType resultType = ResultType.Value;

        public string okButton = "OK";                  //Text of 'OK' button.
        public string cancelButton = "Cancel";          //Text of 'Cancel' button.

        public string style = "android:Theme.DeviceDefault.Light.Dialog.Alert"; //Dialog theme

        //Save PlayerPrefs Settings
        public bool saveValue = false;                  //Whether to save the text (Also local value is always overwritten).
        [SerializeField] private string saveKey = "";   //When specifying the PlayerPrefs key for text.

        //Localize resource ID data
        [Serializable]
        public class LocalizeData
        {
            public LocalizeStringResource localizeResource;
            public string titleID = "title";
            public string messageID = "message";
            public string okButtonID = "okButton";
            public string cancelButtonID = "cancelButton";
        }
        public LocalizeData localize;

        //Callbacks
        [Serializable] public class ResultHandler : UnityEvent<float> { }   //float value
        public ResultHandler OnResult;

        [Serializable] public class ResultTextHandler : UnityEvent<string> { }   //text string
        public ResultTextHandler OnResultText;

#region PlayerPrefs Section

        //Defalut PlayerPrefs Key (It is used only when saveKey is empty)
        const string TEXT_PREF = "_text";

        //Saved key in PlayerPrefs (Default key is "gameObject.name + '_text'")
        public string SaveKey {
            get { return string.IsNullOrEmpty(saveKey) ? gameObject.name + TEXT_PREF : saveKey; }
        }

        //Load local values manually.
        public void LoadPrefs()
        {
            string str = PlayerPrefs.GetString(SaveKey, defaultValue.ToString()); //string type in PlayerPrefs.
            float value;
            if (float.TryParse(str, out value))
                defaultValue = value;
        }

        //Save local values manually.
        public void SavePrefs()
        {
            PlayerPrefs.SetString(SaveKey, defaultValue.ToString());    //string type in PlayerPrefs.
            PlayerPrefs.Save();
        }

        //To save callback results directly (string type). Because there is rounding error will occur when converting float type.
        private void SavePrefs(string result)
        {
            PlayerPrefs.SetString(SaveKey, result);
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

        //The currently (default) value.
        //･If saveValue is true, it will be automatically saved.
        public float CurrentValue {
            get { return defaultValue; }
            set {
                defaultValue = value;
                if (saveValue)
                    SavePrefs();
            }
        }


        //The value for reset.
        private float initValue;

        //Store the values of the inspector.
        private void StoreInitValue()
        {
            initValue = defaultValue;
        }

        //Restore the value of inspector and delete PlayerPrefs key.
        public void ResetValue()
        {
            defaultValue = initValue;
            DeletePrefs();
        }


        //Initialize localized string
        private void ApplyLocalize()
        {
            if (localize.localizeResource != null)
            {
                title = localize.localizeResource.Text(localize.titleID, title);
                message = localize.localizeResource.Text(localize.messageID, message);
                okButton = localize.localizeResource.Text(localize.okButtonID, okButton);
                cancelButton = localize.localizeResource.Text(localize.cancelButtonID, cancelButton);
            }
        }

        //Specify language and apply (update) localized string
        public override void ApplyLocalize(SystemLanguage language)
        {
            if (localize.localizeResource != null)
            {
                title = localize.localizeResource.Text(localize.titleID, language, title);
                message = localize.localizeResource.Text(localize.messageID, language, message);
                okButton = localize.localizeResource.Text(localize.okButtonID, language, okButton);
                cancelButton = localize.localizeResource.Text(localize.cancelButtonID, language, cancelButton);
            }
        }

#endregion

        // Use this for initialization
        private void Awake()
        {
            ApplyLocalize();
            StoreInitValue();

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


        //Show dialog
        public void Show()
        {
#if UNITY_EDITOR
            Debug.Log("NumericTextDialogController.Show called");
#elif UNITY_ANDROID
            AndroidPlugin.ShowNumericTextDialog(
                title,
                message,
                defaultValue,
                maxLength,
                enableDecimal,
                enableSign,
                gameObject.name, "ReceiveResult",
                okButton, cancelButton,
                style);
#endif
        }

        //Set value dynamically and show dialog (current value will be overwritten)
        //Note: When changed dynamically, it is inconsistent with the value saved in Playerprefs (better to use saveValue is false).
        public void Show(float value)
        {
            defaultValue = value;
            Show();
        }


        //Returns value when 'OK' pressed.
        private void ReceiveResult(string result)
        {
            result = string.IsNullOrEmpty(result) ? "0" : result;   //empty is "0" (text string)

            float value;
            if (!float.TryParse(result, out value))
                return;

            if (saveValue)
            {
                defaultValue = value;
                SavePrefs(result);      //Saved as string type (Because there is rounding error will occur when converting float type).
            }

            switch (resultType)
            {
                case ResultType.Value:
                    if (OnResult != null)
                        OnResult.Invoke(value);
                    break;
                case ResultType.Text:
                    if (OnResultText != null)
                        OnResultText.Invoke(result);
                    break;
            }
        }
    }
}