using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Manage Write Controller (API 23 or higher)
    /// 
    ///･Open Manage Write Permission settings.
    ///
    ///・システム設定を変更できるアプリ権限設定画面を開く。
    /// </summary>
    public class ManageWriteController : ActionURIOnThisPackageBase
    {
        protected override string action {
            get { return "android.settings.action.MANAGE_WRITE_SETTINGS"; }
        }


        //Inspector settings
        public bool thisApp = true;     //false = all App


        public override void StartAction()
        {
            if (thisApp)
                base.StartAction();
            else
            {
#if UNITY_EDITOR
                Debug.Log(name + ".Show");
#elif UNITY_ANDROID
                AndroidPlugin.StartAction(action);
#endif
            }
        }
    }
}
