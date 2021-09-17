using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Permission Check Controller
    /// 
    /// Check if permission is given and call back (Synonymous with 'in AndroidManifest.xml').
    ///·Requestable permissions need to be written in "AndroidManifest.xml" in advance.
    ///·There is no request on the device before API 23 (Android 6.0), always callback only the result.
    ///·The explanation dialog of the rationale will not always appear if the user checks "Don't ask again" on request.
    ///·Use "Constant Value" in the developer manual for the permission string (eg: "android.permission.WRITE_EXTERNAL_STORAGE").
    /// https://developer.android.com/reference/android/Manifest.permission.html
    ///
    /// 
    /// パーミッションが許可（付与）されているかどうかを調べ、コールバックする（「AndroidManifest.xml」にあるか？と同義）。
    ///・要求できるパーミッションはあらかじめ「AndroidManifest.xml」に書かれている必要がある。
    ///・API 23 (Android 6.0) より前のデバイスでは要求は出ず、常に結果のみをコールバックする。
    ///・根拠の説明ダイアログは、要求のときユーザーが「今後表示しない」をチェックすると常に出なくなる。
    ///・パーミッションの文字列はデベロッパーマニュアルの「Constant Value」を使う（例："android.permission.WRITE_EXTERNAL_STORAGE"）。
    /// https://developer.android.com/reference/android/Manifest.permission.html
    ///==========================================================
    ///·Permissions used in fantomPlugin are as follows:
    ///・プラグインで利用するパーミッションは以下の通り：
    /// android.permission.RECORD_AUDIO
    /// android.permission.WRITE_EXTERNAL_STORAGE (or android.permission.READ_EXTERNAL_STORAGE : When read only)
    /// android.permission.BLUETOOTH
    /// android.permission.VIBRATE
    /// android.permission.BODY_SENSORS
    ///==========================================================
    /// </summary>
    public class PermissionCheckController : LocalizableBehaviour, ILocalizable
    {
        //Inspector Settings
        public string permission = "android.permission.WRITE_EXTERNAL_STORAGE";     //Permission to check

        public bool checkOnStart = false;               //Execute check automatically at 'Start()'

        public bool requestWhenNotGranted = false;      //If permission is not granted, give explanation of the rationale dialog and request.

        public string title = "Title";                  //Rationale dialog title
        [Multiline] public string message = "Message";  //Rationale dialog message
        
        public string style = "android:Theme.DeviceDefault.Light.Dialog.Alert"; //Rationale dialog theme

        //Localize resource ID data
        [Serializable]
        public class LocalizeData
        {
            public LocalizeStringResource localizeResource;
            public string titleID = "title";            //Rationale dialog title ID
            public string messageID = "message";        //Rationale dialog message ID
        }
        public LocalizeData localize;


        //Callbacks
        [Serializable] public class ResultHandler : UnityEvent<string, bool> { }    //permission, granted
        public ResultHandler OnResult;

        public UnityEvent OnGranted;        //When permission granted
        public UnityEvent OnDenied;         //When permission denied

        [Serializable] public class AllowedHandler : UnityEvent<string> { }    //permission
        public AllowedHandler OnAllowed;    //When permission granted after the request

#region Properties and Local values Section

        //Initialize localized string
        private void ApplyLocalize()
        {
            if (localize.localizeResource != null)
            {
                title = localize.localizeResource.Text(localize.titleID, title);
                message = localize.localizeResource.Text(localize.messageID, message);
            }
        }

        //Specify language and apply (update) localized string
        public override void ApplyLocalize(SystemLanguage language)
        {
            if (localize.localizeResource != null)
            {
                title = localize.localizeResource.Text(localize.titleID, language, title);
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
            if (checkOnStart)
                CheckPermission();
        }

        // Update is called once per frame
        //private void Update()
        //{

        //}

#pragma warning disable 0649    //oldGranted' is never assigned to, and will always have its default value `false'. (But it is used for Android Platform.)

        bool oldGranted;

        //Check for permission (using local value)
        public void CheckPermission()
        {
            if (string.IsNullOrEmpty(permission))
                return;

#if UNITY_EDITOR
            Debug.Log("PermissionCheckController.CheckPermission called.");
#elif UNITY_ANDROID
            bool granted = oldGranted = AndroidPlugin.CheckPermission(permission);

            if (!granted && requestWhenNotGranted)
            {
                AndroidPlugin.CheckPermissionAndRequest(permission, title, message, gameObject.name, "ReceiveResultPermission", style);
            }
            else
            {
                if (OnResult != null)
                {
                    OnResult.Invoke(permission, granted);
                }

                if (granted)
                {
                    if (OnGranted != null)
                        OnGranted.Invoke();
                }
                else
                {
                    if (OnDenied != null)
                        OnDenied.Invoke();
                }
            }
#endif
        }

        //Set permission string dynamically and check (current permission string will be overwritten)
        public void CheckPermission(string permission)
        {
            this.permission = permission;
            CheckPermission();
        }


        //Callback hander of 'CheckPermissionAndRequest()'
        private void ReceiveResultPermission(string result)
        {
            if (string.IsNullOrEmpty(result))
                return;

            bool granted = (result == "PERMISSION_GRANTED");

            if (OnResult != null)
            {
                OnResult.Invoke(permission, granted);
            }

            if (granted)
            {
                if (OnGranted != null)
                    OnGranted.Invoke();
            }
            else
            {
                if (OnDenied != null)
                    OnDenied.Invoke();
            }

            if (OnAllowed != null)
            {
                if (granted && oldGranted != granted)
                    OnAllowed.Invoke(permission);
            }
        }
    }
}
