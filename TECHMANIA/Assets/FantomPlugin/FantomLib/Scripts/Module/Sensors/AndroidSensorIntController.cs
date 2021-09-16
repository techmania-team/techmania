using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Android Sensor Controller (int)
    /// http://fantom1x.blog130.fc2.com/blog-entry-294.html
    /// 
    ///･It is the same as 'AndroidSensorController' just by setting 'SensorType' to 'int' type(> 0).
    /// For testing unknown ID etc.
    /// 
    ///・’SensorType’を'int'型(> 0)にしただけで、'AndroidSensorController'と同じものである。未知のID等のテスト用。
    /// 
    ///(Sensor Type)
    /// https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_ACCELEROMETER
    ///(Sensor Values)
    /// https://developer.android.com/reference/android/hardware/SensorEvent.html#values
    ///(Sensor Delay)
    /// https://developer.android.com/reference/android/hardware/SensorManager.html#SENSOR_DELAY_FASTEST
    ///(Sensors Overview)
    /// https://developer.android.com/guide/topics/sensors/sensors_overview.html
    /// </summary>
    public class AndroidSensorIntController : MonoBehaviour
    {
#pragma warning disable 0414    //The private field is assigned but its value is never used. (*In fact, it uses on the Android platform.)

        //Inspector Settings
        [SerializeField] private int sensorType = 0;
        [SerializeField] private SensorDelay sensorDelay = SensorDelay.Normal;

        //Inspector settings
        public bool startListeningOnEnable = false;     //Automatically set listener with 'OnEnable()' (Always removed in 'OnDisable()').    //OnEnable() でリスナーを自動で登録する（OnDisable() では常に解除する）。

        //Callbacks
        [Serializable] public class SensorChangedHandler : UnityEvent<int, float[]> { }   //SensorType, Values
        public SensorChangedHandler OnSensorChanged;

        [Serializable] public class ErrorHandler : UnityEvent<string> { }       //error state message
        public ErrorHandler OnError;

#region Properties and Local values Section

        //Properties
        private bool isSupportedSensor = false;     //Cached supported Sensor.
        private bool isSupportedChecked = false;    //Already checked.

        public bool IsSupportedSensor {
            get {
                if (!isSupportedChecked)
                {
#if UNITY_EDITOR
                    isSupportedSensor = true;       //For Editor
#elif UNITY_ANDROID
                    isSupportedSensor = AndroidPlugin.IsSupportedSensor(sensorType);
#endif
                    isSupportedChecked = true;
                }
                return isSupportedSensor;
            }
        }

#endregion

        // Use this for initialization
        private void Start()
        {
            if (!IsSupportedSensor)
            {
                if (OnError != null)
                    OnError.Invoke("Not supported: " + sensorType.ToString());
            }
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
#if UNITY_EDITOR
            Debug.Log("AndroidPlugin.ReleaseSensors called.");
#elif UNITY_ANDROID
            AndroidPlugin.ReleaseSensors();
#endif
        }


        // Update is called once per frame
        //private void Update()
        //{

        //}


        //Set listener for sensor values acquisition.
        //センサーの値取得のリスニングを開始する
        public void StartListening()
        {
            if (!IsSupportedSensor)
                return;
#if UNITY_EDITOR
            Debug.Log("AndroidSensorIntController.StartListening called");
#elif UNITY_ANDROID
            AndroidPlugin.SetSensorListener(sensorType, (int)sensorDelay, gameObject.name, "ReceiveValues");
#endif
        }

        //Remove (release) listener for sensor values acquisition.
        //センサーの値取得のリスニングを停止（解放）する
        public void StopListening()
        {
            if (!IsSupportedSensor)
                return;
#if UNITY_EDITOR
            Debug.Log("AndroidSensorIntController.StopListening called");
#elif UNITY_ANDROID
            AndroidPlugin.RemoveSensorListener(sensorType);
#endif
        }


        //Callback handler for sensor values.
        private void ReceiveValues(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            SensorInfo info = JsonUtility.FromJson<SensorInfo>(json);

            if (OnSensorChanged != null)
                OnSensorChanged.Invoke(info.type, info.values);
        }
    }
}
