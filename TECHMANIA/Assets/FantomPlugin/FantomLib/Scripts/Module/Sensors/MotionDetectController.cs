using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Motion Detect Sensor Controller (API 24 or higher)
    /// http://fantom1x.blog130.fc2.com/blog-entry-294.html
    /// 
    ///･A TYPE_MOTION_DETECT event is produced if the device has been in motion for at least 5 seconds with a maximal latency of 5 additional seconds.
    /// ie: it may take up anywhere from 5 to 10 seconds afte the device has been at rest to trigger this event. The only allowed value is 1.0.
    ///(Sensor Type)
    /// https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_MOTION_DETECT
    ///(Sensor Values)
    /// https://developer.android.com/reference/android/hardware/SensorEvent.html#values
    ///(Sensor Delay)
    /// https://developer.android.com/reference/android/hardware/SensorManager.html#SENSOR_DELAY_FASTEST
    /// </summary>
    public class MotionDetectController : SensorControllerBase
    {
        protected override SensorType sensorType {
            get { return SensorType.MotionDetect; }
        }

        //Callbacks
        public UnityEvent OnMotionDetectSensorChanged;



        //Callback handler for sensor values.
        protected override void ReceiveValues(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            base.ReceiveValues(json);

            if (OnMotionDetectSensorChanged != null)
                OnMotionDetectSensorChanged.Invoke();    //triggers an event
        }
    }
}

