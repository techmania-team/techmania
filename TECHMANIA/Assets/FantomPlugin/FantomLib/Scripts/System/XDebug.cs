using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace FantomLib
{
    /// <summary>
    /// Debugging log for runtime system (singleton)
    /// 
    /// 実行環境用デバッグコンソール（シングルトン）
    /// </summary>
    public class XDebug : SingletonBehaviour<XDebug>
    {
#region Inspector settings Section

        //Inspector Settings
        public bool hideOnAwake = false;    //Excute 'Hide()' with 'Awake()'        //Awake() で Hide() を実行する
        public bool clearOnDestory = true;  //Execute 'Clear()' with 'OnDestory()'. //OnDestory() でクリアを実行する

        public int lines = 0;               //Limit number of lines (Queue) for text buffer (0: no limit)   //テキストのバッファ（キュー）の制限行数（0 で無し）
        private static Queue lineQueue;     //Text buffer (each line)               //テキストのバッファ（改行ごと）
        private static StringBuilder sb;    //For join                              //テキストのバッファ結合用

        public Text outputText;             //Log output UI-Text                    //ログ出力する UI-Text
        public Scrollbar scrollbar;         //Log output UI-Scrollbar               //コンソールの UI-Scrollbar（自動スクロール用）
        public CanvasGroup canvasGroup;     //Show / hide with alpha                //アルファで表示/非表示を切り替える

#endregion Inspector settings Section

#region Unity life cycle Section

        // Use this for initialization
        protected new void Awake()
        {
            base.Awake();

            if (hideOnAwake)
                Hide();

            if (lines > 0 && lineQueue == null)
            {
                lineQueue = new Queue(lines);
                sb = new StringBuilder(128 * lines);
            }
        }

        protected void OnDestroy()
        {
            StopAllCoroutines();

            if (clearOnDestory)
                Clear(0);
        }

        protected void Start()
        {
        }

        // Update is called once per frame
        //protected void Update()
        //{
        //}

#endregion Unity life cycle Section

#region Internal processing method Section

        //Make the scroll bar last (always display the last text)
        //(*) Note that it will not be affected unless it is called after 1 frame.
        // (If long length of strings delay it for a few frames will go well)
        //
        //スクロールバーを最後にする（最後のテキストを常に表示させる）
        //※１フレーム後に呼び出さないと反映されないので注意
        // （文字数が多いときは WaitFrames() で数フレーム遅延させると上手く行く）
        public void ScrollLast()
        {
            if (scrollbar != null)
                scrollbar.value = 0;
        }

        //Wait n frames for action
        //ｎフレーム待ってから実行
        private IEnumerator WaitFrames(Action action, int count)
        {
            count = Math.Max(0, count);
            while (--count >= 0)
                yield return null;

            action();
        }

        //Wait n seconds for action
        //ｎ秒待ってから実行
        private IEnumerator WaitSeconds(Action action, float sec)
        {
            yield return new WaitForSeconds(sec);
            action();
        }


        //Display log
        //以下ログ出力
        const int DEF_WAIT_FRAMES = 3;  //Automatic scrolling goes well if it is a few frames.  //デフォルトの待ちフレーム数

        //Display text (Join each line when limit number of lines)
        //テキストの出力（行数制限がある場合は文字列バッファを結合する）
        private static void OutputText(object mes, bool newline = true)
        {
            if (Instance.outputText)
            {
                if (Instance.lines > 0 && lineQueue != null)
                {
                    lineQueue.Enqueue(mes + (newline ? "\n" : ""));
                    while (lineQueue.Count > Instance.lines)
                        lineQueue.Dequeue();

                    sb.Length = 0;
                    foreach (var item in lineQueue)
                        sb.Append(item);

                    Instance.outputText.text = sb.ToString();
                }
                else
                {
                    Instance.outputText.text += mes + (newline ? "\n" : "");
                }
            }
        }

        //Wait n frames and display log
        //ｎフレーム遅延でのテキスト出力
        private static void OutputTextDelayedFrames(object mes, int delayedFrames = DEF_WAIT_FRAMES, bool newline = true)
        {
            if (Instance.outputText)
            {
                OutputText(mes, newline);
                Instance.StartCoroutine(Instance.WaitFrames(() => Instance.ScrollLast(), delayedFrames));
            }
        }

        //Wait n seconds and display log
        //ｎ秒数遅延でのテキスト出力
        private static void OutputTextDelayedSeconds(object mes, float delayedSeconds, bool newline = true)
        {
            if (Instance.outputText)
            {
                OutputText(mes, newline);
                Instance.StartCoroutine(Instance.WaitSeconds(() => Instance.ScrollLast(), delayedSeconds));
            }
        }

#endregion

#region Static method for Code Section

        //Log
        public static void Log(object mes, bool newline)
        {
            Log(mes, DEF_WAIT_FRAMES, newline);
        }

        public static void Log(object mes, int delayedFrames = DEF_WAIT_FRAMES, bool newline = true)
        {
            if (HasICollection(mes))
            {
                string str = Join((ICollection)mes);
                Debug.Log(str);
                OutputTextDelayedFrames(str, delayedFrames, newline);
            }
            else
            {
                Debug.Log(mes);
                OutputTextDelayedFrames(mes, delayedFrames, newline);
            }
        }

        public static void Log(object mes, float delayedSeconds, bool newline = true)
        {
            if (HasICollection(mes))
            {
                string str = Join((ICollection)mes);
                Debug.Log(str);
                OutputTextDelayedSeconds(str, delayedSeconds, newline);
            }
            else
            {
                Debug.Log(mes);
                OutputTextDelayedSeconds(mes, delayedSeconds, newline);
            }
        }

        public static void LogFormat(string format, params object[] args)
        {
            Log(string.Format(format, args));
        }



        //LogWarging
        const string WarningPrefix = "Warning: ";
        
        public static void LogWarning(object mes, bool newline)
        {
            LogWarning(mes, DEF_WAIT_FRAMES, newline);
        }

        public static void LogWarning(object mes, int delayedFrames = DEF_WAIT_FRAMES, bool newline = true)
        {
            if (HasICollection(mes))
            {
                string str = Join((ICollection)mes);
                Debug.LogWarning(str);
                OutputTextDelayedFrames(WarningPrefix + str, delayedFrames, newline);
            }
            else
            {
                Debug.LogWarning(mes);
                OutputTextDelayedFrames(WarningPrefix + mes, delayedFrames, newline);
            }
        }

        public static void LogWarning(object mes, float delayedSeconds, bool newline = true)
        {
            if (HasICollection(mes))
            {
                string str = Join((ICollection)mes);
                Debug.LogWarning(str);
                OutputTextDelayedSeconds(WarningPrefix + str, delayedSeconds, newline);
            }
            else
            {
                Debug.LogWarning(mes);
                OutputTextDelayedSeconds(WarningPrefix + mes, delayedSeconds, newline);
            }
        }

        public static void LogWarningFormat(string format, params object[] args)
        {
            LogWarning(string.Format(format, args));
        }



        //LogError
        const string ErrorPrefix = "Error: ";

        public static void LogError(object mes, bool newline)
        {
            LogError(mes, DEF_WAIT_FRAMES, newline);
        }

        public static void LogError(object mes, int delayedFrames = DEF_WAIT_FRAMES, bool newline = true)
        {
            if (HasICollection(mes))
            {
                string str = Join((ICollection)mes);
                Debug.LogError(str);
                OutputTextDelayedFrames(ErrorPrefix + str, delayedFrames, newline);
            }
            else
            {
                Debug.LogError(mes);
                OutputTextDelayedFrames(ErrorPrefix + mes, delayedFrames, newline);
            }
        }

        public static void LogError(object mes, float delayedSeconds, bool newline = true)
        {
            if (HasICollection(mes))
            {
                string str = Join((ICollection)mes);
                Debug.LogError(str);
                OutputTextDelayedSeconds(ErrorPrefix + str, delayedSeconds, newline);
            }
            else
            {
                Debug.LogError(mes);
                OutputTextDelayedSeconds(ErrorPrefix + mes, delayedSeconds, newline);
            }
        }

        public static void LogErrorFormat(string format, params object[] args)
        {
            LogError(string.Format(format, args));
        }



        //Clear
        public static void Clear()
        {
            Clear(DEF_WAIT_FRAMES);
        }

        public static void Clear(int delayedFrames)
        {
            if (Instance.outputText)
            {
                if (lineQueue != null)
                {
                    lineQueue.Clear();
                    sb.Length = 0;
                }

                Instance.outputText.text = "";
                if (delayedFrames > 0)
                    Instance.StartCoroutine(Instance.WaitFrames(() => Instance.ScrollLast(), delayedFrames));
                else
                    Instance.ScrollLast();
            }
        }

        public static void Clear(float delayedSeconds)
        {
            if (Instance.outputText)
            {
                if (lineQueue != null)
                {
                    lineQueue.Clear();
                    sb.Length = 0;
                }

                Instance.outputText.text = "";
                if (delayedSeconds > 0)
                    Instance.StartCoroutine(Instance.WaitSeconds(() => Instance.ScrollLast(), delayedSeconds));
                else
                    Instance.ScrollLast();
            }
        }

#endregion

#region Instance method for editor (eg Inspector registration)

        //Display from the callback etc. in the inspector.
        //(*) Note that registering the enum / struct type in UnityEvent <T0> in the inspector will be null. (It does not become '<Missing>', but it seems to be an error at runtime).
        //
        //インスペクタでコールバックから表示するなど
        //※インスペクタで UnityEvent<T0> に enum / struct 型を登録すると null になるので注意（'<Missing>'にはならないが、ランタム時にエラーになるようだ）。

        public void _Log(object message)
        {
            Log(message);
        }

        public void _Clear()
        {
            Clear();
        }

        public void _Show()
        {
            Show();
        }

        public void _Hide()
        {
            Hide();
        }

#endregion

#region Other method Section

        public void SetVisible(bool visible)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = visible ? 1 : 0;
                canvasGroup.interactable = visible;
            }
        }

        public void ToggleVisible()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = (canvasGroup.alpha == 0) ? 1 : 0;
                canvasGroup.interactable = (canvasGroup.alpha > 0);
            }
        }

        public static void Show()
        {
            Instance.SetVisible(true);
        }

        public static void Hide()
        {
            Instance.SetVisible(false);
        }
        
#endregion

#region Other static method Section

        static bool HasICollection(object obj)
        {
            if (obj != null)
                return obj.GetType().GetInterface("System.Collections.ICollection") != null;
            return false;
        }

        static StringBuilder sbJoin = new StringBuilder(512);

        static string Join(ICollection collection, string separator = ", ", string brackets = "[]")
        {
            sbJoin.Length = 0;

            foreach (var item in collection)
            {
                if (sbJoin.Length > 0)
                    sbJoin.Append(separator);
                sbJoin.Append(item.ToString());
            }

            if (!string.IsNullOrEmpty(brackets))
            {
                if (brackets.Length >= 2)
                {
                    sbJoin.Append(brackets[1]);     //footer
                    sbJoin.Insert(0, brackets[0]);  //header
                }
                else //Length == 1
                {
                    sbJoin.Append(brackets);     //footer
                    sbJoin.Insert(0, brackets);  //header
                }
            }

            return sbJoin.ToString();
        }

#endregion
    }
}