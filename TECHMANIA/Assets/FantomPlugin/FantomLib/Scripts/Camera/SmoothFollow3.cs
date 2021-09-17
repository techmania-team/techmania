using System;
using UnityEngine;

namespace FantomLib
{
    /// <summary>
    /// SmoothFollow added right/left rotation angle, height and distance, 
    /// and added a corresponding to pinch (PinchInput) and swipe (SwipeInput) (originally StandardAssets)
    /// http://fantom1x.blog130.fc2.com/blog-entry-289.html
    /// (Usage)
    ///·You can use it by attaching to a GameObject such as a camera and registering an object as a viewpoint from the inspector to target. 
    /// (Specification)
    ///·It recognizes the whole screen as (0,0) - (1,1) and clicks with the touch or mouse in the valid area (Valid Area).
    ///·The touch operation is effective only for one finger (and the first one) (More than 2 -> When it is 1 does not recognize). 
    ///·In order to separate drag and swipe operations with fingers, If it is larger than the value of AngleOperation.dragWidthLimit (ratio by screen width)
    /// (= finger quickly moved) it is not recognized as a drag (Swipe is recognized by the value of SwipeInput.validWidth).
    ///·Since the touch device is UNITY_ANDROID, UNITY_IOS, if you want to add other devices, add a device to the #if conditional statement (only those that can be acquired by Input.touchCount). 
    /// 
    /// 
    /// SmoothFollow に左右回転アングルと高さと距離の遠近機能を追加したもの
    /// かつ、ピンチ（PinchInput）とスワイプ（SwipeInput）に対応させた（元は StandardAssets の SmoothFollow）
    /// http://fantom1x.blog130.fc2.com/blog-entry-289.html
    ///（使い方）
    ///・カメラなどの GameObject にアタッチして、インスペクタから target に視点となるオブジェクトを登録すれば使用可。
    ///（仕様説明）
    ///・画面全体を(0,0)-(1,1)とし、有効領域内（Valid Area）でタッチまたはマウスでクリックしたとき認識する。
    ///・タッチ操作は指１本のみ（かつ最初の１本）の操作が有効となる（2本以上→１本になったときは認識しない）。
    ///・指でのドラッグとスワイプ操作を分けるため、AngleOperation.dragWidthLimit の値（画面幅による比率）より大きいときは（=指を素早く動かしたときは）
    /// ドラッグとして認識しない（スワイプは SwipeInput.validWidth の値で認識）。
    ///・タッチデバイスを UNITY_ANDROID, UNITY_IOS としているので、他のデバイスも加えたい場合は #if の条件文にデバイスを追加する（Input.touchCount が取得できるもののみ）。
    /// </summary>
    public class SmoothFollow3 : MonoBehaviour
    {
#region Inspector settings Section

        //Inspector Settings
        public Transform target;                    //Object to follow                          //追従するオブジェクト

        public bool autoInitOnPlay = true;          //Automatically calculates distance, height, preAngle from target position at startup   //distance, height, preAngle を起動時に target 位置から自動算出する
        public float distance = 2.0f;               //Distance of XZ plane                      //XZ平面の距離
        public float height = 0f;                   //Y axis height                             //Y軸の高さ
        public float preAngle = 0f;                 //Initial value of camera angle             //カメラアングル初期値

        public bool widthReference = true;          //Make the screen width (Screen.width) size the standard of the ratio (false: based on height (Screen.height))  //画面幅（Screen.width）サイズを比率の基準にする（false=高さ（Screen.height）を基準）

        //Area on screen to recognize: 0.0~1.0 [(0,0):Bottom left of screen, (1,1):Upper right of screen]
        //認識する画面領域（0.0～1.0）[(0,0):画面左下, (1,1):画面右上]
        public Rect validArea = new Rect(0, 0, 1, 1);


        //Rotation operation
        //回転操作
        [Serializable]
        public class AngleOperation
        {
            public float damping = 3.0f;            //Smooth moving speed of left and right rotation    //左右回転のスムーズ移動速度

