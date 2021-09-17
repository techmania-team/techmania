using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Light Sensor Controller
    /// http://fantom1x.blog130.fc2.com/blog-entry-294.html
    /// 
    ///(Sensor Type)
    /// https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_LIGHT
    ///(Sensor Values)
    /// https://developer.android.com/reference/android/hardware/SensorEvent.html#values
    ///(Sensor Delay)
    /// https://developer.android.com/reference/android/hardware/SensorManager.html#SENSOR_DELAY_FASTEST
    ///(Environment Sensors)
    /// https://developer.android.com/guide/topics/sensors/sensors_environment.html
    /// </summary>
    public class LightSensorController : SensorControllerBase
    {
        protected override SensorType sensorType {
            get { return SensorType.Light; }
        }

        //Callbacks
        [Serializable] public class LightSensorChangedHandler : UnityEvent<float> { }   //[lux]
        public LightSensorChangedHandler OnLightSensorChanged;



        //Callback handler for sensor values.
        protected override void ReceiveValues(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            base.ReceiveValues(json);

            if (OnLightSensorChanged != null)
                OnLightSensorChanged.Invoke(info.values[0]);    //[lux]
        }
    }
}
