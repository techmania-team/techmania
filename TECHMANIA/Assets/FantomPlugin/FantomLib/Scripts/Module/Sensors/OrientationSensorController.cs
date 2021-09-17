using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Orientation Sensor Controller
    /// http://fantom1x.blog130.fc2.com/blog-entry-294.html
    ///(*) This sensor was deprecated in Android 2.2 (API level 8), 
    ///    and this sensor type was deprecated in Android 4.4W (API level 20).
    ///    The sensor framework provides alternate methods for acquiring device orientation,
    ///    which are discussed in Computing the Device's Orientation.
    /// 
    ///(Sensor Type)
    /// https://developer.android.com/reference/android/hardware/Sensor.html#TYPE_ORIENTATION
    ///(Sensor Values)
    /// values[0]: Azimuth, angle between the magnetic north direction and the y-axis, around the z-axis (0 to 359). 0=North, 90=East, 180=South, 270=West
    /// values[1]: Pitch, rotation around x-axis (-180 to 180), with positive values when the z-axis moves toward the y-axis.
    /// values[2]: Roll, rotation around the y-axis (-90 to 90) increasing as the device moves clockwise.
    /// https://developer.android.com/reference/android/hardware/SensorEvent.html#values
    ///(Sensor Delay)
    /// https://developer.android.com/reference/android/hardware/SensorManager.html#SENSOR_DELAY_FASTEST
    ///(Position Sensors)
    /// https://developer.android.com/guide/topics/sensors/sensors_position.html
    /// </summary>
    public class OrientationSensorController : SensorControllerBase
    {
        protected override SensorType sensorType {
            get { return SensorType.Orientation; }  //This constant was deprecated in API level 20.
        }

        //Callbacks
        [Serializable] public class OrientationSensorChangedHandler : UnityEvent<float, float, float> { }   //Azimuth, Pitch , Roll [Degrees]
        public OrientationSensorChangedHandler OnOrientationSensorChanged;



        //Callback handler for sensor values.
        //values[0]: Azimuth (angle around the z-axis) [Degrees]
        //values[1]: Pitch (angle around the x-axis). [Degrees]
        //values[2]: Roll (angle around the y-axis). [Degrees]
        protected override void ReceiveValues(string json)
        {
            if (string.IsNullOrEmpty(json))
                return;

            base.ReceiveValues(json);

            if (OnOrientationSensorChanged != null)
                OnOrientationSensorChanged.Invoke(info.values[0], info.values[1], info.values[2]);    //Azimuth, Pitch , Roll [Degrees]
        }
    }
}