            //Key input     //キー入力
            public bool keyEnable = true;           //ON/OFF of rotation key operation          //回転のキー操作の ON/OFF 
            public float keySpeed = 45f;            //Speed by key operation                    //左右回転速度
            public KeyCode keyLeft = KeyCode.Z;     //Left rotation key                         //左回転キー
            public KeyCode keyRight = KeyCode.X;    //Right rotation key                        //右回転キー

            //Drag          //ドラッグ
            public bool dragEnable = true;          //ON/OFF of rotation drag operation         //回転のドラッグ操作の ON/OFF 
            public float dragSpeed = 10f;           //Speed by drag operation                   //ドラッグ操作での回転速度
            public float dragWidthLimit = 0.1f;     //Limit width that can be recognized as a drag (0: unlimited ~ 1: Screen.width [when widthReference=true]). Not recognize more than this width (to distinguish it from swipe).  //ドラッグとして認識できる幅（0 のとき制限なし ～ 1 のとき画面幅）。この幅以上は認識しない（スワイプと区別するため）。
        }
        public AngleOperation angleOperation;


        //Turn operation (constant angle rotation)
        //旋回（一定角度回転）
        [Serializable]
        public class TurnOperation
        {
            public float angle = 90f;                       //Angle of turn                     //旋回の角度

            //Key input     //キー入力
            public bool keyEnable = true;                   //ON/OFF of rotation key operation  //旋回キーの ON/OFF 
            public KeyCode keyLeft = KeyCode.KeypadMinus;   //Left rotation key                 //左旋回キー
            public KeyCode keyRight = KeyCode.KeypadPlus;   //Right rotation key                //右旋回キー

            //Swipe         //スワイプ
            public bool swipeEnable = true;                 //ON/OFF of rotation swipe operation    //スワイプで旋回の ON/OFF 
        }
        public TurnOperation turnOperation;


        //Height operation
        //高さの操作
        [Serializable]
        public class HeightOperation
        {
            public float damping = 2.0f;            //Smooth moving speed of height         //上下高さのスムーズ移動速度

            //Key input     //キー入力
            public bool keyEnable = true;           //ON/OFF of height key operation        //高さのキー操作の ON/OFF
            public float keySpeed = 1.5f;           //Speed by key operation                //キー操作での移動速度
            public KeyCode keyUp = KeyCode.C;       //Key height up                         //高さ上へキー
            public KeyCode keyDown = KeyCode.V;     //Keys height down                      //高さ下へキー

            //Drag          //ドラッグ
            public bool dragEnable = true;          //ON/OFF of height drag operation       //高さのドラッグ操作での ON/OFF
            public float dragSpeed = 0.5f;          //Speed by drag operation               //ドラッグ操作での高さ移動速度
        }
        public HeightOperation heightOperation;


        //Distance operation
        //距離の操作
        [Serializable]
        public class DistanceOperation
        {
            public float damping = 1.0f;            //Smooth moving speed of distance       //距離のスムーズ移動速度（キーとホイール）
            public float min = 1.0f;                //Minimum distance on XZ plane          //XZ平面での最小距離

            //キー入力
            public bool keyEnable = true;           //ON/OFF of distance key operation      //距離のキー操作の ON/OFF
            public float keySpeed = 0.5f;           //Speed by key operation                //距離の移動速度
            public KeyCode keyNear = KeyCode.B;     //Key distance near                     //近くへキー
            public KeyCode keyFar = KeyCode.N;      //Key distance far                      //遠くへキー

            //ホイール
            public bool wheelEnable = true;         //ON/OFF of distance wheel operation    //距離のホイール操作の ON/OFF
            public float wheelSpeed = 7f;           //Speed by wheel operation              //ホイール１目盛りの速度

            //ピンチ
            public bool pinchEnable = true;         //ON/OFF of distance pinch operation        //ピンチで距離を操作する
            public float pinchDamping = 5f;         //Smooth moving speed of distance at pinch  //ピンチでの距離のスムーズ移動速度（キーとホイールでの操作と分けるため）
            public float pinchSpeed = 40f;          //Speed by pinch operation                  //ピンチでの距離の変化速度
        }
        public DistanceOperation distanceOperation;


