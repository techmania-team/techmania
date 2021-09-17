using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace FantomLib
{
    /// <summary>
    /// Display temperature of battery status in text
    /// 
    /// バッテリーステータスの温度をテキストに表示する
    /// </summary>
    public class BatteryTemperatureText : MonoBehaviour {

        //Inspector Settings
        public Text targetText;                  //Display UI-Text                   //表示する UI-Text
        public string format = "{0:F1} ℃";      //The display format (if it is 'F0', there are no decimal places)    //表示フォーマット（'F0' とすれば小数点以下は無くなる）

        //For work
        StringBuilder sb = new StringBuilder(8);


        // Use this for initialization
        private void Start () {
            
        }
        
        // Update is called once per frame
        //private void Update () {
            
        //}


        //Callback handler from 'BatteryStatusController.OnStatus'
        public void ReceiveBatteryStatus(BatteryInfo info)
        {
            if (targetText != null)
            {
                sb.Length = 0;
                sb.AppendFormat(format, info.temperature);
                targetText.text = sb.ToString();
            }
        }
    }
}
