using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Yes/No Dialog Controller
    /// 
    ///･The value of the callback is 'yesValue' when it is a 'Yes' button pressed, and becomes 'noValue' when it is a 'No' button pressed.
    ///(*) Callback from Android to Unity is received under 'GameObject.name'. That is, it is unique within the hierarchy. 
    ///(*) Localization is done only once at startup. It does not apply to dynamically modified character strings (Activated by registering 'LocalizeStringResource' in inspector).
    /// (Theme[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// 
    /// 
    ///・「はい」ボタンのときは yesValue、「いいえ」ボタンのときは noValue がコールバックに返される。
    ///※Android から Unity へコールバック受信は「GameObject 名」で行われるため、ヒエラルキー上ではユニークにしておく必要がある。
    ///※ローカライズは起動時に一度だけ行われる。動的に変更した文字列には適用されないので注意（LocalizeStringResource をインスペクタで登録することで有効になる）。
    /// (テーマ[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// </summary>
    public class YesNoDialogController : LocalizableBehaviour, ILocalizable
    {
        //Inspector Settings
        public string title = "Title";                  //Dialog title
        [Multiline] public string message = "Message";  //Dialog message
        public string yesButton = "OK";                 //Text of 'Yes' button.
        public string yesValue = "yes";                 //Callback value when 'Yes' pressed.
        public string noButton = "Cancel";              //Text of 'No' button.
        public string noValue = "no";                   //Callback value when 'No' pressed.

        public string style = "android:Theme.DeviceDefault.Light.Dialog.Alert"; //Dialog theme

        //Localize resource ID data
        [Serializable]
        public class LocalizeData
        {
            public LocalizeStringResource localizeResource;
            public string titleID = "title";
            public string messageID = "message";
            public string yesButtonID = "yesButton";
            public string noButtonID = "noButton";
        }
        public LocalizeData localize;

        //Callbacks
        [Serializable] public class YesHandler : UnityEvent<string> { }     //yesValue
        public YesHandler OnYes;

        [Serializable] public class NoHandler : UnityEvent<string> { }      //noValue
        public NoHandler OnNo;

#region Properties and Local values Section

        //Initialize localized string
        private void ApplyLocalize()
        {
            if (localize.localizeResource != null)
            {
                title = localize.localizeResource.Text(localize.titleID, title);
                message = localize.localizeResource.Text(localize.messageID, message);
                yesButton = localize.localizeResource.Text(localize.yesButtonID, yesButton);
                noButton = localize.localizeResource.Text(localize.noButtonID, noButton);
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
        public void Show()
        {
#if UNITY_EDITOR
            Debug.Log("YesNoDialogController.Show called");
#elif UNITY_ANDROID
            AndroidPlugin.ShowDialog(
                title,
                message,
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
            if (result == yesValue)
            {
                if (OnYes != null)
                    OnYes.Invoke(yesValue);
            }
            else if (result == noValue)
            {
                if (OnNo != null)
                    OnNo.Invoke(noValue);
            }
        }
    }
}