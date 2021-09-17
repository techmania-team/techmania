using System;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Get swipe direction and call back
    /// http://fantom1x.blog130.fc2.com/blog-entry-250.html
    /// (Usage)
    ///･You can use it by attaching to an appropriate GameObject and registering a function callback to 'OnSwipe' (take a Vector2 argument) from the inspector.
    ///･Or you can monitor the property 'SwipeInput.Direction' on per frame (including not move (Vector2.zero) in this case).
    /// (Specification)
    ///･It is judged by the movement amount of the touch (a mouse in the case of editor or other than smartphone). 
    /// When moving more than the screen width Valid Width (%), it is recognized as a swipe.
    ///･However, ignore it when the movement exceeds the time limit (Timeout).
    ///･Can not recognize with multiple fingers (* In case of two or more fingers, it is invalid because there is possibility of pinching).
    ///･Since the touch device is UNITY_ANDROID, UNITY_IOS, if you want to add other devices, add the device to the '#if' conditional statement (only those that can be acquired by 'Input.touchCount').
    /// 
    /// 
    /// スワイプ方向を取得してコールバックする
    /// http://fantom1x.blog130.fc2.com/blog-entry-250.html
    ///（使い方）
    ///・適当な GameObject にアタッチして、インスペクタから OnSwipe（Vector2 を１つ引数にとる）にコールバックする関数を登録すれば使用可。
    ///・またはプロパティ SwipeInput.Direction をフレーム毎監視しても良い（こちらの場合は無し（Vector2.zero）も含まれる）。
    ///（仕様説明）
    ///・タッチの移動量（エディタやスマホ以外の場合はマウス）で判定する。画面幅の Valid Width（％）以上移動したときスワイプとして認識する。
    ///・ただし、移動が制限時間（Timeout）を超えた時は無視する。
    ///・複数の指では認識できない（※２つ以上の指の場合はピンチの可能性もあるため無効とする）。
    ///・タッチデバイスを UNITY_ANDROID, UNITY_IOS としているので、他のデバイスも加えたい場合は #if の条件文にデバイスを追加する（Input.touchCount が取得できるもののみ）。
    /// </summary>
    public class SwipeInput : MonoBehaviour
    {
#region Inspector settings Section

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

        //Area on screen to recognize: 0.0~1.0 [(0,0):Bottom left of screen, (1,1):Upper right of screen]
        //スワイプとして認識する画面領域（0.0～1.0）[(0,0):画面左下, (1,1):画面右上]
        public Rect validArea = new Rect(0, 0, 1, 1);


        //Swipe event callback (for inspector)
        //スワイプイベントコールバック（インスペクタ用）
        [Serializable]
        public class SwipeHandler : UnityEvent<Vector2> { } //Swipe direction   //スワイプ方向
        public SwipeHandler OnSwipe;

#endregion Inspector settings Section

#region Properties and Local values Section

        //Swipe direction acquisition property (for each frame acquisition)
        //スワイプ方向取得プロパティ（フレーム毎取得用）
        public Vector2 Direction {
            get; private set;
        }


        //Local Values
        Vector2 startPos;                   //Swipe start coordinates.                                          //スワイプ開始座標
        Vector2 endPos;                     //Swipe end coordinates.                                            //スワイプ終了座標
        float limitTime;                    //Swipe time limit (Do not recognize it as swipe beyond this time.  //スワイプ時間制限（この時刻を超えたらスワイプとして認識しない）
        bool pressing;                      //Pressing flag (to obtain only a single finger).                   //押下中フラグ（単一指のみの取得にするため）

#endregion Properties and Local values Section

#region Swipe operation Section

        //When it becomes active, initialize (reset when the application interrupt etc.)
        //アクティブになったら、初期化する（アプリの中断などしたときはリセットする）
        void OnEnable()
        {
            pressing = false;
        }

        // Update is called once per frame
        void Update()
        {
            Direction = Vector2.zero;    //Reset per frame   //フレーム毎にリセット

#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)   //Only platforms you want to obtain with touch.     //タッチで取得したいプラットフォームのみ
            if (Input.touchCount == 1)  //Multiple fingers are impossible (because there is a possibility of pinching in case of two or more fingers).  //複数の指は不可とする（※２つ以上の指の場合はピンチの可能性もあるため）
#endif
            {
                if (!pressing && Input.GetMouseButtonDown(0))   //When pressed (left click / touch can be acquired).    //押したとき（左クリック/タッチが取得できる）
                {
                    startPos = Input.mousePosition;
                    if (validArea.xMin * Screen.width <= startPos.x && startPos.x <= validArea.xMax * Screen.width &&
                        validArea.yMin * Screen.height <= startPos.y && startPos.y <= validArea.yMax * Screen.height)   //Within recognition area   //認識エリア内
                    {
                        pressing = true;
                        limitTime = Time.time + timeout;
                    }
                }
                else if (pressing && Input.GetMouseButtonUp(0))  //Only when it is already pressed (Note that this function can not distinguish which finger when touching 2 or more).   //既に押されているときのみ（※この関数は２つ以上タッチの場合、どの指か判別できないので注意）
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
                                Direction = Mathf.Sign(dist.x) < 0 ? Vector2.left : Vector2.right;
                        }
                        else    //Recognized as vertical direction                      //縦方向として認識
                        {
                            if (requiredPx < dy)   //Recognize if it exceeds length     //長さを超えていたら認識
                                Direction = Mathf.Sign(dist.y) < 0 ? Vector2.down : Vector2.up;
                        }

                        if (Direction != Vector2.zero)
                        {
                            if (OnSwipe != null)
                                OnSwipe.Invoke(Direction);
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