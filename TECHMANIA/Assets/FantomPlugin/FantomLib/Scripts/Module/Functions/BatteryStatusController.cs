using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Battery Status Controller
    ///
    ///·Get battery status in detail (Stored in the BatteryInfo class).
    ///·Status information is sent when there is a change on the Android system.
    ///·BatteryInfo.status is the same as UnityEngine.BatteryStatus (https://docs.unity3d.com/en/current/ScriptReference/BatteryStatus.html).
    /// 'status' represents the state of charge of the device (string type). 
    /// https://developer.android.com/reference/android/os/BatteryManager.html#BATTERY_STATUS_CHARGING
    /// 'Health' represents the state of the battery condition (string type).
    /// https://developer.android.com/reference/android/os/BatteryManager.html#BATTERY_HEALTH_COLD
    ///(*)It is always stopped by 'OnDisable()', 'OnDestroy()', 'OnApplicationQuit()' (Unreleasing listening causes memory leak, so release when not using it). (*) Also released by 'AndroidPlugin.Release()'.
    ///(*)Pause/Resume of the application requires no operation (automatically stop/start).
    ///(*)Required 'FullPluginOnUnityPlayerActivity' in 'AndroidManifest.xml' (rename and use 'AndroidManifest-FullPlugin~.xml').
    ///
    ///・バッテリーのステータスを詳細に取得する（BatteryInfo クラスに格納される）。
    ///・ステータス情報は Androidシステム上で変化のあったときに送られる。
    ///・BatteryInfo.status は UnityEngine.BatteryStatus（https://docs.unity3d.com/ja/current/ScriptReference/BatteryStatus.html） と同じものになる。
    ///「health」とはバッテリーのコンディションを表す（文字列型）
    ///  GOOD : 良好
    ///  OVERHEAT : オーバーヒート
    ///  DEAD : 故障
    ///  OVER_VOLTAGE : 過電圧
    ///  UNSPECIFIED_FAILURE : 特定失敗
    ///  COLD : 低温
    /// https://developer.android.com/reference/android/os/BatteryManager.html#BATTERY_STATUS_CHARGING
    ///「status」とは充電状態を表す（文字列型）
    ///  CHARGING : 充電中
    ///  DISCHARGING : 充電を止めた
    ///  NOT_CHARGING : 充電中でない
    ///  FULL : 満タン
    /// https://developer.android.com/reference/android/os/BatteryManager.html#BATTERY_HEALTH_COLD
    ///※OnDisable(), OnDestroy(), OnApplicationQuit() では必ず停止される（リスニングの未解放はメモリリークの原因となるため、利用しないときは常に解放する）。※AndroidPlugin.Release() でも解放される。
    ///※アプリケーションの Pause(一時停止)/Resume(復帰)では操作不要（自動的に停止・再開される）。
    ///※「FullPluginOnUnityPlayerActivity」を「AndroidManifest.xml」で使う必要がある（「AndroidManifest-FullPlugin～.xml」をリネームして使う）。
    /// </summary>
    public class BatteryStatusController : MonoBehaviour
    {
        //Inspector settings
        public bool startListeningOnEnable = false;     //Automatically start listening with 'OnEnable()' (Always stopped in 'OnDisable()').    //OnEnable() でリスニングを自動で開始する（OnDisable() では常に停止する）。

        //Callbacks
        [Serializable] public class StatusHandler : UnityEvent<BatteryInfo> { }
        public StatusHandler OnStatus;


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

        
        //Start listening for battery information acquisition.
        //バッテリーの情報取得のリスニングを開始する
        public void StartListening()
        {
            if (OnStatus == null)
                return;
#if UNITY_EDITOR
            Debug.Log("BatteryStatusController.StartListening called");
#elif UNITY_ANDROID
            AndroidPlugin.StartBatteryStatusListening(gameObject.name, "ReceiveStatus");
#endif
        }

        //Stop (release) listening for battery information acquisition.
        //バッテリーの情報取得のリスニングを停止（解放）する
        public void StopListening()
        {
            if (OnStatus == null)
                return;
#if UNITY_EDITOR
            Debug.Log("BatteryStatusController.StopListening called");
#elif UNITY_ANDROID
            AndroidPlugin.StopBatteryStatusListening();
#endif
        }


        //Callback handler for battery information.
        private void ReceiveStatus(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            BatteryInfo info = JsonUtility.FromJson<BatteryInfo>(json);
            if (OnStatus != null)
                OnStatus.Invoke(info);
        }
    }
}
