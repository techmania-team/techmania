using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace FantomLib
{
    /// <summary>
    /// Get swipe direction and call back (Suitable for judgment on UI, 'EventSystem' and 'Graphics Raycaster' are required)
    /// http://fantom1x.blog130.fc2.com/blog-entry-250.html
    /// (Usage)
    ///･You can use it by attaching to a GameObject that has a UI such as Image, Text, Button, etc. and registering a function callback to OnSwipe (Vector 2 as one argument) from the inspector.
    ///･There is an 'EventSystem' in the scene, and 'Graphics Raycaster' is attached to the (root) Canvas.
    /// (Specification)
    ///･It is judged by the movement amount of the touch started from UI (a mouse in the case of editor or other than smartphone). 
    /// When moving more than the screen width Valid Width (%), it is recognized as a swipe.
    ///･However, ignore it when the movement exceeds the time limit (Timeout).
    ///･Can not recognize with multiple fingers (* In case of two or more fingers, it is invalid because there is possibility of pinching).
    ///･Since the touch device is UNITY_ANDROID, UNITY_IOS, if you want to add other devices, add the device to the '#if' conditional statement (only those that can be acquired by 'Input.touchCount').
    ///(*) If it is a smartphone, it is not possible to recognize well when UI is transparent, so be careful (opaque image is as good as possible).
    /// 
    /// 
    /// スワイプ方向を取得してコールバックする（UI上での判定に向いている。EventSystem と Graphics Raycaster が必要）
    /// http://fantom1x.blog130.fc2.com/blog-entry-250.html
    ///（使い方）
    ///・Image や Text, Button などの UI を持つ GameObject にアタッチして、インスペクタから OnSwipe（Vector2 を１つ引数にとる）にコールバックする関数を登録すれば使用可。
    ///・シーンに EventSystem、(ルート)Canvas に Graphics Raycaster がアタッチされている必要がある。
    ///（仕様説明）
    ///・UI 上から開始されたタッチの移動量（エディタやスマホ以外の場合はマウス）で判定する。画面幅の Valid Width（％）以上移動したときスワイプとして認識する。
    ///・ただし、移動が制限時間（Timeout）を超えた時は無視する。
    ///・複数の指では認識できない（※２つ以上の指の場合はピンチの可能性もあるため無効とする）。
    ///・タッチデバイスを UNITY_ANDROID, UNITY_IOS としているので、他のデバイスも加えたい場合は #if の条件文にデバイスを追加する（Input.touchCount が取得できるもののみ）。
    ///※スマホだとUIを透過にしていると、上手く認識できないようなので注意（なるべく不透明画像が良い）。
    /// </summary>
    public class SwipeEventTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
#region Inspector settings and Local values Section

        //Inspector Settings
        //Make the screen width (Screen.width) size the standard of the ratio (based on false = height (Screen.height))
        //画面幅（Screen.width）サイズを比率の基準にする（false=高さ（Screen.height）を基準）
        public bool widthReference = true;

        //Screen ratio of movement amount recognized as swipe [Ratio to screen width] (0.0~1.0 : Width of entire screen ratio, recognize it as a swipe with a movement amount longer than this value)
        //スワイプとして認識する移動量の画面比[画面幅に対する比率]（0.0～1.0：1.0で端から端まで。この値より長い移動量でスワイプとして認識する）
        public float validWidth = 0.25f;

        //Time to recognize as a swipe (to recognize it as a swipe in less time)
        //スワイプとして認識する時間（これより短い時間でスワイプとして認識する）
        public float timeout = 0.5f;


        //Swipe event callback (for inspector)
        //スワイプイベントコールバック（インスペクタ用）
        [Serializable]
        public class SwipeHandler : UnityEvent<Vector2> { } //Swipe direction   //スワイプ方向
        public SwipeHandler OnSwipe;


        //Local Values
        Vector2 startPos;                   //Swipe start coordinates.                                          //スワイプ開始座標
        Vector2 endPos;                     //Swipe end coordinates.                                            //スワイプ終了座標
        float limitTime;                    //Swipe time limit (Do not recognize it as swipe beyond this time.  //スワイプ時間制限（この時刻を超えたらスワイプとして認識しない）
        bool pressing;                      //Pressing flag (to obtain only a single finger).                   //押下中フラグ（単一指のみの取得にするため）

        //The acquired swipe direction (for each frame judgment) [zero, no left, right, up, down direction]
        //取得したスワイプ方向（フレーム毎判定用）[zeroがなしで、left, right, up, downが方向]
        Vector2 swipeDir = Vector2.zero;

#endregion Inspector settings and Local values Section
       
#region Swipe operation Section

        //When it becomes active, initialize (reset when the application interrupt etc.)
        //アクティブになったら、初期化する（アプリの中断などしたときはリセットする）
        void OnEnable()
        {
            pressing = false;
        }
        
        // Update is called once per frame
        private void Update () {
            swipeDir = Vector2.zero;    //Reset per frame   //フレーム毎にリセット

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)   //Only platforms you want to obtain with touch.     //タッチで取得したいプラットフォームのみ
            if (Input.touchCount != 1)  //Multiple fingers are impossible (because there is a possibility of pinching in case of two or more fingers).  //複数の指は不可とする（※２つ以上の指の場合はピンチの可能性もあるため）
#else
            if (!Input.GetMouseButton(0))
#endif
            {
                pressing = false;
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)   //Only platforms you want to obtain with touch.     //タッチで取得したいプラットフォームのみ
            if (Input.touchCount == 1)  //Multiple fingers are impossible (because there is a possibility of pinching in case of two or more fingers).  //複数の指は不可とする（※２つ以上の指の場合はピンチの可能性もあるため）
#endif
            {
                if (!pressing)
                {
                    startPos = Input.mousePosition;
                    pressing = true;
                    limitTime = Time.time + timeout;
                }
            }
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)   //タッチで取得したいプラットフォームのみ
            else  //Invalid it when there is not one touch (since there is also a possibility of pinching in case of two or more fingers).  //タッチが１つでないときは無効にする（※２つ以上の指の場合はピンチの可能性もあるため）
            {
                pressing = false;
            }
