using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FantomLib
{
    /// <summary>
    /// Toast Controller
    ///
    ///(*) Localization is done only once at startup. It does not apply to dynamically modified character strings (Activated by registering 'LocalizeStringResource' in inspector).
    /// 
    ///※ローカライズは起動時に一度だけ行われる。動的に変更した文字列には適用されないので注意（LocalizeStringResource をインスペクタで登録することで有効になる）。
    /// </summary>
    public class ToastController : LocalizableBehaviour, ILocalizable
    {
        //Inspector Settings
        [Multiline] public string message = "Message";  //Message to be displayed on Toast.
        public bool longDuration = false;               //Display time is long (true = 3.5s / false = 2.0s).

        //Localize resource ID data
        [Serializable]
        public class LocalizeData
        {
            public LocalizeStringResource localizeResource;
            public string messageID = "message";
        }
        public LocalizeData localize;

#region Properties and Local values Section

        //Initialize localized string
        private void ApplyLocalize()
        {
            if (localize.localizeResource != null)
            {
                message = localize.localizeResource.Text(localize.messageID, message);
            }
        }

        //Specify language and apply (update) localized string
        //(*) When dynamically changing message, it is better not to use it because it is incompatible.
        //※動的に message を変更する場合は、互換性がないので使わない方が良い。
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
        }

        private void Start()
        {

        }

        // Update is called once per frame
        //private void Update()
        //{

        //}

        
        //Show Toast with local message
        public void Show()
        {
#if UNITY_EDITOR
            Debug.Log("ToastController.Show called : " + message);
#elif UNITY_ANDROID
            AndroidPlugin.ShowToast(message, longDuration);
#endif
        }

        //Set message dynamically and show (current message will be overwritten)
        public void Show(string message)
        {
            this.message = message;
            Show();
        }

        //Set message and longDuration dynamically, and show (current message will be overwritten)
        public void Show(string message, bool longDuration)
        {
            this.message = message;
            this.longDuration = longDuration;
            Show();
        }

        //(*) LocalizeString overload
        public void Show(LocalizeString message)
        {
            if (message != null)
                Show(message.Text);
        }

        //(*) LocalizeString overload
        public void Show(LocalizeString message, bool longDuration)
        {
            if (message != null)
                Show(message.Text, longDuration);
        }


        //Force close Toast
        public void Cancel()
        {
#if UNITY_EDITOR
            Debug.Log("ToastController.Cancel called");
#elif UNITY_ANDROID
            AndroidPlugin.CancelToast();
#endif
        }
    }
}
