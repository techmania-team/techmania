using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FantomLib
{
    /// <summary>
    /// Change the scale with a pinch (local scale)
    /// http://fantom1x.blog130.fc2.com/blog-entry-288.html
    /// (Usage)
    ///･Attach to the GameObject that you want to stretch and then use it if you register PinchInput callbacks from the inspector 
    /// 
    /// ピンチでスケールを変化させる（ローカルスケール）
    /// http://fantom1x.blog130.fc2.com/blog-entry-288.html
    ///（使い方）
    ///・伸縮したい GameObject にアタッチして、インスペクタから PinchInput のコールバックを登録すれば使用可。
    /// </summary>
    public class PinchToScale : MonoBehaviour
    {
#region Inspector settings and Local values Section

        //Inspector Settings
        public Transform target;    //Object to be changed in scale     //スケール変化させるオブジェクト

        //Local Values
        Vector3 startScale;         //Scale at pinch start              //ピンチ開始時スケール
        Vector3 initScale;          //Initial scale (for reset)         //起動初期スケール（リセット用）

#endregion Inspector settings and Local values Section

#region Unity life cycle Section

        // Use this for initialization
        private void Start()
        {
            if (target == null)
                target = gameObject.transform;  //指定がないときは自身を対象とする

            initScale = target.localScale;
        }

        // Update is called once per frame
        //private void Update () {

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
            if (target != null)
                startScale = target.localScale;
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
            if (target != null)
                target.localScale = startScale * ratio;
        }

        //Restore the initial scale
        //スケールを元に戻す
        public void ResetScale()
        {
            if (target != null)
                target.localScale = initScale;
        }

#endregion Pinch operation Section
    }
}