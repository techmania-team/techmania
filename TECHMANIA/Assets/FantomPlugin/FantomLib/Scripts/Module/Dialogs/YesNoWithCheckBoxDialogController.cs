using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Yes/No with CheckBox Dialog Controller
    /// 
    ///･The value of the callback is 'yesValue' when it is a Yes button pressed, and becomes 'noValue' when it is a No button pressed.
    ///(*) Callback from Android to Unity is received under 'GameObject.name'. That is, it is unique within the hierarchy. 
    ///(*) When using value save (saveChecked), it is better to give a specific save name (saveCheckedKey) individually
    ///    (By default it is saved as GameObject.name [*using PlayerPrefs], so the same name across the scene, it will be overwritten).
    ///(*) Localization is done only once at startup. It does not apply to dynamically modified character strings (Activated by registering 'LocalizeStringResource' in inspector).
    /// (Theme[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// 
    /// 
    ///・「はい」ボタンのときは yesValue、「いいえ」ボタンのときは noValue がコールバックに返される。
    ///※Android から Unity へコールバック受信は「GameObject 名」で行われるため、ヒエラルキー上ではユニークにしておく必要がある。
    ///※値の保存（saveChecked）をするときは、なるべく固有の保存名（saveCheckedKey）を設定した方が良い
    ///（デフォルトではGameObject名で保存されるため[※PlayerPrefs を利用]、シーンをまたがって同じ名前があると上書きされてしまう）。
    ///※ローカライズは起動時に一度だけ行われる。動的に変更した文字列には適用されないので注意（LocalizeStringResource をインスペクタで登録することで有効になる）。
    /// (テーマ[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// </summary>
    public class YesNoWithCheckBoxDialogController : SavedCheckedBehaviour, ILocalizable
    {
        //Inspector Settings
        public string title = "Title";                          //Dialog title
        [Multiline] public string message = "Message";          //Dialog message
        public string yesButton = "OK";                         //Text of 'Yes' button.
        public string yesValue = "yes";                         //Callback value when 'Yes' pressed.
        public string noButton = "Cancel";                      //Text of 'No' button.
        public string noValue = "no";                           //Callback value when 'No' pressed.

        //CheckBox
        [SerializeField] private bool defaultChecked = false;   //Default state of CheckBox (If saved, it will be overwritten).
        public string checkBoxText = "Remember me";             //Text of CheckBox
        public Color checkBoxTextColor = Color.black;           //Text color of CheckBox

        public string style = "android:Theme.DeviceDefault.Light.Dialog.Alert"; //Dialog theme

        //Save PlayerPrefs Settings
        public bool saveChecked = true;                         //Whether to save the CheckBox value (Also local value is always overwritten).

        [Serializable]
        public enum SaveCondition
        {
            Both,               //'Yes' or 'No'
            YesOnly,            //Save only when pressed 'Yes'.
            NoOnly,             //Save only when pressed 'No'.
        }
        public SaveCondition saveCondition = SaveCondition.YesOnly;

        [SerializeField] private string saveCheckedKey = "";    //When specifying the PlayerPrefs key for CheckBox.

        //Localize resource ID data
        [Serializable]
        public class LocalizeData
        {
            public LocalizeStringResource localizeResource;
            public string titleID = "title";
            public string messageID = "message";
            public string yesButtonID = "yesButton";
            public string noButtonID = "noButton";
            public string checkBoxTextID = "checkBoxText";
        }
        public LocalizeData localize;

        //Callbacks
        [Serializable] public class YesHandler : UnityEvent<string, bool> { }   //yesValue, checked
        public YesHandler OnYes;

        [Serializable] public class NoHandler : UnityEvent<string, bool> { }    //noValue, checked
        public NoHandler OnNo;

#region PlayerPrefs Section

        //Defalut PlayerPrefs Key (It is used only when saveCheckedKey is empty)
        const string CHECKED_PREF = "_checked";     //For Checkbox

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
                yesButton = localize.localizeResource.Text(localize.yesButtonID, yesButton);
                noButton = localize.localizeResource.Text(localize.noButtonID, noButton);
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
                yesButton = localize.localizeResource.Text(localize.yesButtonID, language, yesButton);
                noButton = localize.localizeResource.Text(localize.noButtonID, language, noButton);
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
            Debug.Log("YesNoWithCheckBoxDialogController.Show called");
#elif UNITY_ANDROID
            AndroidPlugin.ShowDialogWithCheckBox(
                title,
                message,
                checkBoxText, checkBoxTextColor,
                defaultChecked,
                gameObject.name, "ReceiveResult",
                yesButton, yesValue,
                noButton, noValue,
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


        //Returns value when button pressed.
        private void ReceiveResult(string result)
        {
            bool check = result.EndsWith(", CHECKED_TRUE");
            bool yes = result.StartsWith(yesValue);
            bool no = result.StartsWith(noValue);
            if (saveChecked)
            {
                if (saveCondition == SaveCondition.Both ||
                    (saveCondition == SaveCondition.YesOnly && yes) ||
                    (saveCondition == SaveCondition.NoOnly && no))
                {
                    defaultChecked = check;
                    SavePrefs();
                }
            }

            if (yes)
            {
                if (OnYes != null)
                    OnYes.Invoke(yesValue, check);
            }
            else if (no)
            {
                if (OnNo != null)
                    OnNo.Invoke(noValue, check);
            }
        }
    }
}
