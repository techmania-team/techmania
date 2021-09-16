using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FantomLib
{
    /// <summary>
    /// Release AndroidPlugin on Unity life cycle.
    /// </summary>
    public class AndroidPluginRelease : MonoBehaviour
    {
        //Inspector Settings
        public bool releaseOnDisable = false;
        public bool releaseOnDestroy = false;
        public bool releaseOnQuit = true;


        // Use this for initialization
        private void Start()
        {

        }

        private void OnDisable()
        {
            if (releaseOnDisable)
                Release();
        }

        private void OnDestroy()
        {
            if (releaseOnDestroy)
                Release();
        }

        private void OnApplicationQuit()
        {
            if (releaseOnQuit)
                Release();
        }

        // Update is called once per frame
        //private void Update()
        //{

        //}

       
        //Execute AndroidPlugin release
        public void Release()
        {
#if UNITY_EDITOR
            Debug.Log("AndroidPlugin.Release called");
#elif UNITY_ANDROID
            AndroidPlugin.Release();
#endif
        }
    }
}