#endif
        }

        public void OnPointerUp(PointerEventData eventData)
        {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)   //Only platforms you want to obtain with touch.     //タッチで取得したいプラットフォームのみ
            if (Input.touchCount == 1)  //Multiple fingers are impossible (because there is a possibility of pinching in case of two or more fingers).  //複数の指は不可とする（※２つ以上の指の場合はピンチの可能性もあるため）
#endif
            {
                if (pressing)
                {
                    pressing = false;

                    if (Time.time < limitTime)  //Recognize before time limit   //時間制限前なら認識
                    {
                        endPos = Input.mousePosition;
                        Vector2 dist = endPos - startPos;
                        float dx = Mathf.Abs(dist.x);
                        float dy = Mathf.Abs(dist.y);
                        float requiredPx = widthReference ? Screen.width * validWidth : Screen.height * validWidth;

                        if (dy < dx)    //Recognized as horizontal direction            //横方向として認識
                        {
                            if (requiredPx < dx)   //Recognize if it exceeds length     //長さを超えていたら認識
                                swipeDir = Mathf.Sign(dist.x) < 0 ? Vector2.left : Vector2.right;
                        }
                        else    //Recognized as vertical direction                      //縦方向として認識
                        {
                            if (requiredPx < dy)   //Recognize if it exceeds length     //長さを超えていたら認識
                                swipeDir = Mathf.Sign(dist.y) < 0 ? Vector2.down : Vector2.up;
                        }

                        if (swipeDir != Vector2.zero)
                        {
                            if (OnSwipe != null)
                                OnSwipe.Invoke(swipeDir);
                        }
                    }
                }
            }
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)   //タッチで取得したいプラットフォームのみ
            else  //Invalid it when there is not one touch (since there is also a possibility of pinching in case of two or more fingers).  //タッチが１つでないときは無効にする（※２つ以上の指の場合はピンチの可能性もあるため）
            {
                pressing = false;
            }
#endif
        }

#endregion Swipe operation Section
    }
}
