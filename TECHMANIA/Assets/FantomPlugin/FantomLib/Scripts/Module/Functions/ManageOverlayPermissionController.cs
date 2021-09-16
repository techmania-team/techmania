using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Manage Overlay Permission Controller (API 23 or higher)
    /// 
    ///･Open Manage Overlay Permission settings.
    ///
    ///・フローティング表示するアプリ権限設定画面を開く。
    /// </summary>
    public class ManageOverlayPermissionController : ActionURIOnThisPackageBase
    {
        protected override string action {
            get { return "android.settings.action.MANAGE_OVERLAY_PERMISSION"; }
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