using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Get long key press and call back
    /// </summary>
    public class LongKeyInput : MonoBehaviour
    {
#region Inspector settings Section

        //Inspector Settings
        public KeyCode targetKey = KeyCode.Escape;  //target key code

        public float validTime = 1.5f;              //Time to recognize as long press (to recognize it as a long press with longer time)    //長押しとして認識する時間（これより長い時間で長押しとして認識する）

        //Callbacks
        //Long press complete event callback        //長押し完了イベントコールバック
        public UnityEvent OnLongPress;

        //Long press/progress start event callback  //長押し/進捗開始のイベントコールバック
        public UnityEvent OnStart;

        //Progress event callback                   //進捗のイベントコールバック
        [Serializable] public class ProgressHandler : UnityEvent<float> { } //Amount at progress: 0~1f  //進捗 0～1f
        public ProgressHandler OnProgress;

        //Progress interrupted event callback       //進捗中断のイベントコールバック
        public UnityEvent OnCancel;

#endregion Inspector settings Section

#region Properties and Local values Section

        //Local Values
        float requiredTime;         //Long press recognition time (recognize it as long press after this time)  //長押し認識時刻（この時刻を超えたら長押しとして認識する）
        bool pressing;              //Pressing flag (also used for acquiring only one press)                    //押下中フラグ（単一のみの取得にするため）

        //Long press detection property (For each frame acquisition)
        //長押検出プロパティ（フレーム毎取得用）
        public bool IsLongPress {
            get; private set;
        }

#endregion

#region LongPress operation Section

        //アクティブになったら、初期化する（アプリの中断などしたときはリセットする）
        void OnEnable()
        {
            pressing = false;
        }

        // Update is called once per frame
        void Update()
        {
            IsLongPress = false;    //Reset per frame

            if (!pressing && Input.GetKeyDown(targetKey))
            {
                pressing = true;
                requiredTime = Time.time + validTime;

                if (OnStart != null)
                    OnStart.Invoke();   //progress start event
            }
            if (pressing)
            {
                if (Input.GetKey(targetKey))
                {
                    if (requiredTime < Time.time)
                    {
                        IsLongPress = true;
                        pressing = false;

                        if (OnLongPress != null)
                            OnLongPress.Invoke();   //complete event
                    }
                    else
                    {
                        if (OnProgress != null)
                        {
                            float amount = Mathf.Clamp01(1f - (requiredTime - Time.time) / validTime);  //0~1f
                            OnProgress.Invoke(amount);  //progress event
                        }
                    }
                }
                if (Input.GetKeyUp(targetKey))
                {
                    pressing = false;

                    if (OnCancel != null)
                        OnCancel.Invoke();  //progress cancel event
                }
            }
        }

#endregion LongPress operation Section
    }
}
