using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FantomLib
{
    /// <summary>
    /// Pinch to operate distance
    /// (*) use PinchInput callbacks
    /// http://fantom1x.blog130.fc2.com/blog-entry-288.html
    /// (Usage)
    ///·You can use it by attaching to a GameObject such as a camera and registering PinchInput callbacks from the inspector. 
    ///·Distance is a straight line distance from target.
    /// 
    /// ピンチで距離を操作する
    /// http://fantom1x.blog130.fc2.com/blog-entry-288.html
    ///（使い方）
    ///・カメラなどの GameObject にアタッチして、インスペクタから PinchInput のコールバックを登録すれば使用可。
    ///・距離は target からの直線距離となる。
    /// </summary>
    public class PinchToDistance : MonoBehaviour
    {
#region Inspector settings Section

        //Inspector Settings
        public Transform target;            //Object to be a viewpoint          //視点となるオブジェクト
        public float speed = 2f;            //Rate of change                    //変化速度
        public float minDistance = 1.0f;    //Minimum distance to approach      //近づける最小距離
        public bool lookAt = true;          //Look at the object                //オブジェクトの方を向く

        //LocalValues
        float initDistance;                 //Initial distance (for reset)      //起動初期距離（リセット用）

#endregion Inspector settings Section

#region Unity life cycle Section

        // Use this for initialization
        private void Start()
        {
            if (target != null)
            {
                Vector3 dir = target.position - transform.position;
                initDistance = dir.magnitude;
                if (lookAt)
                    transform.LookAt(target.position);
            }
        }

        // Update is called once per frame
        //private void Update()
        //{

        //}

#endregion Unity life cycle Section

#region Pinch operation Section

        //width: distance of two fingers of pinch
        //center: The coordinates of the center of two fingers of pinch
        //
        //width: ピンチ幅
        //center: ピンチの2本指の中心の座標
        public void OnPinchStart(float width, Vector2 center)
        {
            //Not used
        }

        //width: distance of two fingers of pinch
        //delta: The difference in pinch width just before
        //ratio: Stretch ratio from the start of pinch width (1:At the start of pinch, Expand by 1 or more, lower than 1 (1/2, 1/3, ...)
        //
        //width: ピンチ幅
        //delta: 直前のピンチ幅の差
        //ratio: ピンチ幅の開始時からの伸縮比(1:ピンチ開始時, 1以上拡大, 1より下(1/2,1/3,...)縮小)
        public void OnPinch(float width, float delta, float ratio)
        {
            if (target == null)
                return;

            Vector3 dir = target.position - transform.position;
            float distance = Math.Max(minDistance, dir.magnitude - delta * speed);
            Vector3 pos = target.position - dir.normalized * distance;
            transform.position = pos;
            if (lookAt)
                transform.LookAt(target.position);
        }

        //Restore the initial distance
        //初期の距離に戻す
        public void ResetDistance()
        {
            if (target == null)
                return;

            Vector3 dir = target.position - transform.position;
            Vector3 pos = target.position - dir.normalized * initDistance;
            transform.position = pos;
            if (lookAt)
                transform.LookAt(target.position);
        }

#endregion Pinch operation Section
    }
}