using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace FantomLib
{
    /// <summary>
    /// Display FPS measurement value on UI-Text
    /// http://fantom1x.blog130.fc2.com/blog-entry-307.html
    /// 
    /// FPS 測定値を UI-Text に表示する
    /// http://fantom1x.blog130.fc2.com/blog-entry-307.html
    /// </summary>
    public class FpsText : MonoBehaviour {

        //Inspector Settings
        public Text targetText;                     //Display UI-Text       //表示する UI-Text
        public string format = "{0:F1} FPS";        //The display format (if it is 'F0', there are no decimal places)    //表示フォーマット（'F0' とすれば小数点以下は無くなる）

        //For measurement   //測定用
        int tick = 0;                               //Number of frames      //フレーム数
        float elapsed = 0;                          //Elapsed time          //経過時間
        float fps = 0;                              //Frame rate            //フレームレート

        //For work
        StringBuilder sb = new StringBuilder(16);


        // Use this for initialization
        private void Awake () {
            if (targetText == null)
                targetText = GetComponentInChildren<Text>();
        }

        private void Start () {
            
        }

        // Update is called once per frame
        private void Update () {
            tick++;
            elapsed += Time.deltaTime;
            if (elapsed >= 1f) {
                fps = tick / elapsed;
                tick = 0;
                elapsed = 0;

                if (targetText != null)
                {
                    sb.Length = 0;
                    sb.AppendFormat(format, fps);
                    targetText.text = sb.ToString();
                }
            }
        }
    }
}
