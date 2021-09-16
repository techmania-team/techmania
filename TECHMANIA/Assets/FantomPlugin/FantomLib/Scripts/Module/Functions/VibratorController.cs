using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    //(*) Required: '<uses-permission android:name="android.permission.VIBRATE"/>' in 'AndroidManifest.xml'
    /// <summary>
    /// Vibrator Controller
    /// 
    /// Note: To use this function, you need 'VIBRATE' permission in the 'AndroidManifest.xml'.
    /// Note: This function is deprecated in API 26 (Android 8.0), so some devices may not be available depending on the model.
    /// 
    ///※この機能を利用するには "AndroidManifest.xml" に "VIBRATE" パーミッションが必要です。
    ///※API 26 (Android 8.0) では、この機能は deprecated（廃止予定）となっているため、端末によっては使えないものがあるかもしれません。
    /// </summary>
    public class VibratorController : MonoBehaviour
    {
        //Inspector Settings
        [Serializable]
        public enum VibratorType {
            OneShot,        //It vibrates only once.
            Pattern,        //It vibrates once with a pattern.
            PatternLoop,    //Vibrates repeatedly with a pattern.
        }
        public VibratorType vibratorType = VibratorType.OneShot;

        //Because it can not be saved in the inspector if it is a long type, it is converted from an int type.
        //That is, the value will be in the int range (however, a long range is not necessary).
        [SerializeField] private int duration;      //Vibration duration when SimpleOneShot.
        [SerializeField] private int[] pattern;     //Vibration pattern. Specify the duration in milliseconds in the order of off, on, off, on, ....

        //Callbacks
        [Serializable] public class ErrorHandler : UnityEvent<string> { }       //error state message
        public ErrorHandler OnError;

#region Properties and Local values Section

        //Local values
        //Because it can not be saved in the inspector if it is a long type, it is converted from an int type.
        //That is, the value will be in the int range (however, a long range is not necessary).
        private long mDuration;
        private long[] mPattern;

        //Properties
        public long[] Pattern {
            get { return mPattern; }
            set { mPattern = value; }
        }

        public long Duration {
            get { return mDuration; }
            set { mDuration = value; }
        }

        private static bool isSupportedVibrator = false;    //Cached supported Vibrator (Because Vibrator shares one, it is static).
        private static bool isSupportedChecked = false;     //Already checked (Because Vibrator shares one, it is static).

        //Whether the device supports Vibrator.
        public bool IsSupportedVibrator {
            get {
                if (!isSupportedChecked)
                {
#if UNITY_EDITOR
                    isSupportedVibrator = true;     //For Editor (* You can rewrite it as you like.)
#elif UNITY_ANDROID
                    isSupportedVibrator = AndroidPlugin.IsSupportedVibrator();
#endif
                    isSupportedChecked = true;
                }
                return isSupportedVibrator;
            }
        }

        //Whether necessary permissions are granted.
        public bool IsPermissionGranted {
            get {
#if UNITY_EDITOR
                return true;    //For Editor (* You can rewrite it as you like.)
#elif UNITY_ANDROID
                return AndroidPlugin.CheckPermission("android.permission.VIBRATE");
#else
                return false;
#endif
            }
        }

#endregion

        // Use this for initialization
        private void Awake()
        {
            //It mainly converts from int type to long type (also inspector -> local value).
            mDuration = duration;
            if (pattern != null && pattern.Length > 0)
                mPattern = pattern.Select(e => (long)e).ToArray();
        }

        private void Start()
        {
            if (!IsSupportedVibrator)
            {
                if (OnError != null)
                    OnError.Invoke("Not supported: Vibrator");
            }
            if (!IsPermissionGranted)
            {
                if (OnError != null)
                    OnError.Invoke("Permission denied: VIBRATE");
            }
        }

        // Update is called once per frame
        //private void Update()
        //{

        //}

        
        //Start vibration
        public void StartVibrator()
        {
            if (!IsSupportedVibrator || !IsPermissionGranted)
                return;

#if UNITY_EDITOR
            Debug.Log("VibratorController.StartVibrator called");
#elif UNITY_ANDROID
            switch (vibratorType)
            {
                case VibratorType.OneShot:
                    AndroidPlugin.StartVibrator(mDuration);
                    break;
                case VibratorType.Pattern:
                    AndroidPlugin.StartVibrator(mPattern, false);
                    break;
                case VibratorType.PatternLoop:
                    AndroidPlugin.StartVibrator(mPattern, true);
                    break;
            }
#endif
        }

        //Start vibration with oneshot (current value will be overwritten)
        public void StartVibrator(long duration)
        {
            if (vibratorType != VibratorType.OneShot)
                return;

            Duration = duration;
            StartVibrator();
        }

        //Start vibration with pattern (current value will be overwritten)
        public void StartVibrator(long[] pattern)
        {
            if (vibratorType == VibratorType.OneShot)
                return;

            Pattern = pattern;
            StartVibrator();
        }


        //Interrupt the vibration
        public void Cancel()
        {
#if UNITY_EDITOR
            Debug.Log("VibratorController.Cancel called");
#elif UNITY_ANDROID
            AndroidPlugin.CancelVibrator();
#endif
        }

    }
}
