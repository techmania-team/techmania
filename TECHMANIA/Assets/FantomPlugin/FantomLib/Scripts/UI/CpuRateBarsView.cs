using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FantomLib
{
    using BarType = CpuRateBar.BarType;

    /// <summary>
    /// Update bars with information on CPU 0 ~ n utilization
    /// 
    /// CPU 0～n 使用率の情報でバーを更新する
    /// </summary>
    public class CpuRateBarsView : MonoBehaviour {

        //Inspector Settings
        public bool applySettingOnAwake = false;    //Apply settings at startup     //起動時に設定を適用する

        public BarType barType = BarType.Each;      //Appearance of the bar         //バーの外観

        public Color userColor;                     //User Bar color when 'Each' (when changing with%)      //Each のときのバー色（%で変化するとき）
        public Color niceColor;                     //Nice Bar color when 'Each' (when changing with%)      //Each のときのバー色（%で変化するとき）
        public Color systemColor;                   //System Bar color when 'Each' (when changing with%)    //Each のときのバー色（%で変化するとき）
        public Color idleColor;                     //Idle Bar color (when changing with%)                  //バー色（%で変化するとき）

        public Gradient useGradColor;               //User Bar color when 'UseGrad' (when changing with%)   //'UseGrad' のときのバー色（%で変化するとき）

        public CpuRateBar[] cpuRateBars;            //CPU 0 ~ n 


        // Use this for initialization
        private void Awake () {
            if (applySettingOnAwake)
                ApplySetting();
        }

        private void Start () {

        }
        
        // Update is called once per frame
        //private void Update () {
            
        //}


        //Register 'CpuRateController.OnStatus'
        public void RecieveCpuRates(CpuRateInfo[] infos)
        {
            for (int i = 0; i < cpuRateBars.Length; i++)
            {
                if (cpuRateBars[i] == null)
                    continue;

                if (i < infos.Length)
                {
                    cpuRateBars[i].gameObject.SetActive(true);
                    cpuRateBars[i].SetCpuRate(infos[i]);
                }
                else
                {
                    cpuRateBars[i].gameObject.SetActive(false);
                }
            }
        }

        //When 'applySettingOnAwake = true'
        private void ApplySetting()
        {
            for (int i = 0; i < cpuRateBars.Length; i++)
            {
                if (cpuRateBars[i] == null)
                    continue;

                cpuRateBars[i].barType = barType;

                switch (barType)
                {
                    case BarType.Each:
                        cpuRateBars[i].UserBarColor = userColor;
                        cpuRateBars[i].NiceBarColor = niceColor;
                        cpuRateBars[i].SystemBarColor = systemColor;
                        cpuRateBars[i].IdleBarColor = idleColor;
                        break;
                    case BarType.UseGrad:
                        cpuRateBars[i].useGradColor = useGradColor;
                        cpuRateBars[i].IdleBarColor = idleColor;
                        break;
                }
            }
        }
    }
}
