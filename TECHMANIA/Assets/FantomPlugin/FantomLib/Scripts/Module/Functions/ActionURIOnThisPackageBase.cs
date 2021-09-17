using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Base class that executes ActionURI with URI as its package
    /// 
    /// URI を自身のパッケージとした ActionURI を実行するベースクラス
    /// </summary>
    public abstract class ActionURIOnThisPackageBase : MonoBehaviour
    {
        //(*) Required override action.
        protected virtual string action {
            get { return "android.intent.action.VIEW"; }
        }


        // Use this for initialization
        protected void Start()
        {

        }

        // Update is called once per frame
        //protected void Update()
        //{

        //}

        
        //Start Action to URI
        public virtual void StartAction()
        {
            string uri = "package:" + Application.identifier;   //add scheme
#if UNITY_EDITOR
            Debug.Log(name + ".Show : uri = " + uri);
#elif UNITY_ANDROID
            AndroidPlugin.StartActionURI(action, uri);
#endif
        }
        
        //Alias
        public virtual void Show()
        {
            StartAction();
        }

    }
}
