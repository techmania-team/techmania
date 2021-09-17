using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FantomLib
{
    /// <summary>
    /// Use Image.fillAmount as progress
    /// 
    /// Image.fillAmount を進捗として操作する
    /// </summary>
    public class ProgressImageFill : MonoBehaviour
    {
        [Range(0, 1)] public float preAmount = 0;       //fillAmount on startup/reset           //起動/リセット時の fillAmount
        [Range(0, 1)] public float startAmount = 0;     //fillAmount at start of progress       //進捗開始時の fillAmount
        [Range(0, 1)] public float completeAmount = 1;  //fillAmount at completion of progress  //進捗完了時の fillAmount
        public bool delayedResetOnComplete = true;      //Call delayed reset on completion      //完了時に遅延リセットを実行
        public float resetDelay = 0.1f;                 //Delayed reset time                    //遅延リセットの時間

        public Image fillImage;                         //Image for operate fillAmount          //fillAmount を操作する Image


        // Use this for initialization
        private void Start()
        {
            if (fillImage != null)
                fillImage.fillAmount = preAmount;
        }

        // Update is called once per frame
        //private void Update()
        //{

        //}

        public void OnStart()
        {
            if (fillImage != null)
                fillImage.fillAmount = startAmount;
        }

        public void OnProgress(float amount)
        {
            if (fillImage != null)
                fillImage.fillAmount = amount;
        }

        public void OnComplete()
        {
            if (fillImage != null)
                fillImage.fillAmount = completeAmount;

            if (delayedResetOnComplete)
                Invoke("OnReset", resetDelay);
        }

        public void OnReset()
        {
            if (fillImage != null)
                fillImage.fillAmount = preAmount;
        }
    }
}