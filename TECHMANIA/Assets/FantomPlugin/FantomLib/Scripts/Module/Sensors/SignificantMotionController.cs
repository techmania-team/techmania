using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Significant Motion Sensor Controller (API 18 or higher)
    /// http://fantom1x.blog130.fc2.com/blog-entry-294.html
    /// 
    ///･It triggers when an event occurs and then automatically disables itself.
    /// The sensor continues to operate while the device is asleep and will automatically wake the device to notify when significant motion is detected.
    ///(Sensor Type)
    /// https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_SIGNIFICANT_MOTION
    ///(Sensor Values)
    /// https://developer.android.com/reference/android/hardware/SensorEvent.html#values
    ///(Sensor Delay)
    /// https://developer.android.com/reference/android/hardware/SensorManager.html#SENSOR_DELAY_FASTEST
    ///(Motion Sensors)
    /// https://developer.android.com/guide/topics/sensors/sensors_motion.html
    /// </summary>
    public class SignificantMotionController : SensorControllerBase
    {
        protected override SensorType sensorType {
            get { return SensorType.SignificantMotion; }
        }

        //Callbacks
        public UnityEvent OnSignificantMotionSensorChanged;



        //Callback handler for sensor values.
        protected override void ReceiveValues(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            base.ReceiveValues(json);

            if (OnSignificantMotionSensorChanged != null)
                OnSignificantMotionSensorChanged.Invoke();    //triggers an event
        }
    }
}
