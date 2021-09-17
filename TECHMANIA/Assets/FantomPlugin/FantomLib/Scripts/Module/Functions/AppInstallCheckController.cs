using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// App Install Check Controller
    /// 
    /// Check if the package exists on the Android system. 
    /// When it exists (= installed), it returns the localized application name, version number, version name.
    /// You can not get packages that are not published to the Android system (always return false).
    ///･Information to be acquired is as follows:
    /// ･Localized application name (only if set in the application)
    /// ･Version code (Internal number that always increments [integer value])
    /// ･Version name (Character string used as the version [number] to be displayed to the user)
    /// https://developer.android.com/studio/publish/versioning.html#appversioning
    /// 
    /// 
    /// Androidシステムにパッケージが存在するかを確認する。
    /// 存在する（＝インストールされている）ときは、ローカライズされたアプリ名、バージョン番号、バージョン名を返す。
    /// Androidシステムに公開されてないパッケージは取得できない（常にfalseが返る）。
    ///・取得される情報は以下の通り
    ///　・ローカライズされたアプリ名（アプリで設定されてる場合のみ）
    ///　・アプリのバージョン番号（常にインクリメントする内部番号[整数値]）
    ///　・アプリのバージョン名（ユーザーに表示するバージョン[番号]として使用される文字列）
    /// https://developer.android.com/studio/publish/versioning.html#appversioning
    /// </summary>
    public class AppInstallCheckController : MonoBehaviour
    {
        //Inspector Settings
        public string packageName = "com.android.vending";  //Package name (Applicatin ID) to check

        public bool checkOnStart = false;   //Execute check automatically at 'Start()'

        //Callbacks
        [Serializable] public class InstalledHandler : UnityEvent<string, int, string> { }    //app name, version code, version name
        public InstalledHandler OnInstalled;      //When installed

        public UnityEvent OnNotInstalled;   //When not installed



        // Use this for initialization
        private void Start()
        {
            if (checkOnStart)
                CheckInstall();
        }

        // Update is called once per frame
        //private void Update()
        //{

        //}


        
        //Check if the package exists on the Android system.
        public void CheckInstall()
        {
            if (string.IsNullOrEmpty(packageName))
                return;

#if UNITY_EDITOR
            Debug.Log("AppInstallCheckController.CheckInstall called.");
#elif UNITY_ANDROID
            if(AndroidPlugin.IsExistApplication(packageName))
            {
                string appName = AndroidPlugin.GetApplicationName(packageName);
                int verCode = AndroidPlugin.GetVersionCode(packageName);
                string verName = AndroidPlugin.GetVersionName(packageName);
                if (OnInstalled != null)
                    OnInstalled.Invoke(appName, verCode, verName);
            }
            else
            {
                if (OnNotInstalled != null)
                    OnNotInstalled.Invoke();
            }
#endif
        }

        //Set packageName dynamically and check install (current packageName will be overwritten)
        public void CheckInstall(string packageName)
        {
            this.packageName = packageName;
            CheckInstall();
        }
    }
}
