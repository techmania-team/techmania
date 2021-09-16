using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Mailer Controller
    ///
    ///(*) Behavior may be different depending on the mailer (application) selected by the user. There are parameters that can not be received by the application (Gmail recommended).
    ///(*) If you add an attachment file, it will be the same method as text sharing (text + attached file send), so applications other than mailer will also appear on the list.
    ///(*) Localization is done only once at startup. It does not apply to dynamically modified character strings (Activated by registering 'LocalizeStringResource' in inspector).
    /// (Theme[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// 
    /// 
    ///※ユーザーが選択したメーラー（アプリ）によって挙動が異なる場合がある。アプリによって受け取れないパラメタがある（Gmail 推奨）。
    ///※添付ファイルを追加した場合は、テキスト共有と同じ方法になるので（テキスト＋添付ファイル送信）、メーラー以外のアプリも一覧に出てくる。
    ///※ローカライズは起動時に一度だけ行われる。動的に変更した文字列には適用されないので注意（LocalizeStringResource をインスペクタで登録することで有効になる）。
    /// (テーマ[Style])
    /// https://developer.android.com/reference/android/R.style.html#Theme
    /// </summary>
    public class MailerController : LocalizableBehaviour, ILocalizable
    {
        //Inspector Settings
        public string mailAddress = "xxx@example.com";
        public string subject = "Title";                //mail title
        [Multiline] public string body = "Message";     //mail body

        //Localize resource ID data
        [Serializable]
        public class LocalizeData
        {
            public LocalizeStringResource localizeResource;
            public string subjecctID = "subject";
            public string bodyID = "body";
        }
        public LocalizeData localize;

#region Properties and Local values Section

        private string attachmentURI;       //image etc.

        //Replace with only one URI.
        public void SetAttachment(string contentURI)
        {
            attachmentURI = contentURI;
        }

        //Clear URI
        public void ClearAttachment()
        {
            attachmentURI = "";
        }


        //Initialize localized string
        private void ApplyLocalize()
        {
            if (localize.localizeResource != null)
            {
                subject = localize.localizeResource.Text(localize.subjecctID, subject);
                body = localize.localizeResource.Text(localize.bodyID, body);
            }
        }

        //Specify language and apply (update) localized string
        //(*) When dynamically changing text string, it is better not to use it because it is incompatible.
        //※動的にテキストを変更する場合は、互換性がないので使わない方が良い。
        public override void ApplyLocalize(SystemLanguage language)
        {
            if (localize.localizeResource != null)
            {
                subject = localize.localizeResource.Text(localize.subjecctID, language, subject);
                body = localize.localizeResource.Text(localize.bodyID, language, body);
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


        //Show Mailer with local values
        public void Show()
        {
#if UNITY_EDITOR
            Debug.Log(name + ".Show : mailAddress = " + mailAddress + ", attachment = " + attachmentURI);
#elif UNITY_ANDROID
            if (!string.IsNullOrEmpty(attachmentURI))
            {
                string[] extra = { "android.intent.extra.EMAIL", "android.intent.extra.SUBJECT", "android.intent.extra.TEXT", "android.intent.extra.STREAM" };
                string[] query = { mailAddress, subject, body, attachmentURI };
                AndroidPlugin.StartAction("android.intent.action.SEND", extra, query, "text/plain");  //SENDTO can not be used when attachment is added (It is the same way as text send).  //添付ファイル付きの場合は SENDTO は使えない（テキスト送信と同じ方法になる）
            }
            else
                AndroidPlugin.StartActionSendMail(mailAddress, subject, body);
#endif
        }

        //Set values dynamically and show Mailer (current values will be overwritten)
        public void Show(string mailAddress)
        {
            Show(mailAddress, this.subject, this.body);
        }

        //Set values dynamically and show Mailer (current values will be overwritten)
        public void Show(string mailAddress, string subject)
        {
            Show(mailAddress, subject, this.body);
        }

        //Set values dynamically and show Mailer (current values will be overwritten)
        public void Show(string mailAddress, string subject, string body)
        {
            this.mailAddress = mailAddress;
            this.subject = subject;
            this.body = body;
            ClearAttachment();
            Show();
        }

        //Add attachment
        //Set values dynamically and show Mailer (current values will be overwritten)
        public void Show(string mailAddress, string subject, string body, string attachmentURI)
        {
            this.mailAddress = mailAddress;
            this.subject = subject;
            this.body = body;
            SetAttachment(attachmentURI);
            Show();
        }

        //(*) LocalizeString overload
        //Set values dynamically and show Mailer (current values will be overwritten)
        public void Show(string mailAddress, LocalizeString subject, LocalizeString body)
        {
            this.mailAddress = mailAddress;
            if (subject != null)
                this.subject = subject.Text;
            if (body != null)
                this.body = body.Text;
            ClearAttachment();
            Show();
        }

        //(*) LocalizeString overload
        //Add attachment
        //Set values dynamically and show Mailer (current values will be overwritten)
        public void Show(string mailAddress, LocalizeString subject, LocalizeString body, string attachmentURI)
        {
            this.mailAddress = mailAddress;
            if (subject != null)
                this.subject = subject.Text;
            if (body != null)
                this.body = body.Text;
            SetAttachment(attachmentURI);
            Show();
        }
    }
}
