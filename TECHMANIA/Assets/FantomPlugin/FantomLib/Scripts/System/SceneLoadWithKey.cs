using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace FantomLib
{
    /// <summary>
    /// Load Scene with key input
    /// 
    /// キー入力でシーンをロードする
    /// </summary>
    public class SceneLoadWithKey : MonoBehaviour
    {
        //Inspector Settings
        public int sceneBuildIndex = 0;             //Index of 'Scenes in Build'                                //シーンインデクスで指定用
        public bool useName = false;                //true = use "sceneName" / false = use "sceneBuildIndex"    //シーン名で指定フラグ（true=シーン名を使う/false=インデクスを使う）
        public string sceneName = "";               //Scene Name                                                //シーン名で指定用
        public bool isAdditive = false;             //Additional Load                                           //追加ロード

        public bool enableKey = true;               //enable key operation                                      //キーを有効にする
        public KeyCode loadKey = KeyCode.PageUp;    //Key code to load scene                                    //ロードするキー
        public float loadDelay = 0.0f;              //Load execution delay (Reasonable until 3.0 seconds)       //ロード開始ディレイ（3.0秒くらいまでが妥当）

        //Callbacks
        public UnityEvent OnKeyPressed;             //Callback when press load key                              //ロードキーを押したときのコールバック
        public UnityEvent OnBeforeDelay;            //Callback when just before waiting                         //待機前に呼び出されるコールバック
        public UnityEvent OnBeforeLoad;             //Callback when just before load                            //ロード前に呼び出されるコールバック


        //Local Values
        protected bool done = false;                //Key input done (For double prevention)                    //キー入力実行済み（２重防止用）



        // Use this for initialization
        protected void Start()
        {

        }


        // Update is called once per frame
        protected void Update()
        {
            if (enableKey && !done)
            {
                if (Input.GetKeyDown(loadKey))
                {
                    done = true;

                    if (OnKeyPressed != null)
                        OnKeyPressed.Invoke();

                    OnSceneLoad();
                }
            }
        }


        protected Coroutine coroutine = null;   //For double prevention //2重防止

        //For calling from outside (Load is made unique)
        //外部から呼び出し用（ロードはユニークにする）
        public void OnSceneLoad()
        {
            if (coroutine == null)
                coroutine = StartCoroutine(WaitAndLoad(loadDelay > 0 ? loadDelay : 0));
        }


        //Wait for the specified time and then load the scene (For calling "OnSceneLoad()")
        //指定時間待機してからシーンを読み込む（※OnSceneLoad() から呼び出し用）
        protected virtual IEnumerator WaitAndLoad(float sec)
        {
            if (OnBeforeDelay != null)
                OnBeforeDelay.Invoke();

            yield return new WaitForSeconds(sec);

            if (OnBeforeLoad != null)
                OnBeforeLoad.Invoke();

            if (useName)
            {
                if (!string.IsNullOrEmpty(sceneName))
                {
                    SceneManager.LoadScene(sceneName, isAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single);
                }
                else  //sceneName is empty   //シーン名=空文字
                {
#if UNITY_EDITOR
                    Debug.LogWarning("sceneName is empty.");
#endif
                    done = false;       //reset state   //元に戻す
                    coroutine = null;
                    yield break;
                }
            }
            else
            {
                SceneManager.LoadScene(sceneBuildIndex, isAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single);
            }

            //For additional loading, reset it.
            //追加の場合は元に戻す
            if (isAdditive)
            {
                done = false;
                coroutine = null;
            }
            else
            {
                done = true;    //Just in case  //念のため
            }
        }
    }
}