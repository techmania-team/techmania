using System;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Get long press and call back (Suitable for judgment in whole screen area and partial area of screen)
    /// http://fantom1x.blog130.fc2.com/blog-entry-251.html
    /// (Usage)
    ///･You can use it by attaching to an appropriate GameObject and registering a function callback to 'OnLongClick' (no argument) from the inspector.
    ///･Or you can monitor the property 'LongClickInput.IsLongClick' on per frame (including false in this case).
    /// (Specification)
    ///･Let the entire screen be (0,0)-(1,1) and recognize it when it is pressed for a certain time (Valid Time) within the valid area (Valid Area).
    ///･It is invalid when going out of the effective area on the way or releasing your finger.
    ///･Only the first finger recognition (In the case of multiple fingers, it is invalid because there is a possibility of pinching).
    ///･Since the touch device is UNITY_ANDROID, UNITY_IOS, if you want to add other devices, add the device to the '#if' conditional statement (only those that can be acquired by 'Input.touchCount').
    ///
    /// 
    /// 長押しを取得してコールバックする（画面全域や画面の一部領域などでの判定に向いている）
    /// http://fantom1x.blog130.fc2.com/blog-entry-251.html
    ///（使い方）
    ///・適当な GameObject にアタッチして、インスペクタから OnLongClick（引数なし）にコールバックする関数を登録すれば使用可。
    ///・またはプロパティ LongClickInput.IsLongClick をフレーム毎監視しても良い（こちらの場合は false も含まれる）。
    ///（仕様説明）
    ///・画面全体を(0,0)-(1,1)とし、有効領域内（Valid Area）で一定時間（Valid Time）押下されていたら認識する。
    ///・途中で有効領域外へ出たり、指を離したりしたときは無効。
    ///・はじめの指のみ認識（複数の指の場合、ピンチの可能性があるため無効とする）。
    ///・タッチデバイスを UNITY_ANDROID, UNITY_IOS としているので、他のデバイスも加えたい場合は #if の条件文にデバイスを追加する（Input.touchCount が取得できるもののみ）。
    /// </summary>
    public class LongClickInput : MonoBehaviour
    {
#region Inspector settings Section

        //Inspector Settings

        //Time to recognize as long press (to recognize it as a long press with longer time)
        //長押しとして認識する時間（これより長い時間で長押しとして認識する）
        public float validTime = 1.0f;

        //Area on screen to recognize: 0.0~1.0 [(0,0):Bottom left of screen, (1,1):Upper right of screen]
        //長押しとして認識する画面領域（0.0～1.0）[(0,0):画面左下, (1,1):画面右上]
        public Rect validArea = new Rect(0, 0, 1, 1);


        //Long press event callback
        //長押しイベントコールバック（インスペクタ用）
        public UnityEvent OnLongClick;      //no arguments   //引数なし

        //Long press/progress start event callback
        //長押し・進捗開始のイベントコールバック
        public UnityEvent OnStart;

        //Progress event callback
        //進捗のイベントコールバック
        [Serializable] public class ProgressHandler : UnityEvent<float> { } //Amount at progress: 0~1f  //進捗 0～1f
        public ProgressHandler OnProgress;

        //Progress interrupted event callback
        //進捗中断のイベントコールバック
        public UnityEvent OnCancel;

#endregion Inspector settings Section

#region Properties and Local values Section

        //Long press detection property (For each frame acquisition)
        //長押検出プロパティ（フレーム毎取得用）
        public bool IsLongClick {
            get; private set;
        }


        //Local Values
        Vector2 minPos = Vector2.zero;      //Long press recognition pixel minimum coordinate.                          //長押し認識ピクセル最小座標
        Vector2 maxPos = Vector2.one;       //Long press recognition pixel maximum coordinate.                          //長押し認識ピクセル最大座標
        float requiredTime;                 //Long press recognition time (recognize it as long press after this time). //長押し認識時刻（この時刻を超えたら長押しとして認識する）
        bool pressing;                      //Pressing flag (also used for acquiring only one finger)                   //押下中フラグ（単一指のみの取得にするため）

#endregion Properties and Local values Section

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
            IsLongClick = false;    //Reset per frame   //フレーム毎にリセット

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)   //Only platforms you want to obtain with touch.     //タッチで取得したいプラットフォームのみ
            if (Input.touchCount == 1)  //Multiple fingers are impossible (because there is a possibility of pinching in case of two or more fingers).  //複数の指は不可とする（※２つ以上の指の場合はピンチの可能性もあるため）
#endif
            {
                if (!pressing && Input.GetMouseButtonDown(0))   //When pressed (left click / touch can be acquired).    //押したとき（左クリック/タッチが取得できる）
                {
                    Vector2 pos = Input.mousePosition;
                    minPos.Set(validArea.xMin * Screen.width, validArea.yMin * Screen.height);
                    maxPos.Set(validArea.xMax * Screen.width, validArea.yMax * Screen.height);
                    if (minPos.x <= pos.x && pos.x <= maxPos.x && minPos.y <= pos.y && pos.y <= maxPos.y)   //Within recognition area   //認識エリア内
                    {
                        pressing = true;
                        requiredTime = Time.time + validTime;

                        if (OnStart != null)
                            OnStart.Invoke();
                    }
                }
                if (pressing)      //When already pressed   //既に押されている
                {
                    if (Input.GetMouseButton(0))    //Continue pressing (* This function can not distinguish which finger when touching two or more)    //押下継続（※この関数は２つ以上タッチの場合、どの指か判別できない）
                    {
                        if (requiredTime < Time.time)   //Recognized after a certain period of time     //一定時間過ぎたら認識
                        {
                            Vector2 pos = Input.mousePosition;
                            if (minPos.x <= pos.x && pos.x <= maxPos.x && minPos.y <= pos.y && pos.y <= maxPos.y)   //認識エリア内
                            {
                                IsLongClick = true;

                                if (OnLongClick != null)
                                    OnLongClick.Invoke();
                            }

                            pressing = false;   //Invalid after long press  //長押し完了したら無効にする
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
                    else  //MouseButtonUp, MouseButtonDown
                    {
                        if (pressing)
                        {
                            if (OnCancel != null)
                                OnCancel.Invoke();
                        }

                        pressing = false;
                    }
                }
            }
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)   //Only platforms you want to obtain with touch.     //タッチで取得したいプラットフォームのみ
            else  //Invalid it when there is not one touch (since there is also a possibility of pinching in case of two or more fingers).   //タッチが１つでないときは無効にする（※２つ以上の指の場合はピンチの可能性もあるため）
            {
                pressing = false;
            }
#endif
        }
#endregion LongClick operation Section
    }
}