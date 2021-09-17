using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Wifi Setting Controller
    ///･Open the system settings of Wifi.
    ///
    ///・Wifi のシステム設定画面を開く。
    /// </summary>
    public class WifiSettingController : MonoBehaviour
    {
        public bool ignoreReachableViaLocalAreaNetwork = true;  //Open setting only when wifi is not used.

        //Callbacks
        //Callback when settings closed.        //設定画面が閉じられたときのコールバック
        public UnityEvent OnClose;

        //Callback when error occurrence.       //エラー時のコールバック
        [Serializable] public class ErrorHandler : UnityEvent<string> { }   //error state message
        public ErrorHandler OnError;


        // Use this for initialization
        private void Start()
        {

        }

        // Update is called once per frame
        //private void Update()
        //{

        //}


        //Open the system settings of Wifi.     //Wifi のシステム設定画面を開く。
        public void Show()
        {
            if (ignoreReachableViaLocalAreaNetwork && Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork)
                return;

#if UNITY_EDITOR
            Debug.Log("WifiSettingController.Show called");
#elif UNITY_ANDROID
            AndroidPlugin.OpenWifiSettings(gameObject.name, "ReceiveResult");
#endif
        }

        //Callback handler when receive result
        private void ReceiveResult(string result)
        {
            if (result.StartsWith("CLOSED") || result.StartsWith("SUCCESS"))
            {
                if (OnClose != null)
                    OnClose.Invoke();
            }
            else
            {
                if (OnError != null)
                    OnError.Invoke(result);
            }
        }
    }
}
