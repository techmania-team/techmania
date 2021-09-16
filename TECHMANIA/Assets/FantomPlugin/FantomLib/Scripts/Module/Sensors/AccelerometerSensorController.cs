using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Accelerometer Sensor Controller
    /// http://fantom1x.blog130.fc2.com/blog-entry-294.html
    /// 
    ///(Sensor Type)
    /// https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_ACCELEROMETER
    ///(Sensor Values)
    /// https://developer.android.com/reference/android/hardware/SensorEvent.html#values
    ///(Sensor Delay)
    /// https://developer.android.com/reference/android/hardware/SensorManager.html#SENSOR_DELAY_FASTEST
    ///(Motion Sensors)
    /// https://developer.android.com/guide/topics/sensors/sensors_motion.html
    /// </summary>
    public class AccelerometerSensorController : SensorControllerBase
    {
        protected override SensorType sensorType {
            get { return SensorType.Accelerometer; }
        }

        //Callbacks
        [Serializable] public class AccelerometerSensorChangedHandler : UnityEvent<float, float, float> { }   //x, y, z [m/s^2]
        public AccelerometerSensorChangedHandler OnAccelerometerSensorChanged;



        //Callback handler for sensor values.
        protected override void ReceiveValues(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            base.ReceiveValues(json);

            if (OnAccelerometerSensorChanged != null)
                OnAccelerometerSensorChanged.Invoke(info.values[0], info.values[1], info.values[2]);    //x, y, z [m/s^2]
        }
    }
}