        //Initial reset operation
        //初期状態リセット操作
        [Serializable]
        public class ResetOperation
        {
            public bool keyEnable = true;               //ON/OFF of reset key operation     //初期状態リセットキーの ON/OFF
            public KeyCode key = KeyCode.KeypadPeriod;  //Key reset                         //初期状態リセットキー
        }
        public ResetOperation resetOperation;

#endregion Inspector settings Section

#region Properties and Local values Section

        //Local Values
        float angle;                                //Camera angle (XZ plane)       //カメラアングル(XZ平面)
        Vector3 startPos;                           //Mouse movement start point    //マウス移動始点
        float wantedDistance;                       //Destination distance          //変化先距離
        float resetDistance;                        //For initial distance          //初期距離保存用
        float resetHeight;                          //For initial height            //初期位置高さ保存用
        bool pinched = false;                       //Flag operated with pinch (switch between distanceDamping and pinchDistanceDamping)    //ピンチで操作したフラグ（distanceDamping と pinchDistanceDamping を切り替える）
        bool dragging = false;                      //Drag operation flag           //ドラッグの操作中フラグ


        //Initial reset
        //状態リセット（初期状態に戻す）
        public void ResetOperations()
        {
            height = resetHeight;
            distance = wantedDistance = resetDistance;
            angle = preAngle;
        }

#endregion Properties and Local values Section

#region Unity life cycle Section

        // Use this for initialization
        void Start()
        {
            if (autoInitOnPlay && target != null)
            {
                height = transform.position.y - target.position.y;
                Vector3 dir = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
                distance = dir.magnitude;
                preAngle = AngleXZWithSign(target.forward, dir);
            }

            angle = preAngle;
            resetDistance = wantedDistance = distance;
            resetHeight = height;
        }

