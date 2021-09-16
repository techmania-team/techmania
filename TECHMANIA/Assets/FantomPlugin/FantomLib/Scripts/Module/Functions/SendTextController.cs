using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FantomLib
{
    /// <summary>
    /// Send Text Controller
    /// 
    ///･Send text with Chooser (application selection widget).
    ///･If you send it to Twitter application etc., you can share the text.
    ///(*) Localization is done only once at startup. It does not apply to dynamically modified character strings (Activated by registering 'LocalizeStringResource' in inspector).
    /// 
    ///・Chooser（アプリ選択ウィジェット）でテキストの送信をする。
    ///・Twitterなどに送れば、テキストをシェアできる。
    ///※ローカライズは起動時に一度だけ行われる。動的に変更した文字列には適用されないので注意（LocalizeStringResource をインスペクタで登録することで有効になる）。
    /// </summary>
    public class SendTextController : LocalizableBehaviour, ILocalizable
    {
        //Inspector Settings
        public Text targetText;         //UI Text (When using 'Send()')

        [Serializable]
        public enum SelectionType
        {
            Implicit,   //System default
            Chooser,    //Select with Chooser
        }
        public SelectionType selectionType = SelectionType.Chooser;

        public string chooserTitle = "Select the application to share this text.";  //Title of chooser.


        //Localize resource ID data 
        [Serializable]
        public class LocalizeData
        {
            public LocalizeStringResource localizeResource;
            public string chooserTitleID = "chooserTitle";
        }
        public LocalizeData localize;

#region Properties and Local values Section

        //Initialize localized string
        private void ApplyLocalize()
        {
            if (localize.localizeResource != null)
            {
                chooserTitle = localize.localizeResource.Text(localize.chooserTitleID, chooserTitle);
            }
        }

        //Specify language and apply (update) localized string
        public override void ApplyLocalize(SystemLanguage language)
        {
            if (localize.localizeResource != null)
            {
                chooserTitle = localize.localizeResource.Text(localize.chooserTitleID, language, chooserTitle);
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

        
        
        //Send the text of targetText.
        public void Send()
        {
            if (targetText == null || string.IsNullOrEmpty(targetText.text))
                return;
#if UNITY_EDITOR
            Debug.Log("SendTextController.Send : " + targetText.text);
#elif UNITY_ANDROID
            switch (selectionType)
            {
                case SelectionType.Implicit:
                    AndroidPlugin.StartActionSendText(targetText.text);
                    break;
                case SelectionType.Chooser:
                    AndroidPlugin.StartActionSendText(targetText.text, chooserTitle);
                    break;
            }
#endif
        }

        //Send text dynamically (It does not affect 'UI-Text').
        public void Send(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;
#if UNITY_EDITOR
            Debug.Log("SendTextController.Send : " + text);
#elif UNITY_ANDROID
            switch (selectionType)
            {
                case SelectionType.Implicit:
                    AndroidPlugin.StartActionSendText(text);
                    break;
                case SelectionType.Chooser:
                    AndroidPlugin.StartActionSendText(text, chooserTitle);
                    break;
            }
#endif
        }

        //Send text with attached file.
        //Send text dynamically (It does not affect 'UI-Text').
        public void Send(string text, string contentURI)
        {
            if (string.IsNullOrEmpty(text))
                return;
#if UNITY_EDITOR
            Debug.Log("SendTextController.Send : " + text + ", contentURI = " + contentURI);
#elif UNITY_ANDROID
            switch (selectionType)
            {
                case SelectionType.Implicit:
                    AndroidPlugin.StartActionSendTextWithAttachment(text, contentURI);
                    break;
                case SelectionType.Chooser:
                    AndroidPlugin.StartActionSendTextWithAttachment(text, chooserTitle, contentURI);
                    break;
            }
#endif
        }

        //(*) LocalizeString overload
        public void Send(LocalizeString text)
        {
            if (text != null)
                Send(text.Text);
        }

        //(*) LocalizeString overload
        public void Send(LocalizeString text, string contentURI)
        {
            if (text != null)
                Send(text.Text, contentURI);
        }
    }
}
