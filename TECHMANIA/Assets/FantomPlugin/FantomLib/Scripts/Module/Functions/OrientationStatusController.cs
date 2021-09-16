using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Orientation Status Controller
    /// 
    ///·Get the configuration change when the Android device rotates and call back.
    ///·Status information is sent only when there is a change on the Android system.
    ///·The status is as follows (※ Basically it can be considered "PORTRAIT" or "LANDSCAPE"). 
    ///  PORTRAIT : The screen is oriented vertically
    ///  LANDSCAPE : The screen is oriented horizontally
    ///  UNDEFINED : Undefined (* Normally it is not.)
    ///  UNKNOWN : Unknown (* This also applies to "SQUARE" case, but it is not normal.)
    ///(*)"ORIENTATION_SQUARE" is scheduled to be deprecated in API 16, so it will not be acquired (-> it will become "UNKNOWN", but it should not be present in the current device).
    ///  https://developer.android.com/reference/android/content/res/Configuration.html?hl=ja#ORIENTATION_LANDSCAPE
    ///(*)The following attribute is required for 'activity' tag of 'AndroidManifest.xml' (* In the case of Unity application, it is added by default).
    ///  'android:configChanges="orientation|screenSize"'
    ///·Normally, for applications that rotate the screen in four directions, add the following attributes to the 'activity' tag of 'AndroidManifest.xml'.
    ///  'android:screenOrientation="sensor"'
    ///  https://developer.android.com/guide/topics/manifest/activity-element.html
    ///(*)Required 'FullPluginOnUnityPlayerActivity' in 'AndroidManifest.xml' (* Included by default in 'AndroidManifest-FullPlugin_Sensor.xml').
    /// 
    /// 
    ///・Android端末が回転したときのコンフィグ変化を取得し、コールバックする。
    ///・ステータス情報は Androidシステム上で変化のあったときのみ送られる。
    ///・ステータスは以下のようになる（※基本的には "PORTRAIT" または "LANDSCAPE" と考えて良い）。
    ///  PORTRAIT : 画面が縦向き
    ///  LANDSCAPE : 画面が横向き
    ///  UNDEFINED : 未定義 (※普通はない)
    ///  UNKNOWN : 不明（※"SQUARE" の場合もこれになるが、普通はない）
    ///※"ORIENTATION_SQUARE" は API 16 で廃止予定となっているため取得しない（→ UNKNOWN となるが、現在の端末には無いはず）。
    ///  https://developer.android.com/reference/android/content/res/Configuration.html?hl=ja#ORIENTATION_LANDSCAPE
    ///・画面回転を取得するには「AndroidManifest.xml」の「activity」タグに以下の属性が必要である（※Unity の場合、デフォルトで追加されている）。
    ///  'android:configChanges="orientation|screenSize"'
    ///・通常、４方向に画面回転するアプリには「AndroidManifest.xml」の「activity」タグに以下の属性を付ける。
    ///  'android:screenOrientation="sensor"'
    ///  https://developer.android.com/guide/topics/manifest/activity-element.html
    ///※「FullPluginOnUnityPlayerActivity」を「AndroidManifest.xml」で使う必要がある（※「AndroidManifest-FullPlugin_Sensor.xml」にはデフォルトで含まれている）。
    /// </summary>
    public class OrientationStatusController : MonoBehaviour
    {
        //Inspector settings
        public bool startListeningOnEnable = false;     //Automatically set listener with 'OnEnable()' (Always removed in 'OnDisable()').    //OnEnable() でリスナーを自動で登録する（OnDisable() では常に解除する）。

        //Callbacks
        [Serializable] public class StatusChangedHandler : UnityEvent<string> { }   //orientation status
        public StatusChangedHandler OnStatusChanged;


        // Use this for initialization
        private void Start()
        {

        }

        private void OnEnable()
        {
            if (startListeningOnEnable)
                StartListening();
        }

        private void OnDisable()
        {
            StopListening();
        }

        private void OnDestroy()
        {
            StopListening();
        }

        private void OnApplicationQuit()
        {
            StopListening();
        }


        // Update is called once per frame
        //private void Update()
        //{

        //}


        //Set listener for orientation information acquisition.
        //画面回転の情報取得のリスニングを開始する
        public void StartListening()
        {
#if UNITY_EDITOR
            Debug.Log("OrientationStatusController.StartListening called");
#elif UNITY_ANDROID
            AndroidPlugin.SetOrientationChangeListener(gameObject.name, "ReceiveStatus");
#endif
        }

        //Remove (release) listener for orientation information acquisition.
        //画面回転の情報取得のリスニングを停止（解放）する
        public void StopListening()
        {
#if UNITY_EDITOR
            Debug.Log("OrientationStatusController.StopListening called");
#elif UNITY_ANDROID
            AndroidPlugin.RemoveOrientationChangeListener();
#endif
        }


        //Callback handler for battery information.
        private void ReceiveStatus(string status)
        {
            if (string.IsNullOrEmpty(status))
                return;

            if (OnStatusChanged != null)
                OnStatusChanged.Invoke(status);
        }
    }
}
