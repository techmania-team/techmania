using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FantomLib
{
    /// <summary>
    /// Application exit with key input (with twice push within the time limit), Android Toast displays the pressed state
    /// 
    /// キー入力で終了する（Android の Toast でメッセージ表示）
    /// </summary>
    public class AppExitWithToast : AppExitWithKey
    {
        //Inspector Settings
        public bool showOneMoreMessage = true;
        public LocalizeString oneMoreMessage = new LocalizeString(
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.Japanese, "もう一度押すと終了します。"),
                new LocalizeString.Data(SystemLanguage.English, "Press again to exit."),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "再次按下即可退出。"),
                new LocalizeString.Data(SystemLanguage.Korean, "종료하려면 다시 누릅니다."),
            });


        public bool showExitMessage = false;
        public LocalizeString exitMessage = new LocalizeString(
            new List<LocalizeString.Data>()
            {
                new LocalizeString.Data(SystemLanguage.Japanese, "アプリケーションを終了します。"),
                new LocalizeString.Data(SystemLanguage.English, "Exit the application."),
                new LocalizeString.Data(SystemLanguage.ChineseSimplified, "退出应用程序。"),
                new LocalizeString.Data(SystemLanguage.Korean, "응용 프로그램을 종료합니다."),
            });



        // Use this for initialization
        protected new void Start()
        {
            base.Start();

            oneMoreMessage.Initialize();    //Apply inspector registration. //インスペクタ登録を反映
            exitMessage.Initialize();       //Apply inspector registration. //インスペクタ登録を反映

            //Register itself when it is empty
            //独自登録されてないとき、自身を登録する（※インスペクタには表示されないので注意）
            if (showOneMoreMessage && OnFirstPressed.GetPersistentEventCount() == 0)
            {
#if UNITY_EDITOR
                Debug.Log("OnFirstPressed added ShowOneMoreToast (auto)");
#endif
                OnFirstPressed.AddListener(ShowOneMoreToast);
            }

            if (showExitMessage && OnSecondPressed.GetPersistentEventCount() == 0)
            {
#if UNITY_EDITOR
                Debug.Log("OnSecondPressed added ShowExitToast (auto)");
#endif
                OnSecondPressed.AddListener(ShowExitToast);
            }
        }

        //When "Press again to exit." Toast
        //「もう一度押すと終了します。」のときの Toast
        public void ShowOneMoreToast()
        {
            if (!showOneMoreMessage)
                return;

#if UNITY_EDITOR
            Debug.Log("ShowOneMoreToast called");
#elif UNITY_ANDROID
            string text = oneMoreMessage.Text;
            if (!string.IsNullOrEmpty(text))
                AndroidPlugin.ShowToast(text);
#endif
        }

        //When "Exit the application." Toast (*) When using it you better put a time to display a bit with exitDelay
        //「アプリケーションを終了します。」のときの Toast（デフォでは使用してない。使用するときは exitDelay で少し表示する時間を入れた方が良い）
        public void ShowExitToast()
        {
            if (!showExitMessage)
                return;

#if UNITY_EDITOR
            Debug.Log("ShowExitToast called");
#elif UNITY_ANDROID
            string text = exitMessage.Text;
            if (!string.IsNullOrEmpty(text))
                AndroidPlugin.ShowToast(text);
#endif
        }

        //Wait for the specified time and then exit. (For calling "OnExit()")
        //Ignore WebGL etc. (Since we only have to close the browser)
        //指定時間待機してから終了。エディタの場合はプレイモードの終了。（※OnExit() から呼び出し用）
        //WebGL等は無視（ブラウザを閉じるしかないので）
        protected override IEnumerator WaitAndExit(float sec)
        {
            if (OnBeforeDelay != null)
                OnBeforeDelay.Invoke();

            yield return new WaitForSeconds(sec);

            if (OnBeforeExit != null)
                OnBeforeExit.Invoke();

#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidPlugin.CancelToast();    //(*) Since the Toast tends to remain long on the screen, it disappears here.    //トーストが画面に長く残る傾向があるので、ここで消す
#endif

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; //For Editor //Editorではプレイを停止
#elif !UNITY_WEBGL && !UNITY_WEBPLAYER
            Application.Quit();
#endif
            done = true;
        }
    }
}
