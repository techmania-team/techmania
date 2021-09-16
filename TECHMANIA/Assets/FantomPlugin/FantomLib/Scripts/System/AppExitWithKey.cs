using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Application exit with key input (with twice push within the time limit)
    /// 
    /// キー入力で終了する（制限時間内に２回押しで終了機能付き）
    /// </summary>
    public class AppExitWithKey : MonoBehaviour
    {
        public bool enableKey = true;               //true = use key                                        //キーを有効にする
        public KeyCode exitKey = KeyCode.Escape;    //Key code to finish                                    //終了するキー

        public bool oneMoreConfirm = false;         //'Press again' mode (press twice to exit)              //「もう一度押す」モードにする（２回押しで終了）
        public float oneMoreDuration = 3.0f;        //Time limit of 'Press again'                           //「もう一度押す」の長さ（制限時間）
        public float exitDelay = 0.0f;              //Exit execution delay (Reasonable until 3.0 seconds)   //終了実行ディレイ（3.0秒くらいまでが妥当）


        //n回目キー押下コールバック
        public UnityEvent OnFirstPressed;           //First time press                                      //１回目を押したとき
        public UnityEvent OnSecondPressed;          //Second time press                                     //２回目を押したとき

        //終了直前のイベントコールバック
        public UnityEvent OnBeforeDelay;            //Callback when just before waiting                     //待機前に呼び出されるコールバック
        public UnityEvent OnBeforeExit;             //Callback when just before exit                        //終了前に呼び出されるコールバック


        //Local Values
        protected bool pressed = false;             //First time press flag                                 //１回目押下した
        protected float limitTime;                  //Second pressing time limit                            //２回目押下制限時間（この時間以降は無効）
        protected bool done = false;                //Key input done (For double prevention)                //キー入力実行済み（２重防止用）



        // Use this for initialization
        protected void Start()
        {

        }


        // Update is called once per frame
        protected void Update()
        {
            if (enableKey && !done)
            {
                if (Input.GetKeyDown(exitKey))
                {
                    if (oneMoreConfirm)
                    {
                        if (!pressed) //First time press    //1回目押下
                        {
                            pressed = true;
                            limitTime = Time.time + oneMoreDuration;

                            if (OnFirstPressed != null)
                                OnFirstPressed.Invoke();
                        }
                        else //Second time press    //2回目押下
                        {
                            if (Time.time < limitTime)  //Valid if it is within the time limit  //制限時間内なら有効
                            {
                                done = true;

                                if (OnSecondPressed != null)
                                    OnSecondPressed.Invoke();

                                OnExit();
                            }
                        }
                    }
                    else //When it exit only once   //1回のみで終了のとき
                    {
                        done = true;
                        OnExit();
                    }
                }

                if (limitTime <= Time.time)     //Reset after time limit    //制限時間を過ぎたらリセット
                    pressed = false;
            }
        }


        protected Coroutine coroutine = null; //For double prevention   //2重防止

        //For calling from outside (Exit is made unique)
        //外部から呼び出し用（終了はユニークにする）
        public void OnExit()
        {
            if (coroutine == null)
                coroutine = StartCoroutine(WaitAndExit(exitDelay > 0 ? exitDelay : 0));
        }


        //Wait for the specified time and then exit (For calling "OnExit()").
        //Ignore WebGL etc. (Since we only have to close the browser)
        //
        //指定時間待機してから終了。エディタの場合はプレイモードの終了（※OnExit() から呼び出し用）。
        //WebGL等は無視（ブラウザを閉じるしかないので）
        protected virtual IEnumerator WaitAndExit(float sec)
        {
            if (OnBeforeDelay != null)
                OnBeforeDelay.Invoke();

            yield return new WaitForSeconds(sec);

            if (OnBeforeExit != null)
                OnBeforeExit.Invoke();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false; //For editor   //エディタではプレイを停止
#elif !UNITY_WEBGL && !UNITY_WEBPLAYER
            Application.Quit();
#endif
            done = true;    //Just in cas   //念のため
        }
    }

}