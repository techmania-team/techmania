using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Heart Beat Sensor Controller (API 24 or higher)
    /// http://fantom1x.blog130.fc2.com/blog-entry-294.html
    /// 
    ///･A sensor of this type returns an event everytime a hear beat peak is detected.
    /// Peak here ideally corresponds to the positive peak in the QRS complex of an ECG signal.
    ///･A confidence value of 0.0 indicates complete uncertainty - that a peak is as likely to be at the indicated timestamp as anywhere else.
    /// A confidence value of 1.0 indicates complete certainly - that a peak is completely unlikely to be anywhere else on the QRS complex.
    ///(Sensor Type)
    /// https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_HEART_BEAT
    ///(Sensor Values)
    /// https://developer.android.com/reference/android/hardware/SensorEvent.html#values
    ///(Sensor Delay)
    /// https://developer.android.com/reference/android/hardware/SensorManager.html#SENSOR_DELAY_FASTEST
    /// </summary>
    public class HeartBeatController : SensorControllerBase
    {
        protected override SensorType sensorType {
            get { return SensorType.HeartBeat; }
        }

        //Callbacks
        [Serializable] public class HeartBeatSensorChangedHandler : UnityEvent<float> { }   //[confidence=0~1]
        public HeartBeatSensorChangedHandler OnHeartBeatSensorChanged;



        //Callback handler for sensor values.
        protected override void ReceiveValues(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            base.ReceiveValues(json);

            if (OnHeartBeatSensorChanged != null)
                OnHeartBeatSensorChanged.Invoke(info.values[0]);    //[confidence=0~1]
        }
    }
}
