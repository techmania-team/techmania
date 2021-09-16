using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// OK Dialog Controller
    /// 
    ///･The value of the callback is always 'resultValue'.
    ///(*) Callback from Android to Unity is received under 'GameObject.name'. That is, it is unique within the hierarchy. 
    ///(*) Localization is done only once at startup. It does not apply to dynamically modified character strings (Activated by registering 'LocalizeStringResource' in inspector).
    /// (Theme[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// 
    /// 
    ///・コールバックは常に「resultValue」の値が返ってくる。
    ///※Android から Unity へコールバック受信は「GameObject 名」で行われるため、ヒエラルキー上ではユニークにしておく必要がある。
    ///※ローカライズは起動時に一度だけ行われる。動的に変更した文字列には適用されないので注意（LocalizeStringResource をインスペクタで登録することで有効になる）。
    /// (テーマ[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// </summary>
    public class OKDialogController : LocalizableBehaviour, ILocalizable
    {
        //Inspector Settings
        public string title = "Title";                  //Dialog title
        [Multiline] public string message = "Message";  //Dialog message
        public string okButton = "OK";                  //Text of 'OK' button.
        public string resultValue = "ok";               //Callback value when 'OK' pressed.

        public string style = "android:Theme.DeviceDefault.Light.Dialog.Alert"; //Dialog theme

        //Localize resource ID data
        [Serializable]
        public class LocalizeData
        {
            public LocalizeStringResource localizeResource;
            public string titleID = "title";
            public string messageID = "message";
            public string okButtonID = "okButton";
        }
        public LocalizeData localize;

        //Callbacks
        [Serializable] public class CloseHandler : UnityEvent<string> { }   //resultValue
        public CloseHandler OnClose;

#region Properties and Local values Section

        //Initialize localized string
        private void ApplyLocalize()
        {
            if (localize.localizeResource != null)
            {
                title = localize.localizeResource.Text(localize.titleID, title);
                message = localize.localizeResource.Text(localize.messageID, message);
                okButton = localize.localizeResource.Text(localize.okButtonID, okButton);
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
            Debug.Log("OKDialogController.Show called");
#elif UNITY_ANDROID
            AndroidPlugin.ShowDialog(
                title,
                message,
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


        //Returns value when closed dialog.
        private void ReceiveResult(string result)
        {
            if (result == resultValue)
            {
                if (OnClose != null)
                    OnClose.Invoke(resultValue);
            }
        }
    }
}
