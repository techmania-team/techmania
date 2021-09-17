using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FantomLib
{
    /// <summary>
    /// Localize Text (For UI-Text)
    /// 
    /// Apply LocalizeString to UI-Text.
    ///･Ignored if languages are not found (= remain as original text).
    ///·Ignored if font size is 0 (= remains as original size).
    ///(*) Localization is done only once at startup. It does not apply to dynamically modified character strings (Activated by registering 'LocalizeStringResource' in inspector).
    ///
    /// 
    /// ローカライズされたテキストを取得して、UI の Text に代入する。
    ///・言語が見つからない場合は無視される（＝元のままとなる）。
    ///・フォントサイズが 0 になっている場合は無視される（＝元のままとなる）。
    ///※ローカライズは起動時に一度だけ行われる。動的に変更した文字列には適用されないので注意（LocalizeStringResource をインスペクタで登録することで有効になる）。
    /// </summary>
    public class LocalizeText : LocalizableBehaviour, ILocalizable
    {
        //Inspector Settings
        public Text targetText;             //Target UI text
        public LocalizeString localize;     //Localized text data

        [Serializable]
        public enum ResourceType
        {
            Local,      //use local value 'LocalizeString'
            Resource,   //use value from 'LocalizeStringResource' linked with ID.
        }
        public ResourceType resourceType = ResourceType.Local;

        //Localize resource ID data
        [Serializable]
        public class LocalizeData
        {
            public LocalizeStringResource localizeResource;
            public string textID = "text";
        }
        public LocalizeData localizeData;

#region Properties and Local values Section

        //Get text from LocalizeString and replace.
        //LocalizeStringからテキストを取得し、置き換える。
        private void ApplyLocalizeToText(Text targetText, SystemLanguage language = SystemLanguage.Unknown)
        {
            if (targetText == null)
                return;

            string str = "";
            int fontSize = 0;
            switch (resourceType)
            {
                case ResourceType.Local:
                    if (localize != null)
                    {
                        str = localize.TextByLanguage(language);
                        fontSize = localize.FontSizeByLanguage(language);
                    }
                    break;

                case ResourceType.Resource:
                    if (localizeData.localizeResource != null)
                    {
                        LocalizeString loc = localizeData.localizeResource[localizeData.textID];
                        if (loc != null)    //ID is found
                        {
                            str = loc.TextByLanguage(language);
                            fontSize = loc.FontSizeByLanguage(language);
                        }
                    }
                    break;
            }

            if (!string.IsNullOrEmpty(str))
            {
                targetText.text = str;
                if (fontSize > 0)
                    targetText.fontSize = fontSize;
            }
        }

        //Apply (reset) localized string
        public void ApplyLocalize()
        {
            ApplyLocalizeToText(targetText);
        }

        //Specify language and apply (update) localized string
        public override void ApplyLocalize(SystemLanguage language)
        {
            ApplyLocalizeToText(targetText, language);
        }

#endregion

        // Use this for initialization
        private void Awake()
        {
            ApplyLocalize();
        }

        private void Start()
        {
            
        }

        // Update is called once per frame
        //private void Update()
        //{

        //}
    }
}
