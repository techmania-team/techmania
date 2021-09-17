using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Hardware volume control class (Media volume only)
    /// http://fantom1x.blog130.fc2.com/blog-entry-283.html
    ///･For example for VR (Cardboard/Hakosuko) setting (Register in the inspector):
    /// enableHardKey = true, hardOperation = false,
    /// "OnVolumeCalled" -> something UI (uGUI, etc.), "OnHardVolumeKeyUp" -> "VolumeUp()", "OnHardVolumeKeyDown" -> "VolumeDown()"
    /// </summary>
    public class HardVolumeController : MonoBehaviour
    {

        public bool enableHardKey = false;      //true = Receive and use hardware button events / false = Ignore hardware button events

        [SerializeField] private bool hardOperation = true; //true = Volume operation on smartphone possible /false = Disable volume operation at the smartphone (*) Change at the runtime by the 'HardOperation' property.

        public bool showUI = true;              //Display System UI when operating volume from Unity.



        //Callback when hardware volume is operated

        //Callback when operating a hardware button, or when this class's volume operation method is called (-> Use for UI display etc.)
        //Argument: int is the hardware volume after operation.
        [Serializable]
        public class VolumeCalledHandler : UnityEvent<int> { }
        public VolumeCalledHandler OnVolumeCalled;

        //Increase the volume with the hardware button Callback when operated.
        public UnityEvent OnHardVolumeKeyUp;

        //Decrease the volume with the hardware button Callback when operated.
        public UnityEvent OnHardVolumeKeyDown;



#if UNITY_EDITOR
        //For debug (Editor only)
        public int debugVolume = 2;
        public bool debugIncreasement = true;
        public KeyCode debugVolumeUpKey = KeyCode.KeypadPlus;
        public KeyCode debugVolumeDownKey = KeyCode.KeypadMinus;
#endif



        //Get maximum hardware volume property
        private int mMaxVolume = -1;    //For cache

        public int maxVolume {
            get {
                if (mMaxVolume < 0)
                {
#if UNITY_EDITOR
                    mMaxVolume = 15;    //For debug (Editor only)
#elif UNITY_ANDROID
                    mMaxVolume = AndroidPlugin.GetMediaMaxVolume();
#endif
                }
                return mMaxVolume;
            }
        }


        //Get current hardware volume property
        public int volume {
            get {
                int vol = -1;
#if UNITY_EDITOR
                vol = debugVolume;    //For debug (Editor only)
#elif UNITY_ANDROID
                vol = AndroidPlugin.GetMediaVolume();
#endif
                return vol;
            }
            set {
                int vol = Mathf.Clamp(value, 0, maxVolume);
#if UNITY_ANDROID && !UNITY_EDITOR
                vol = AndroidPlugin.SetMediaVolume(vol, showUI);
#endif
#if UNITY_EDITOR
                debugVolume = vol;    //For debug (Editor only)
#endif
                if (OnVolumeCalled != null)
                    OnVolumeCalled.Invoke(vol);
            }
        }


        //Enable/Disable the volume operation by the smartphone itself by pressing the hardware button
        public bool HardOperation {
            get { return hardOperation; }
            set {
                hardOperation = value;
#if UNITY_ANDROID && !UNITY_EDITOR
                AndroidPlugin.HardKey.SetVolumeOperation(value);
#endif
            }
        }



        protected void OnEnable()
        {
#if UNITY_EDITOR
            Debug.Log("HardKey Listener registered.");
#elif UNITY_ANDROID
            //Register listener for hardware volume button
            AndroidPlugin.HardKey.SetKeyVolumeUpListener(this.gameObject.name, "HardVolumeKeyChange", "VolumeUp");
            AndroidPlugin.HardKey.SetKeyVolumeDownListener(this.gameObject.name, "HardVolumeKeyChange", "VolumeDown");
#endif
            HardOperation = hardOperation;    //Set the inspector setting
        }


        protected void OnDisable()
        {
#if UNITY_EDITOR
            Debug.Log("HardKey Listener removed.");
#elif UNITY_ANDROID
            //Unregister listener for hardware volume button
            AndroidPlugin.HardKey.RemoveAllListeners();
#endif
        }


        // Use this for initialization
        protected void Start()
        {
        }


        // Update is called once per frame
        protected void Update()
        {
#if UNITY_EDITOR
            //For debug (Editor only)
            if (Input.GetKeyDown(debugVolumeUpKey))
            {
                if (debugIncreasement)
                    debugVolume = Mathf.Clamp(++debugVolume, 0, maxVolume);

                if (OnHardVolumeKeyUp != null)
                    OnHardVolumeKeyUp.Invoke();
            }
            else if (Input.GetKeyDown(debugVolumeDownKey))
            {
                if (debugIncreasement)
                    debugVolume = Mathf.Clamp(--debugVolume, 0, maxVolume);

                if (OnHardVolumeKeyDown != null)
                    OnHardVolumeKeyDown.Invoke();
            }
#endif
        }


        //Hardware volume button event callback handler (called from Android native)
        protected void HardVolumeKeyChange(string message)
        {
            if (enableHardKey)
            {
                if (message == "VolumeUp")      //(*) This value is the value registered for the listener (SetKeyVolumeUpListener())
                {
                    if (OnHardVolumeKeyUp != null)
                        OnHardVolumeKeyUp.Invoke();
                }
                else if (message == "VolumeDown") //(*) This value is the value registered for the listener (SetKeyVolumeDownListener())
                {
                    if (OnHardVolumeKeyDown != null)
                        OnHardVolumeKeyDown.Invoke();
                }
            }
        }


        //Increase volume (limit: maxVolume)
        public void VolumeUp()
        {
            int vol = -1;
#if UNITY_EDITOR
            vol = Mathf.Clamp(++debugVolume, 0, maxVolume); //For debug (Editor only)
#elif UNITY_ANDROID
            vol = AndroidPlugin.AddMediaVolume(1, showUI);  //current volume + 1
#endif
            if (OnVolumeCalled != null)
                OnVolumeCalled.Invoke(vol);
        }


        //Decrease volume (limit: 0)
        public void VolumeDown()
        {
            int vol = -1;
#if UNITY_EDITOR
            vol = Mathf.Clamp(--debugVolume, 0, maxVolume);  //For debug (Editor only)
#elif UNITY_ANDROID
            vol = AndroidPlugin.AddMediaVolume(-1, showUI);  //current volume - 1
#endif
            if (OnVolumeCalled != null)
                OnVolumeCalled.Invoke(vol);
        }


        //Mute the volume (Set to 0)
        public void VolumeMute()
        {
            int vol = -1;
#if UNITY_EDITOR
            vol = debugVolume = 0;    //デバッグ用値
#elif UNITY_ANDROID
            vol = AndroidPlugin.SetMediaVolume(0, showUI);  //set volume = 0
#endif
            if (OnVolumeCalled != null)
                OnVolumeCalled.Invoke(vol);
        }


        //Get the current volume
        public void VolumeNow()
        {
            int vol = volume;   //From property

            if (OnVolumeCalled != null)
                OnVolumeCalled.Invoke(vol);
        }

    }

}
