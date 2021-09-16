using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Geomagnetic Rotation Vector Sensor Controller
    /// http://fantom1x.blog130.fc2.com/blog-entry-294.html
    /// 
    ///(Sensor Type)
    /// https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_ROTATION_VECTOR
    ///(Sensor Values)
    /// https://developer.android.com/reference/android/hardware/SensorEvent.html#values
    ///(Sensor Delay)
    /// https://developer.android.com/reference/android/hardware/SensorManager.html#SENSOR_DELAY_FASTEST
    ///(Position Sensors)
    /// https://developer.android.com/guide/topics/sensors/sensors_position.html
    /// </summary>
    public class GeomagneticRotationVectorSensorController : SensorControllerBase
    {
        protected override SensorType sensorType {
            get { return SensorType.GeomagneticRotationVector; }
        }

        //Callbacks
        [Serializable] public class GeomagneticRotationVectorSensorChangedHandler : UnityEvent<float, float, float> { }   //x, y, z
        public GeomagneticRotationVectorSensorChangedHandler OnGeomagneticRotationVectorSensorChanged;



        //Callback handler for sensor values.
        protected override void ReceiveValues(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            base.ReceiveValues(json);

            if (OnGeomagneticRotationVectorSensorChanged != null)
                OnGeomagneticRotationVectorSensorChanged.Invoke(info.values[0], info.values[1], info.values[2]);    //x, y, z
        }
    }
}
