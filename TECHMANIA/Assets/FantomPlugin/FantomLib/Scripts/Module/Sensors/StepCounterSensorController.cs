using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Step Counter Sensor Controller (API 19 or higher)
    /// http://fantom1x.blog130.fc2.com/blog-entry-294.html
    /// 
    /// Note: A sensor of this type returns the number of steps taken by the user since the last reboot while activated.
    /// The value is returned as a float (with the fractional part set to zero) and is reset to zero only on a system reboot.
    ///(Sensor Type)
    /// https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_STEP_COUNTER
    ///(Sensor Values)
    /// https://developer.android.com/reference/android/hardware/SensorEvent.html#values
    ///(Sensor Delay)
    /// https://developer.android.com/reference/android/hardware/SensorManager.html#SENSOR_DELAY_FASTEST
    ///(Motion Sensors)
    /// https://developer.android.com/guide/topics/sensors/sensors_motion.html
    /// </summary>
    public class StepCounterSensorController : SensorControllerBase
    {
        protected override SensorType sensorType {
            get { return SensorType.StepCounter; }
        }

        //Callbacks
        [Serializable] public class StepCounterSensorChangedHandler : UnityEvent<int> { }   //steps since starting
        public StepCounterSensorChangedHandler OnStepCounterSensorChanged;



        private int startSteps = -1;     //startup steps

        //Reset the current count (only the number of steps of the difference, keep the number of steps of the sensor intact)
        //現在のカウントのリセット（差分の歩数のみ。センサーの歩数はそのまま）
        public void ResetCount()
        {
            startSteps = -1;
        }


        //Callback handler for sensor values.
        protected override void ReceiveValues(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            base.ReceiveValues(json);

            int nowSteps = Mathf.RoundToInt(info.values[0]);
            if (startSteps < 0)
                startSteps = nowSteps;

            if (OnStepCounterSensorChanged != null)
                OnStepCounterSensorChanged.Invoke(nowSteps - startSteps);    //steps since starting.     //開始してからの歩数
        }
    }
}
