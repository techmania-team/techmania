using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Bluetooth Setting Controller
    ///･Show the dialog that request to enable bluetooth.
    ///
    ///・Bluetooth の接続要求ダイアログを表示する。
    /// </summary>
    public class BluetoothSettingController : MonoBehaviour
    {
        //Callbacks
        //Callback when receive result.         //結果受信時のコールバック
        [Serializable] public class ResultHandler : UnityEvent<bool> { }    //isOn
        public ResultHandler OnResult;

        //Callback when error occurrence.       //エラー時のコールバック
        [Serializable] public class ErrorHandler : UnityEvent<string> { }   //error state message
        public ErrorHandler OnError;

#region Properties and Local values Section

        //Whether necessary permissions are granted.
        public bool IsPermissionGranted {
            get {
#if UNITY_EDITOR
                return true;    //For Editor (* You can rewrite it as you like.)
#elif UNITY_ANDROID
                return AndroidPlugin.CheckPermission("android.permission.BLUETOOTH");
#else
                return false;
#endif
            }
        }

#endregion

        // Use this for initialization
        private void Start()
        {
            if (!IsPermissionGranted)
            {
                if (OnError != null)
                    OnError.Invoke("Permission denied: BLUETOOTH");
            }
        }

        // Update is called once per frame
        //private void Update()
        //{

        //}


        //Request to enable Bluetooth
        public void StartRequest()
        {
            if (!IsPermissionGranted)
                return;
#if UNITY_EDITOR
            Debug.Log("BluetoothSettingController.StartRequest called");
#elif UNITY_ANDROID
            AndroidPlugin.StartBluetoothRequestEnable(gameObject.name, "ReceiveResult");
#endif
        }


        //Callback handler when receive result
        private void ReceiveResult(string result)
        {
            if (result.StartsWith("SUCCESS"))
            {
                if (OnResult != null)
                    OnResult.Invoke(true);
            }
            else if (result.StartsWith("CANCEL"))
            {
                if (OnResult != null)
                    OnResult.Invoke(false);
            }
            else
            {
                if (OnError != null)
                    OnError.Invoke(result);
            }
        }
    }
}
