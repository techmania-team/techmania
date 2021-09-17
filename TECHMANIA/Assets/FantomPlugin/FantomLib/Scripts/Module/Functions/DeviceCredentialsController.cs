using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Device Credentials Controller (API Level 21 or higher)
    /// 
    ///(*) The authentication method depends on user's setting (fingerprint, pattern, PIN, password, etc.).
    ///(*) Only 'OnSuccess' callback is registered by method just before opening Device Credentials (Since only the public method can be used in the inspector, which is not good security).
    ///    It is also deleted when it is closed (for security, prevent malfunction reasons).
    ///(*) Authentication works only in a unique execution (Judge only one session. Multiple (duplication) is not accepted).
    ///(*) The session timeout (invalid) at a certain time (SESSION_TIMEOUT). This session timeout is different from Android's authentication system (timeout in this script.).
    ///(*) message (description character string) may not be reflected by the device (because the system rewrites depending on the state).
    ///(*) Title and message are basically empty (because messages are set automatically by system language).
    /// 
    /// 
    ///※認証方法はユーザーの設定による（指紋・パターン・PIN・パスワード等）。
    ///※認証画面を開く直前に、「OnSuccess」コールバックのみメソッドによる登録を必要としている（インペクタでは public メソッドしか使えず、セキュリティ的に良くないため）。
    ///  また閉じられたときに必ず削除される（セキュリティや誤動作を防ぐため）。
    ///※認証はユニークな動作となる（唯一のセッションのみ判定する。複数（重複）は受け付けない）。
    ///※一定時間で（SESSION_TIMEOUT）でセッションがタイムアウト（無効）になる。このセッションがタイムアウトは Andrdoiの認証システムのものとは別のものである（このスクリプト内でのタイムアウト）。
    ///※message（メッセージ文字列）はデバイスによって反映されない場合がある（状況によってシステムが書き換えるため）。
    ///※title, message は基本的に空で良い（システム言語によって自動でメッセージが設定されるため）。
    /// </summary>
    public class DeviceCredentialsController : MonoBehaviour
    {
        //Inspector Settings
        public string title = "";               //Empty is system default title (*empty recommended)
        [Multiline] public string message = ""; //Empty is system default message (*empty recommended)

        //Callbacks
        //(*) When registering a method in UnityEvent with the inspector, it is used in 'public' scope and it is not good security, so it changed to other scope.
        //※インペクタで UnityEvent にメソッドを登録する場合、public スコープでの利用となり、セキュリティ的に良くないので、スコープを変更している。
        private UnityEvent OnSuccess = new UnityEvent();   //ok (authorized)

        public UnityEvent OnFailure;    //unauthorized, cancel

        [Serializable] public class ErrorHandler : UnityEvent<string> { }   //error status
        public ErrorHandler OnError;

#region Properties and Local values Section

        const float SESSION_TIMEOUT = 5f * 60;      //Time to automatically cancel session [second]
        static long resetTimeTicks;                 //Force reset time (tick = 1/10000000[sec])
        static volatile string sessionID;           //Generated sessionID each time (also a unique execution flag.)
        static HashSet<UnityAction> check = new HashSet<UnityAction>();     //Duplication check

        //Register the callback of 'OnSuccess'.
        //(*) When the authentication screen is closed, it is always deleted.
        //※認証画面が閉じられたら、必ず削除される。
        internal void SetOnSuccess(UnityAction OnSuccess)
        {
            if (OnSuccess != null && !check.Contains(OnSuccess))
            {
                check.Add(OnSuccess);
                this.OnSuccess.AddListener(OnSuccess);
            }
        }

        //Unregister all callbacks of 'OnSuccess'.
        //'OnSuccuss'コールバックを全て削除する。
        internal void RemoveOnSuccessAll()
        {
            OnSuccess.RemoveAllListeners();
            check.Clear();
        }

        //Set sessionID and timeout time (resetTimeTicks)
        //セッションIDとタイムアウト時刻をセットする
        private void StartSession()
        {
#if UNITY_ANDROID
            sessionID = AndroidPlugin.GenerateSessionID();
#endif
            resetTimeTicks = (long)(SESSION_TIMEOUT * TimeSpan.TicksPerSecond) + DateTime.Now.Ticks;
        }

        //Delete sessionID and 'OnSuccuss' callback.
        //セッションIDと'OnSuccuss'コールバックを削除する。
        private void ResetSession()
        {
            sessionID = "";
            resetTimeTicks = 0;
            RemoveOnSuccessAll();
        }

        //Check session timeout.
        private bool CheckTimeout()
        {
            return (resetTimeTicks < DateTime.Now.Ticks);
        }

#endregion

        // Use this for initialization
        private void Start()
        {

        }

        // Update is called once per frame
        //private void Update()
        //{

        //}


        
        //Show confirm device credentials screen
        //(*) It is necessary to register 'OnSuccess' with method in advance.
        //(*) The support status is always checked (because the system setting may be changed dynamically)
        //
        //※事前にメソッドで「OnSuccess」を登録する必要がある。
        //※サポート状態は常にチェックされる（動的にシステム設定が変更される可能性があるため）
        internal void Show()
        {
            if (!string.IsNullOrEmpty(sessionID) && !CheckTimeout())
                return;
#if UNITY_EDITOR
            Debug.Log(name + ".Show called");
#elif UNITY_ANDROID
            StartSession();
            AndroidPlugin.ShowDeviceCredentials(gameObject.name, "ReceiveResult", sessionID, title, message);
#endif
        }

        //Set 'OnSuccess' callback and show (overload)
        //「OnSuccess」を登録して、認証画面を開く。
        internal void Show(UnityAction OnSuccess)
        {
            SetOnSuccess(OnSuccess);
            Show();
        }


        //Receive the result
        private void ReceiveResult(string json)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(sessionID)
                || CheckTimeout())
            {
                if (OnError != null)
                    OnError.Invoke("Invalid session or timeout.");

                ResetSession();
                return;
            }

            CredentialInfo info = JsonUtility.FromJson<CredentialInfo>(json);
            if (info.sessionID == sessionID)
            {
                switch (info.status)
                {
                    case "SUCCESS_CREDENTIALS":         //ok
                        if (OnSuccess != null)
                            OnSuccess.Invoke();
                        break;
                    case "UNAUTHORIZED_CREDENTIALS":    //including cancel
                        if (OnFailure != null)
                            OnFailure.Invoke();
                        break;
                    default:
                        if (OnError != null)
                            OnError.Invoke(info.status); //not supported, secure off
                        break;
                }
            }

            //Unregister each time    //毎回削除する
            ResetSession();
        }
    }
}
