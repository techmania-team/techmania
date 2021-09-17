using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FantomLib
{
    /// <summary>
    /// Hardware Volume When operated, Android Toast displays the current volume
    /// 
    /// ハードウェア音量操作したとき、Android の Toast で現在の音量を表示する（AndroidPlugin を使用する）
    /// </summary>
    public class HardVolumeControllerWithToast : HardVolumeController
    {

        public bool enableToast = true;     //Display Android Toast on/off     //Toast の表示可否


        protected void Awake()
        {
            //Register itself when it is empty (* Note that it is not displayed in the inspector).
            //独自登録されてないとき、自身を登録する（※インスペクタには表示されないので注意）
            if (OnVolumeCalled.GetPersistentEventCount() == 0)
            {
#if UNITY_EDITOR
                Debug.Log("OnVolumeCalled added DisplayVolume (auto)");
#endif
                OnVolumeCalled.AddListener(DisplayVolume);
            }
        }


        //Display Android Toast
        //Android の Toast で表示
        public void ShowToast(string message)
        {
            if (!enableToast)
                return;

#if UNITY_EDITOR
            Debug.Log("ShowToast : " + message);
#elif UNITY_ANDROID
            if (!string.IsNullOrEmpty(message))
                AndroidPlugin.ShowToast(message);
#endif
        }


        //Format to a string for display Android Toast
        //Android の Toast で表示（文字列にフォーマットする）
        public void DisplayVolume(int value)
        {
            ShowToast("Volume : " + value + " / " + maxVolume);
        }

    }

}