        // Update is called once per frame
        void Update()
        {
#if !UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS)   //Only platforms you want to obtain with touch      //タッチで取得したいプラットフォームのみ（モバイル等）
            if (Input.touchCount != 1 || Input.touches[0].fingerId != 0) //Limit to operation of the first finger   //最初の指１本の操作に限定する
            {
                dragging = false;
                return;
            }
#endif
            //Rotation operation
            //回転のキー操作
            if (angleOperation.keyEnable)
            {
                if (Input.GetKey(angleOperation.keyLeft))
                    angle = Mathf.Repeat(angle + angleOperation.keySpeed * Time.deltaTime, 360f);

                if (Input.GetKey(angleOperation.keyRight))
                    angle = Mathf.Repeat(angle - angleOperation.keySpeed * Time.deltaTime, 360f);
            }

            //Turn operation (constant angle rotation)
            //旋回（一定角度回転）キー操作
            if (turnOperation.keyEnable)
            {
                if (Input.GetKeyDown(turnOperation.keyLeft))
                    TurnLeft();

                if (Input.GetKeyDown(turnOperation.keyRight))
                    TurnRight();
            }

            //Height operation
            //高さのキー操作
            if (heightOperation.keyEnable)
            {
                if (Input.GetKey(heightOperation.keyUp))
                    height += heightOperation.keySpeed * Time.deltaTime;

                if (Input.GetKey(heightOperation.keyDown))
                    height -= heightOperation.keySpeed * Time.deltaTime;
            }

            //Rotation or height operation by drag
            //ドラッグ操作
            if (angleOperation.dragEnable || heightOperation.dragEnable)
            {
                Vector3 movePos = Vector3.zero;

                if (!dragging && Input.GetMouseButtonDown(0))
                {
                    startPos = Input.mousePosition;
                    if (validArea.xMin * Screen.width <= startPos.x && startPos.x <= validArea.xMax * Screen.width &&
                        validArea.yMin * Screen.height <= startPos.y && startPos.y <= validArea.yMax * Screen.height)
                    {
                        dragging = true;
                    }
                }
                else if (dragging)
                {
                    if (Input.GetMouseButton(0))
                    {
                        movePos = Input.mousePosition - startPos;
                        startPos = Input.mousePosition;

                        //Restrict by drag width (to separate from swipe)
                        //ドラッグ幅で制限する（スワイプと分別するため）
                        if (angleOperation.dragWidthLimit > 0)
                        {
                            float limit = (widthReference ? Screen.width : Screen.height) * angleOperation.dragWidthLimit;
                            float d = Mathf.Max(Mathf.Abs(movePos.x), Mathf.Abs(movePos.y));  //大きい方で判定
                            if (d > limit)
                            {
                                movePos = Vector3.zero;     //To disable drag   //操作を無効にする
                                dragging = false;
                            }
                        }
                    }
                    else //Input.GetMouseButtonUp(0), exit
                    {
                        dragging = false;
                    }
                }

                if (movePos != Vector3.zero)
                {
                    //Rotation drag operation
                    //回転のドラッグ操作
                    if (angleOperation.dragEnable)
                        angle = Mathf.Repeat(angle + movePos.x * angleOperation.dragSpeed * Time.deltaTime, 360f);

                    //Heigh drag operation
                    //高さのドラッグ操作
                    if (heightOperation.dragEnable)
                        height -= movePos.y * heightOperation.dragSpeed * Time.deltaTime;
                }
            }

            //Distance operation
            //距離のキー操作
            if (distanceOperation.keyEnable)
            {
                if (Input.GetKey(distanceOperation.keyNear))
                {
                    wantedDistance = Mathf.Max(distanceOperation.min, distance - distanceOperation.keySpeed);
                    pinched = false;
                }

                if (Input.GetKey(distanceOperation.keyFar))
                {
                    wantedDistance = distance + distanceOperation.keySpeed;
                    pinched = false;
                }
            }

            //Distance operation by wheel
            //距離のホイール遠近
            if (distanceOperation.wheelEnable)
            {
                float mw = Input.GetAxis("Mouse ScrollWheel");  //-0.1f, 0f, 0.1f
                if (mw != 0)
                {
                    wantedDistance = Mathf.Max(distanceOperation.min, distance - mw * distanceOperation.wheelSpeed); //0.1 x n times
                    pinched = false;
                }
            }

            //Initial reset operation
            //初期状態リセット
            if (resetOperation.keyEnable)
            {
                if (Input.GetKeyDown(resetOperation.key))
                    ResetOperations();
            }
        }

        void LateUpdate()
        {
            if (target == null)
                return;

            //Follower position
            //追従先位置
            float wantedRotationAngle = target.eulerAngles.y + angle;
            float wantedHeight = target.position.y + height;

            //Current position
            //現在位置
            float currentRotationAngle = transform.eulerAngles.y;
            float currentHeight = transform.position.y;

            //Smooth movement distance (direction) to following destination
            //追従先へのスムーズ移動距離(方向)
            currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, wantedRotationAngle,
                angleOperation.damping * Time.deltaTime);
            currentHeight = Mathf.Lerp(currentHeight, wantedHeight, heightOperation.damping * Time.deltaTime);
            distance = Mathf.Lerp(distance, wantedDistance,
                (pinched ? distanceOperation.pinchDamping : distanceOperation.damping) * Time.deltaTime);

            //Camera movement
            //カメラの移動
            var currentRotation = Quaternion.Euler(0, currentRotationAngle, 0);
            Vector3 pos = target.position - currentRotation * Vector3.forward * distance;
            pos.y = currentHeight;
            transform.position = pos;

            transform.LookAt(target);
        }

#endregion Unity life cycle Section

#region Pinch, Swipe operation Section

        //width: distance of two fingers of pinch
        //center: The coordinates of the center of two fingers of a pinch
        //
        //width: ピンチ幅
        //center: ピンチの2本指の中心の座標
        public void OnPinchStart(float width, Vector2 center)
        {
            //Not used
        }


