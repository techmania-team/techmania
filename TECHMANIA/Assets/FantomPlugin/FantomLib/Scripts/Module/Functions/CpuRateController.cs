using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Listening for CPU Rate information
    /// 
    /// CPU使用率の情報取得のリスニング
    /// </summary>
    public class CpuRateController : MonoBehaviour {

        //Inspector settings
        public bool startListeningOnEnable = false;     //Automatically start listening with 'OnEnable()' (Always stopped in 'OnDisable()').    //OnEnable() でリスニングを自動で開始する（OnDisable() では常に停止する）。
        
        [Range(1, 600)] public float interval = 2f;     //Measurement interval [sec] (* The shorter the load the higher)    //計測間隔 [秒]（※短いほど負荷が高い）

        //Callbacks
        [Serializable] public class StatusHandler : UnityEvent<CpuRateInfo[]> { }   //CPU 0 ~ n
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

        
        //Start listening for CPU Rate information acquisition.
        //CPU使用率の情報取得のリスニングを開始する
        public void StartListening()
        {
            if (OnStatus == null)
                return;
#if UNITY_EDITOR
            Debug.Log("CpuRateController.StartListening called");
#elif UNITY_ANDROID
            AndroidPlugin.StartCpuRateListening(gameObject.name, "ReceiveStatus", interval);
#endif
        }

        //Stop (release) listening for CPU Rate information acquisition.
        //CPU使用率の情報取得のリスニングを停止（解放）する
        public void StopListening()
        {
            if (OnStatus == null)
                return;
#if UNITY_EDITOR
            Debug.Log("CpuRateController.StopListening called");
#elif UNITY_ANDROID
            AndroidPlugin.StopCpuRateListening();
#endif
        }


        //Callback handler for CPU Rate information.
        private void ReceiveStatus(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            string[] items = json.Split('\n');
            CpuRateInfo[] infos = new CpuRateInfo[items.Length];
            
            for (int i = 0; i < infos.Length; i++)
            {
                infos[i] = JsonUtility.FromJson<CpuRateInfo>(items[i]);
            }

            if (OnStatus != null)
                OnStatus.Invoke(infos);
        }
    }
}
