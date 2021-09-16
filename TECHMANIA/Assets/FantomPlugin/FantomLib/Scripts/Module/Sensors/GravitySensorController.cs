using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Gravity Sensor Controller
    /// http://fantom1x.blog130.fc2.com/blog-entry-294.html
    /// 
    ///(Sensor Type)
    /// https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_GRAVITY
    ///(Sensor Values)
    /// https://developer.android.com/reference/android/hardware/SensorEvent.html#values
    ///(Sensor Delay)
    /// https://developer.android.com/reference/android/hardware/SensorManager.html#SENSOR_DELAY_FASTEST
    ///(Motion Sensors)
    /// https://developer.android.com/guide/topics/sensors/sensors_motion.html
    /// </summary>
    public class GravitySensorController : SensorControllerBase
    {
        protected override SensorType sensorType {
            get { return SensorType.Gravity; }
        }

        //Callbacks
        [Serializable] public class GravitySensorChangedHandler : UnityEvent<float> { }   //[m/s^2]
        public GravitySensorChangedHandler OnGravitySensorChanged;



        //Callback handler for sensor values.
        protected override void ReceiveValues(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            base.ReceiveValues(json);

            if (OnGravitySensorChanged != null)
                OnGravitySensorChanged.Invoke(info.values[0]); //[m/s^2]
        }
    }
}