        //Pinch to operate distance (for mobile)
        //(*) use PinchInput.OnPinch callback handler
        //http://fantom1x.blog130.fc2.com/blog-entry-288.html
        //width: distance of two fingers of pinch
        //delta: The difference in pinch width just before
        //ratio: Stretch ratio from the start of pinch width (1:At the start of pinch, Expand by 1 or more, lower than 1 (1/2, 1/3, ...)
        //
        //ピンチで距離を操作（モバイル等）
        //http://fantom1x.blog130.fc2.com/blog-entry-288.html
        //・PinchInput.OnPinch のコールバックハンドラ
        //width: ピンチ幅
        //delta: 直前のピンチ幅の差
        //ratio: ピンチ幅の開始時からの伸縮比 (1:ピンチ開始時, 1以上拡大, 1より下(1/2,1/3,...)縮小)
        public void OnPinch(float width, float delta, float ratio)
        {
            if (!distanceOperation.pinchEnable)
                return;

            if (delta != 0)
            {
                wantedDistance = Mathf.Max(distanceOperation.min, distance - delta * distanceOperation.pinchSpeed);
                pinched = true;
            }
        }


        //Swipe to operate turn
        //(*) SwipeInput.OnSwipe callback handler
        //http://fantom1x.blog130.fc2.com/blog-entry-250.html
        //
        //スワイプで旋回
        //・SwipeInput.OnSwipe のコールバックハンドラ
        //http://fantom1x.blog130.fc2.com/blog-entry-250.html
        public void OnSwipe(Vector2 dir)
        {
            if (!turnOperation.swipeEnable)
                return;

            if (dir == Vector2.left)
                TurnLeft();
            else if (dir == Vector2.right)
                TurnRight();
        }


        //Turn left operation (constant angle rotation)
        //左旋回（一定角度旋回）
        public void TurnLeft()
        {
            angle = Mathf.Repeat(MultipleCeil(angle - turnOperation.angle, turnOperation.angle), 360f);
        }

        //Turn right operation (constant angle rotation)
        //右旋回（一定角度旋回）
        public void TurnRight()
        {
            angle = Mathf.Repeat(MultipleFloor(angle + turnOperation.angle, turnOperation.angle), 360f);
        }

#endregion Pinch, Swipe operation Section

#region Other static method Section

        //Calculate a smaller multiple
        //より小さい倍数を求める（倍数で切り捨てられるような値）
        //http://fantom1x.blog130.fc2.com/blog-entry-248.html
        static float MultipleFloor(float value, float multiple)
        {
            return Mathf.Floor(value / multiple) * multiple;
        }

        //Calculate a larger multiple
        //より大きい倍数を求める（倍数で繰り上がるような値）
        static float MultipleCeil(float value, float multiple)
        {
            return Mathf.Ceil(value / multiple) * multiple;
        }

        //Angle between direction vectors in 2D (XY plane) with sign (degrees)
        //2D（XY平面）での方向ベクトル同士の角度を符号付きで返す（度）
        //http://fantom1x.blog130.fc2.com/blog-entry-253.html#AngleWithSign
        static float AngleXZWithSign(Vector3 from, Vector3 to)
        {
            Vector3 projFrom = from;
            Vector3 projTo = to;
            projFrom.y = projTo.y = 0;      //Ignore y axis (project on XZ plane)   //y軸を無視する（XZ平面に投影する）
            float angle = Vector3.Angle(projFrom, projTo);
            float cross = CrossXZ(projFrom, projTo);
            return (cross != 0) ? angle * -Mathf.Sign(cross) : angle;   //Invert the sign of the 2D outer product   //2D外積の符号を反転する
        }

        //Outer product in 2D (XY plane)
        //2Dでの外積を求める（XY平面）
        //http://fantom1x.blog130.fc2.com/blog-entry-253.html#Cross2D
        static float CrossXZ(Vector3 a, Vector3 b)
        {
            return a.x * b.z - a.z * b.x;
        }

#endregion Other static method Section
    }
}