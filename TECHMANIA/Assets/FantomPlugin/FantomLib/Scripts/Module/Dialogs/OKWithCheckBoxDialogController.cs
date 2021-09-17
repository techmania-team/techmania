using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// OK with CheckBox Dialog Controller
    /// 
    ///･The value of the callback is always 'resultValue'.
    ///(*) Callback from Android to Unity is received under 'GameObject.name'. That is, it is unique within the hierarchy. 
    ///(*) When using value save (saveChecked), it is better to give a specific save name (saveCheckedKey) individually.
    ///    (By default it is saved as GameObject.name [*using PlayerPrefs], so the same name across the scene, it will be overwritten).
    /// (Theme[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// 
    /// 
    ///・コールバックは常に「resultValue」の値が返ってくる。
    ///※Android から Unity へコールバック受信は「GameObject 名」で行われるため、ヒエラルキー上ではユニークにしておく必要がある。
    ///※値の保存（saveChecked）をするときは、なるべく固有の保存名（saveCheckedKey）を設定した方が良い
    ///（デフォルトではGameObject名で保存されるため[※PlayerPrefs を利用]、シーンをまたがって同じ名前があると上書きされてしまう）。
    ///※ローカライズは起動時に一度だけ行われる。動的に変更した文字列には適用されないので注意（LocalizeStringResource をインスペクタで登録することで有効になる）。
    /// (テーマ[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// </summary>
    public class OKWithCheckBoxDialogController : SavedCheckedBehaviour, ILocalizable
    {
        //Inspector Settings
        public string title = "Title";                          //Dialog title
        [Multiline] public string message = "Message";          //Dialog message
        public string okButton = "OK";                          //Text of 'OK' button.
        public string resultValue = "closed";                   //Callback value when closed dialog.

        //CheckBox
        [SerializeField] private bool defaultChecked = false;   //Default state of CheckBox (If saved, it will be overwritten).
        public string checkBoxText = "Remember me";             //Text of CheckBox
        public Color checkBoxTextColor = Color.black;           //Text color of CheckBox

        public string style = "android:Theme.DeviceDefault.Light.Dialog.Alert"; //Dialog theme

        //Save PlayerPrefs Settings
        public bool saveChecked = true;                         //Whether to save the CheckBox value (Also local value is always overwritten).
        [SerializeField] private string saveCheckedKey = "";    //When specifying the PlayerPrefs key for CheckBox.

        //Localize resource ID data
        [Serializable]
        public class LocalizeData
        {
            public LocalizeStringResource localizeResource;
            public string titleID = "title";
            public string messageID = "message";
            public string okButtonID = "okButton";
            public string checkBoxTextID = "checkBoxText";
        }
        public LocalizeData localize;

        //Callbacks
        [Serializable] public class CloseHandler : UnityEvent<string, bool> { }     //resultValue, checked
        public CloseHandler OnClose;

#region PlayerPrefs Section

        //Defalut PlayerPrefs Key (It is used only when saveCheckedKey is empty)
        const string CHECKED_PREF = "_checked";

        //Saved key in PlayerPrefs (Default key is "gameObject.name + '_checked'")
        public string SaveCheckedKey {
            get { return string.IsNullOrEmpty(saveCheckedKey) ? gameObject.name + CHECKED_PREF : saveCheckedKey; }
        }

        //Load local values manually.
        public void LoadPrefs()
        {
            defaultChecked = XPlayerPrefs.GetBool(SaveCheckedKey, defaultChecked);
        }

        //Save local values manually.
        //･To be saved value is only checked state.
        public void SavePrefs()
        {
            XPlayerPrefs.SetBool(SaveCheckedKey, defaultChecked);
            PlayerPrefs.Save();
        }

        //Delete PlayerPrefs key.
        //Note: Local values are not initialized at this time.
        public void DeletePrefs()
        {
            PlayerPrefs.DeleteKey(SaveCheckedKey);
        }

        //Returns true if the saved value exists.
        public bool HasPrefs {
            get { return PlayerPrefs.HasKey(SaveCheckedKey); }
        }

        //Checked already saved state. When first time (before saving) always false.
        public override bool SavedChecked {
            get { return XPlayerPrefs.GetBool(SaveCheckedKey, false); }
        }

#endregion

#region Properties and Local values Section

        //Initial state of CheckBox.
        //･If saveChecked is true, it will be automatically saved.
        public bool DefaultChecked {
            get { return defaultChecked; }
            set {
                defaultChecked = value;
                if (saveChecked)
                    SavePrefs();
            }
        }

        //The value for reset.
        private bool initChecked;

        //Store the value of the inspector.
        private void StoreInitChecked()
        {
            initChecked = defaultChecked;
        }

        //Restore the value of the inspector and delete PlayerPrefs key.
        public void ResetChecked()
        {
            defaultChecked = initChecked;
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
                checkBoxText = localize.localizeResource.Text(localize.checkBoxTextID, checkBoxText);
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
                checkBoxText = localize.localizeResource.Text(localize.checkBoxTextID, language, checkBoxText);
            }
        }

#endregion

        // Use this for initialization
        private void Awake()
        {
            ApplyLocalize();
            StoreInitChecked();

            if (saveChecked)
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
            Debug.Log("OKWithCheckBoxDialogController.Show called");
#elif UNITY_ANDROID
            AndroidPlugin.ShowDialogWithCheckBox(
                title,
                message,
                checkBoxText, checkBoxTextColor,
                defaultChecked, 
                gameObject.name, "ReceiveResult",
                okButton, resultValue,
                style);
#endif
        }

        //Set message dynamically and show dialog (current message will be overwritten)
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


        //The callback value (resultValue) always returns.
        private void ReceiveResult(string result)
        {
            bool check = result.EndsWith(", CHECKED_TRUE");
            if (saveChecked)
            {
                defaultChecked = check;
                SavePrefs();
            }

            if (result.StartsWith(resultValue))
            {
                if (OnClose != null)
                    OnClose.Invoke(resultValue, check);
            }
        }
    }
}
