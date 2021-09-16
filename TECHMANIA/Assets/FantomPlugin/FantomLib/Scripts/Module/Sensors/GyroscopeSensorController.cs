using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Gyroscope Sensor Controller
    /// http://fantom1x.blog130.fc2.com/blog-entry-294.html
    /// 
    ///(Sensor Type)
    /// https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_GYROSCOPE
    ///(Sensor Values)
    /// https://developer.android.com/reference/android/hardware/SensorEvent.html#values
    ///(Sensor Delay)
    /// https://developer.android.com/reference/android/hardware/SensorManager.html#SENSOR_DELAY_FASTEST
    ///(Motion Sensors)
    /// https://developer.android.com/guide/topics/sensors/sensors_motion.html
    /// </summary>
    public class GyroscopeSensorController : SensorControllerBase
    {
        protected override SensorType sensorType {
            get { return SensorType.Gyroscope; }
        }

        //Callbacks
        [Serializable] public class GyroscopeSensorChangedHandler : UnityEvent<float, float, float> { }   //x, y, z [rad/s]
        public GyroscopeSensorChangedHandler OnGyroscopeSensorChanged;



        //Callback handler for sensor values.
        protected override void ReceiveValues(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            base.ReceiveValues(json);

            if (OnGyroscopeSensorChanged != null)
                OnGyroscopeSensorChanged.Invoke(info.values[0], info.values[1], info.values[2]);    //[rad/s]
        }
    }
}
