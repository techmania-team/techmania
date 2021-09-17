using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FantomLib
{
    /// <summary>
    /// Web Search Controller
    /// </summary>
    public class WebSearchController : MonoBehaviour
    {
        //Inspector Settings
        public string keyword = "keyword";      //Search keyword


        // Use this for initialization
        private void Start()
        {

        }

        // Update is called once per frame
        //private void Update()
        //{

        //}

        
        //Start Web Search with local keyword
        public void StartSearch()
        {
#if UNITY_EDITOR
            Debug.Log("WebSearchController.StartSearch : keyword = " + keyword);
#elif UNITY_ANDROID
            AndroidPlugin.StartWebSearch(keyword);
#endif
        }

        //Set keyword dynamically and start Web Search (current keyword will be overwritten)
        public void StartSearch(string keyword)
        {
            this.keyword = keyword;
#if UNITY_EDITOR
            Debug.Log("WebSearchController.StartSearch : keyword = " + keyword);
#elif UNITY_ANDROID
            AndroidPlugin.StartWebSearch(keyword);
#endif
        }

        //Set keyword dynamically and start Web Search (current keyword will be overwritten)
        public void StartSearch(string[] keywords)
        {
            keyword = string.Join(" ", keywords);
#if UNITY_EDITOR
            Debug.Log("WebSearchController.StartSearch : keyword = " + keyword);
#elif UNITY_ANDROID
            AndroidPlugin.StartWebSearch(keyword);
#endif
        }
    }
}
