using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Step Detector Sensor Controller (API 19 or higher)
    /// http://fantom1x.blog130.fc2.com/blog-entry-294.html
    /// 
    /// Note: A sensor of this type triggers an event each time a step is taken by the user.
    /// The only allowed value to return is 1.0 and an event is generated for each step.
    ///(Sensor Type)
    /// https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_STEP_DETECTOR
    ///(Sensor Values)
    /// https://developer.android.com/reference/android/hardware/SensorEvent.html#values
    ///(Sensor Delay)
    /// https://developer.android.com/reference/android/hardware/SensorManager.html#SENSOR_DELAY_FASTEST
    ///(Motion Sensors)
    /// https://developer.android.com/guide/topics/sensors/sensors_motion.html
    /// </summary>
    public class StepDetectorSensorController : SensorControllerBase
    {
        protected override SensorType sensorType {
            get { return SensorType.StepDetector; }
        }

        //Callbacks
        public UnityEvent OnStepDetectorSensorChanged;



        //Callback handler for sensor values.
        protected override void ReceiveValues(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            base.ReceiveValues(json);

            if (OnStepDetectorSensorChanged != null)
                OnStepDetectorSensorChanged.Invoke();    //triggers an event
        }
    }
}