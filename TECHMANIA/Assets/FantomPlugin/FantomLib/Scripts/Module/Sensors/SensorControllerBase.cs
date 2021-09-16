using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Sensor Controller base class (for making controllers for each sensor)
    /// http://fantom1x.blog130.fc2.com/blog-entry-294.html
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
    public abstract class SensorControllerBase : MonoBehaviour
    {
#pragma warning disable 0414    //The private field is assigned but its value is never used. (*In fact, it uses on the Android platform.)

        //(*) Required override sensor type.
        protected virtual SensorType sensorType {
            get { return SensorType.None; }
        }

        //Inspector Settings
        [SerializeField] private SensorDelay sensorDelay = SensorDelay.Normal;

        //Inspector settings
        public bool startListeningOnEnable = false;     //Automatically set listener with 'OnEnable()' (Always removed in 'OnDisable()').    //OnEnable() でリスナーを自動で登録する（OnDisable() では常に解除する）。

        //Callbacks
        [Serializable] public class SensorChangedHandler : UnityEvent<int, float[]> { }   //SensorType, Values (Common to all sensors)
        public SensorChangedHandler OnSensorChanged;

        [Serializable] public class ErrorHandler : UnityEvent<string> { }       //error state message
        public ErrorHandler OnError;

#region Properties and Local values Section
        //Properties

        //Since it was made possible to give permission with v1.17, it changed to real time acquisition.
        //(*) However, even if you grant permissions at a later time, it seems that the sensor itself will not run without restarting the application.
        //
        //v1.17 でパーミッションを付与できるようにしたため、リアルタイム取得に変更。
        //※ただし、後からパーミッションを付与しても、アプリを再起動しないとセンサー自体は稼働しないらしい。

        public bool IsSupportedSensor {
            get {
#if UNITY_EDITOR
                return true;       //For Editor (* You can rewrite it as you like.)
#elif UNITY_ANDROID
                return AndroidPlugin.IsSupportedSensor(sensorType);
#else
                return false;
#endif
            }
        }

#endregion

        // Use this for initialization
        protected void Start()
        {
            if (!IsSupportedSensor)
            {
                if (OnError != null)
                    OnError.Invoke("Not supported: " + sensorType.ToString());
            }
        }

        protected void OnEnable()
        {
            if (startListeningOnEnable)
                StartListening();
        }

        protected void OnDisable()
        {
            StopListening();
        }

        protected void OnDestroy()
        {
            StopListening();
        }

        protected void OnApplicationQuit()
        {
#if UNITY_EDITOR
            Debug.Log("AndroidPlugin.ReleaseSensors called.");
#elif UNITY_ANDROID
            AndroidPlugin.ReleaseSensors();
#endif
        }


        // Update is called once per frame
        //protected void Update()
        //{

        //}


        //Set listener for sensor values acquisition.
        //センサーの値取得のリスニングを開始する
        public virtual void StartListening()
        {
            if (!IsSupportedSensor)
                return;
#if UNITY_EDITOR
            Debug.Log(sensorType.ToString() + "Controller.StartListening called");
#elif UNITY_ANDROID
            AndroidPlugin.SetSensorListener(sensorType, sensorDelay, gameObject.name, "ReceiveValues");
#endif
        }

        //Remove (release) listener for sensor values acquisition.
        //センサーの値取得のリスニングを停止（解放）する
        public virtual void StopListening()
        {
            if (!IsSupportedSensor)
                return;
#if UNITY_EDITOR
            Debug.Log(sensorType.ToString() + "Controller.StopListening called");
#elif UNITY_ANDROID
            AndroidPlugin.RemoveSensorListener(sensorType);
#endif
        }

        protected SensorInfo info;

        //Callback handler for sensor values.
        protected virtual void ReceiveValues(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            info = JsonUtility.FromJson<SensorInfo>(json);

            if (OnSensorChanged != null)
                OnSensorChanged.Invoke(info.type, info.values);
        }
    }
}
