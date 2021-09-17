using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Ignore Background Data Restrictions Settings Controller (API 24 or higher)
    /// 
    ///･Open this Ignore Background Data Restrictions settings.
    ///
    ///・現在アプリのバックグラウンドデータの制限を無視する設定画面を開く。
    /// </summary>
    public class IgnoreBackgroundDataRestrictionsSettingsController : ActionURIOnThisPackageBase
    {
        protected override string action {
            get { return "android.settings.IGNORE_BACKGROUND_DATA_RESTRICTIONS_SETTINGS"; }
        }

    }
}
