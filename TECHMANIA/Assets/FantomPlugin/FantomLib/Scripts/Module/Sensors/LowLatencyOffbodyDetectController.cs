using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Low Latency Offbody Detect Sensor Controller (API 26 or higher)
    /// http://fantom1x.blog130.fc2.com/blog-entry-294.html
    /// 
    ///(Sensor Type)
    /// https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_LOW_LATENCY_OFFBODY_DETECT
    ///(Sensor Values)
    /// https://developer.android.com/reference/android/hardware/SensorEvent.html#values
    ///(Sensor Delay)
    /// https://developer.android.com/reference/android/hardware/SensorManager.html#SENSOR_DELAY_FASTEST
    /// </summary>
    public class LowLatencyOffbodyDetectController : SensorControllerBase
    {
        protected override SensorType sensorType {
            get { return SensorType.LowLatencyOffbodyDetect; }
        }

        //Callbacks
        [Serializable] public class LowLatencyOffbodyDetectSensorChangedHandler : UnityEvent<bool> { }   //[false (device is off-body) or true (device is on-body)]
        public LowLatencyOffbodyDetectSensorChangedHandler OnLowLatencyOffbodyDetectSensorChanged;



        //Callback handler for sensor values.
        protected override void ReceiveValues(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            base.ReceiveValues(json);

            if (OnLowLatencyOffbodyDetectSensorChanged != null)
                OnLowLatencyOffbodyDetectSensorChanged.Invoke(info.values[0] != 0);    //[0=false (device is off-body) or 1=true (device is on-body)]
        }
    }
}
