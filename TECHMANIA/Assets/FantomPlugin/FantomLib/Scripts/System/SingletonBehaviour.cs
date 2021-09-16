using UnityEngine;

namespace FantomLib
{
    /// <summary>
    /// Use like Singleton MonoBehaviour
    ///·Base class for creating subclass
    /// 
    /// シングルトンで利用する MonoBehaviour
    ///・サブクラスを作る ベースクラス
    /// </summary>
    /// <typeparam name="T">Type of subclass</typeparam>
    public abstract class SingletonBehaviour<T> : MonoBehaviour
                                    where T : Component
    {

        public bool dontDestroyOnLoad = false;      //Do not destroy on scene transitions   //シーン遷移で破棄しない


        //Singleton instance    //シングルトン用インスタンス
        protected static T instance;

        public static T Instance {
            get {
                if (instance == null)
                {
                    GameObject go = new GameObject(typeof(T).Name);
                    instance = go.AddComponent<T>();
                    if (instance.GetComponent<SingletonBehaviour<T>>().dontDestroyOnLoad)
                        DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        // Use this for initialization
        protected void Awake()
        {
            if (instance == null)
            {
                instance = this as T;

                if (dontDestroyOnLoad)
                    DontDestroyOnLoad(gameObject);
            }
            else
            {
                if (instance != this)
                    Destroy(gameObject);   //Delete when double startup     //2重起動のときは削除する
            }
        }

    }
}
