using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Password Text Dialog Controller
    /// 
    ///(*) Callback from Android to Unity is received under 'GameObject.name'. That is, it is unique within the hierarchy. 
    /// (Theme[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// 
    /// 
    ///※Android から Unity へコールバック受信は「GameObject 名」で行われるため、ヒエラルキー上ではユニークにしておく必要がある。
    ///※ローカライズは起動時に一度だけ行われる。動的に変更した文字列には適用されないので注意（LocalizeStringResource をインスペクタで登録することで有効になる）。
    /// (テーマ[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// </summary>
    public class PasswordTextDialogController : LocalizableBehaviour, ILocalizable
    {
        //Inspector Settings
        public string title = "Title";                  //Dialog title
        [Multiline] public string message = "Message";  //Dialog message
        public int maxLength = 0;                       //Limit on number of characters (When it is 0 there is no limit).
        public bool numberOnly = false;                 //Only number characters can be entered.

        public string okButton = "OK";                  //Text of 'OK' button.
        public string cancelButton = "Cancel";          //Text of 'Cancel' button.

        public string style = "android:Theme.DeviceDefault.Light.Dialog.Alert"; //Dialog theme

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
        [Serializable] public class ResultHandler : UnityEvent<string> { }  //text (*)number -> string type
        public ResultHandler OnResult;

#region Properties and Local values Section

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
        }

        private void Start()
        {
            
        }

        // Update is called once per frame
        //private void Update()
        //{

        //}


        //Show dialog
        public void Show(string password)
        {
#if UNITY_EDITOR
            Debug.Log("PasswordTextDialogController.Show called");
#elif UNITY_ANDROID
            AndroidPlugin.ShowPasswordTextDialog(
                title,
                message,
                password,
                maxLength,
                numberOnly,
                gameObject.name, "ReceiveResult",
                okButton, cancelButton,
                style);
#endif
        }


        //Returns value when 'OK' pressed.
        private void ReceiveResult(string result)
        {
            if (OnResult != null)
                OnResult.Invoke(result);    //Note: It is not encrypted (plane text).
        }
    }
}
