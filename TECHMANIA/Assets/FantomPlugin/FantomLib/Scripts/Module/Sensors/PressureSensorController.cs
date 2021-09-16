using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FantomLib
{
    /// <summary>
    /// Pressure Sensor Controller
    /// http://fantom1x.blog130.fc2.com/blog-entry-294.html
    /// 
    ///(Sensor Type)
    /// https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_PRESSURE
    ///(Sensor Values)
    /// https://developer.android.com/reference/android/hardware/SensorEvent.html#values
    ///(Sensor Delay)
    /// https://developer.android.com/reference/android/hardware/SensorManager.html#SENSOR_DELAY_FASTEST
    ///(Environment Sensors)
    /// https://developer.android.com/guide/topics/sensors/sensors_environment.html
    /// </summary>
    public class PressureSensorController : SensorControllerBase
    {
        protected override SensorType sensorType {
            get { return SensorType.Pressure; }
        }

        //Callbacks
        [Serializable] public class PressureSensorChangedHandler : UnityEvent<float> { }   //[hPa (millibar)]
        public PressureSensorChangedHandler OnPressureSensorChanged;



        //Callback handler for sensor values.
        protected override void ReceiveValues(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            base.ReceiveValues(json);

            if (OnPressureSensorChanged != null)
                OnPressureSensorChanged.Invoke(info.values[0]);    //[hPa (millibar)]
        }
    }
}
