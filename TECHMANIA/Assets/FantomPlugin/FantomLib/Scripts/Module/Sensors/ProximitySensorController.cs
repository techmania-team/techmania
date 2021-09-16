using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// ProximitySensorController
    /// http://fantom1x.blog130.fc2.com/blog-entry-294.html
    /// 
    /// Note: Some proximity sensors only support a binary near or far measurement.
    /// In this case, the sensor should report its maximum range value in the far state and a lesser value in the near state.
    ///(Sensor Type)
    /// https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_PROXIMITY
    ///(Sensor Values)
    /// https://developer.android.com/reference/android/hardware/SensorEvent.html#values
    ///(Sensor Delay)
    /// https://developer.android.com/reference/android/hardware/SensorManager.html#SENSOR_DELAY_FASTEST
    ///(Position Sensors)
    /// https://developer.android.com/guide/topics/sensors/sensors_position.html
    /// </summary>
    public class ProximitySensorController : SensorControllerBase
    {
        protected override SensorType sensorType {
            get { return SensorType.Proximity; }
        }

        //Callbacks
        [Serializable] public class ProximitySensorChangedHandler : UnityEvent<float> { }   //[cm] Note: Some proximity sensors only support a binary near or far measurement. In this case, the sensor should report its maximum range value in the far state and a lesser value in the near state.    //搭載されてるセンサーによって、最小と最大しか返さないものもある
        public ProximitySensorChangedHandler OnProximitySensorChanged;



        //Callback handler for sensor values.
        protected override void ReceiveValues(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            base.ReceiveValues(json);

            if (OnProximitySensorChanged != null)
                OnProximitySensorChanged.Invoke(info.values[0]);    //[cm] Note: Some proximity sensors only support a binary near or far measurement. In this case, the sensor should report its maximum range value in the far state and a lesser value in the near state.    //搭載されてるセンサーによって、最小と最大しか返さないものもある
        }
    }
}
