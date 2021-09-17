using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace FantomLib
{
    /// <summary>
    /// Get long press and call back (Suitable for judgment on UI, 'EventSystem' and 'Graphics Raycaster' are required)
    /// http://fantom1x.blog130.fc2.com/blog-entry-251.html
    /// (Usage)
    ///･You can use it by attaching to a GameObject that has a UI such as Image, Text, Button, etc. and registering a function callback to 'OnLongClick' (no argument) from the inspector.
    ///･There is an 'EventSystem' in the scene, and 'Graphics Raycaster' is attached to the (root) Canvas.
    /// (Specification)
    ///･Events (OnPointerDown, OnPointerUp, OnPointerExit) from the EventSystem are acquired, and if it is kept pressed for a certain time (Valid Time), long press is recognized.
    ///･It is invalid when going out of the effective area (out of the UI) or releasing your finger on the way.
    ///･Only the first finger recognition (In the case of multiple fingers, it is invalid because there is a possibility of pinching).
    ///･Since the touch device is UNITY_ANDROID, UNITY_IOS, if you want to add other devices, add the device to the '#if' conditional statement (only those that can be acquired by 'Input.touchCount').
    ///(*) If it is a smartphone, it is not possible to recognize well when UI is transparent, so be careful (opaque image is as good as possible).
    /// 
    /// 
    /// 長押しを取得してコールバックする（UI上での判定に向いている。EventSystem と Graphics Raycaster が必要）
    /// http://fantom1x.blog130.fc2.com/blog-entry-251.html
    ///（使い方）
    ///・Image や Text, Button などの UI を持つ GameObject にアタッチして、インスペクタから OnLongClick（引数なし）にコールバックする関数を登録すれば使用可。
    ///・シーンに EventSystem、(ルート)Canvas に Graphics Raycaster がアタッチされている必要がある。
    ///（仕様説明）
    ///・EventSystem からのイベント（OnPointerDown, OnPointerUp, OnPointerExit）を取得し、一定時間（Valid Time）押下され続けていたら長押しと認識する。
    ///・途中で有効領域外（UIから外れる）へ出たり、指を離したりしたときは無効。
    ///・はじめの指のみ認識（複数指の場合、ピンチの可能性があるため無効とする）。
    ///・タッチデバイスを UNITY_ANDROID, UNITY_IOS としているので、他のデバイスも加えたい場合は #if の条件文にデバイスを追加する（Input.touchCount が取得できるもののみ）。
    ///※スマホだとUIを透過にしていると、上手く認識できないようなので注意（なるべく不透明画像が良い）。
    /// </summary>
    public class LongClickEventTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
#region Inspector settings and Local values Section

        //Inspector Settings
        public float validTime = 1.0f;      //Time to recognize as long press (to recognize it as a long press with longer time)    //長押しとして認識する時間（これより長い時間で長押しとして認識する）


        //Long press event occurred call back
        //長押しイベント発生コールバック
        public UnityEvent OnLongClick;

        //Long press/progress start event callback
        //長押し進捗開始のイベントコールバック
        public UnityEvent OnStart;

        //Progress event callback
        //進捗中のイベントコールバック
        [Serializable] public class ProgressHandler : UnityEvent<float> { }     //Progress 0 to 1f (0~100%)     //進捗 0～1f（0～100%）
        public ProgressHandler OnProgress;

        //Progress interrupted event callback
        //進捗中断のイベントコールバック
        public UnityEvent OnCancel;


        //Local Values
        float requiredTime;                 //Long press recognition time (recognize it as long press after this time)              //長押し認識時刻（この時刻を超えたら長押しとして認識する）
        bool pressing = false;              //Pressing flag (also used for acquiring only a single finger)                          //押下中フラグ（単一指のみの取得としても利用）

#endregion Inspector settings and Local values Section

#region LongClick operation Section

        //When it becomes active, initialize (reset when the application interrupt etc.)
        //アクティブになったら、初期化する（アプリの中断などしたときはリセットする）
        void OnEnable()
        {
            pressing = false;
        }

        // Update is called once per frame
        void Update()
        {
            if (pressing)   //Only the finger that pushed first   //はじめに押した指のみとなる
            {
                if (requiredTime < Time.time)   //Recognized after a certain period of time     //一定時間過ぎたら認識
                {
                    if (OnLongClick != null)
                        OnLongClick.Invoke();   //UnityEvent

                    pressing = false;           //Invalid after long press  //長押し完了したら無効にする
                }
                else
                {
                    if (OnProgress != null)
                    {
                        float amount = Mathf.Clamp01(1f - (requiredTime - Time.time) / validTime);  //0～1f
                        OnProgress.Invoke(amount);
                    }
                }
            }
        }

        //Press in the UI area
        //UI領域内で押下
        public void OnPointerDown(PointerEventData data)
        {
            if (!pressing)          //To make it unique     //ユニークにするため
            {
                pressing = true;
                requiredTime = Time.time + validTime;

                if (OnStart != null)
                    OnStart.Invoke();   //UnityEvent
            }
            else
            {
                pressing = false;   //In the case of two or more fingers, since there is a possibility of pinching, it is made ineffective  //２本以上の指の場合、ピンチの可能性があるため無効にする
            }
        }

        //(*) If it is a smartphone and it is transparent to the UI, it will react even if you move your finger a little.
        //※スマホだとUIを透過にしていると、指を少し動かしただけでも反応してしまうので注意
        public void OnPointerUp(PointerEventData data)
        {
            if (pressing)           //Only the finger that pushed first     //はじめに押した指のみとなる
            {
                if (OnCancel != null)
                    OnCancel.Invoke();   //UnityEvent

                pressing = false;
            }
        }

        //Invalid it if it is outside the UI area
        //UI領域から外れたら無効にする
        public void OnPointerExit(PointerEventData data)
        {
            if (pressing)           //Only the finger that pushed first     //はじめに押した指のみとなる
            {
                if (OnCancel != null)
                    OnCancel.Invoke();   //UnityEvent

                pressing = false;   //Invalid it when it is out of the area     //領域から外れたら無効にする
            }
        }

#endregion LongClick operation Section
    }
}