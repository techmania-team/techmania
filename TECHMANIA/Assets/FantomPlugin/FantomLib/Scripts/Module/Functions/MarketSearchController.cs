using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FantomLib
{
    /// <summary>
    /// Market Search Controller
    /// 
    /// Open and search Google Play
    /// ·For package name (application ID), go to the detail screen.
    /// ·For keywords, go to search results.
    /// (*)Google Play needs to be installed.
    /// 
    /// Google Play を開いて検索する
    /// ・パッケージ名（アプリのID）の場合は詳細画面へ行く。
    /// ・キーワードの場合は検索結果へ行く。
    /// ※Google Play がインストールされている必要がある。
    /// </summary>
    public class MarketSearchController : MonoBehaviour
    {
        //Inspector Settings
        [Serializable]
        public enum SearchType
        {
            PackageName,
            Keyword,
        }
        public SearchType searchType = SearchType.PackageName;  //search method

        public string packageName = "com.google.android.tts";   //Application ID
        public string keyword = "Google TTS";                   //Search keyword


        // Use this for initialization
        private void Start()
        {

        }

        // Update is called once per frame
        //private void Update()
        //{

        //}


        //Start Web Search with local packageName or keyword.
        //It is also the query of the last search done.
        public void Show()
        {
#if UNITY_EDITOR
            Debug.Log("MarketSearchController.Show : searchType = " + searchType
                + ", packageName = " + packageName
                + ", keyword = " + keyword);
#elif UNITY_ANDROID
            switch (searchType)
            {
                case SearchType.PackageName:
                    if (!string.IsNullOrEmpty(packageName))
                        AndroidPlugin.ShowMarketDetails(packageName);
                    break;
                case SearchType.Keyword:
                    if (!string.IsNullOrEmpty(keyword))
                        AndroidPlugin.StartMarketSearch(keyword);
                    break;
            }
#endif
        }

        //Set packageName dynamically and start Market Search (current packageName and searchType will be overwritten)
        public void Show(string packageName)
        {
            this.packageName = packageName;
            searchType = SearchType.PackageName;
            Show();
        }

        //Set keyword dynamically and start Market Search (current keyword and searchType will be overwritten)
        public void StartSearch(string keyword)
        {
            this.keyword = keyword;
            searchType = SearchType.Keyword;
            Show();
        }
    }
}
