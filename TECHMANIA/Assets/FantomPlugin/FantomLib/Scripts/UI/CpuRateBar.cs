using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FantomLib
{
    /// <summary>
    /// Display CPU Rate information (User, Nice, System, Idle) in a bar
    /// 
    /// CPU 使用率の情報（User, Nice, System, Idle）をバーで表示する
    /// </summary>
    public class CpuRateBar : MonoBehaviour {

        public enum BarType
        {
            Each,       //User, Nice, System, Idle
            UseGrad,    //(User + Nice + System), Idle
        }

        //Inspector Settings
        public BarType barType = BarType.Each;  //Appearance of the bar

        public Text nameText;                   //"CPU~"

        public Image userBarImage;              //User bar
        public Image niceBarImage;              //Nice bar
        public Image systemBarImage;            //System bar
        public Image idleBarImage;              //Idle bar

        public Gradient useGradColor;           //User Bar color when 'UseGrad' (when changing with%)   //'UseGrad' のときのバー色（%で変化するとき）


        // Use this for initialization
        private void Start () {
            UserBarFill = 0f;
            NiceBarFill = 0f;
            SystemBarFill = 0f;
            IdleBarFill = 0f;
        }
        
        // Update is called once per frame
        //private void Update () {
            
        //}

        
        //'CpuRateInfo' to bar
        public void SetCpuRate(CpuRateInfo info)
        {
            if (nameText != null)
                nameText.text = info.name.ToUpper() + " : ";

            switch (barType)
            {
                case BarType.Each:
                    UpdateForEashTye(info);
                    break;
                case BarType.UseGrad:
                    UpdateForUseGradTye(info);
                    break;
            }
        }

        //'Each' mode
        private void UpdateForEashTye(CpuRateInfo info)
        {
            float total = info.user;
            UserBarFill = total / 100f;

            total += info.nice;
            NiceBarFill = total / 100f;

            total += info.system;
            SystemBarFill = total / 100f;

            IdleBarFill = 1f;
        }

        //'UseGrad' mode
        private void UpdateForUseGradTye(CpuRateInfo info)
        {
            NiceBarFill = 0f;
            SystemBarFill = 0f;
            IdleBarFill = 1f;

            float rate = (info.user + info.nice + info.system) / 100f;
            UserBarFill = rate;
                
            if (useGradColor != null)
                UserBarColor = useGradColor.Evaluate(rate);
        }


        //User bar fill (0~1f)
        float UserBarFill {
            set {
                if (userBarImage != null)
                    userBarImage.fillAmount = Mathf.Clamp01(value);
            }
        }

        //Nice bar fill (0~1f)
        float NiceBarFill {
            set {
                if (niceBarImage != null)
                    niceBarImage.fillAmount = Mathf.Clamp01(value);
            }
        }

        //System bar fill (0~1f)
        float SystemBarFill {
            set {
                if (systemBarImage != null)
                    systemBarImage.fillAmount = Mathf.Clamp01(value);
            }
        }

        //Idle bar fill (0~1f)
        float IdleBarFill {
            set {
                if (idleBarImage != null)
                    idleBarImage.fillAmount = Mathf.Clamp01(value);
            }
        }


        //User bar color
        public Color UserBarColor {
            set {
                if (userBarImage != null)
                    userBarImage.color = value;
            }
        }

        //Nice bar color
        public Color NiceBarColor {
            set {
                if (niceBarImage != null)
                    niceBarImage.color = value;
            }
        }

        //System bar color
        public Color SystemBarColor {
            set {
                if (systemBarImage != null)
                    systemBarImage.color = value;
            }
        }

        //Idle bar color
        public Color IdleBarColor {
            set {
                if (idleBarImage != null)
                    idleBarImage.color = value;
            }
        }
    }
}