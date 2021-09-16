using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FantomLib
{
    /// <summary>
    /// DialogItem setting parameter for inspector (Convert to DialogItem type at runtime and use it)
    ///·Using the 'Text' property, you can get a localized string when 'LocalizeStringResource' is set.
    /// 
    /// インスペクタ用の DialogItem パラメタ設定（実行時には DialogItem に変換されて利用される）
    ///・Text プロパティを使用すると、LocalizeStringResource が設定されているとき、ローカライズされた文字列を取得できる。
    /// </summary>
    public class DialogItemParameter : MonoBehaviour
    {
        public DialogItemType type = DialogItemType.Divisor;
        public string key = "";                     //Key to be associated with return value

        //Divisor
        public float lineHeight = 1;                //line width (dp)
        public Color lineColor = Color.black;       //When clear, it is not specified.

        //Text
        [Multiline] public string text = "";        //text string
        public Color textColor = Color.black;       //When clear, it is not specified.
        public Color backgroundColor = Color.clear; //When clear, it is not specified.

        [Serializable] public enum TextAlign { None, Left, Center, Right }
        public TextAlign align = TextAlign.None;    //text alignment ("": not specified, "center", "right", "left")

        //Switch
        public bool defaultChecked = false;         //on/off

        //Slider
        public float value = 100;
        public float minValue = 0;
        public float maxValue = 100;
        public int digit = 0;

        //ToggleGroup
        [Serializable]
        public class ToggleItemData
        {
            public string text = "";
            public string value = "";
        }
        public ToggleItemData[] toggleItems;

        public int checkedIndex = 0;


        //Localize
        [Serializable]
        public class LocalizeData
        {
            public LocalizeStringResource localizeResource;
            public string textID = "text";
        }
        public LocalizeData localize;


        [Serializable]
        public class LocalizeItem
        {
            public LocalizeStringResource localizeResource;
            public string[] textID;
        }
        public LocalizeItem localizeItems;


        //Get localized texts
        public string[] TogglesTexts {
            get {
                //return toggleItems.Select(e => e.text).ToArray(); 

                //overwrite by localized text
                string[] texts = new string[toggleItems.Length];
                for (int i = 0; i < toggleItems.Length; i++)
                {
                    if (localizeItems.localizeResource != null && i < localizeItems.textID.Length)
                        texts[i] = localizeItems.localizeResource.Text(localizeItems.textID[i], toggleItems[i].text);
                    else
                        texts[i] = toggleItems[i].text;
                }
                return texts;
            }
        }
        public string[] TogglesValues {
            get { return toggleItems.Select(e => e.value).ToArray(); }
        }

        //Get localized text
        public string Text {
            get {
                if (localize.localizeResource != null)
                    return localize.localizeResource.Text(localize.textID, text);
                return text;
            }
        }

        public string TextByLanguage(SystemLanguage language)
        {
            if (localize.localizeResource != null)
                return localize.localizeResource.Text(localize.textID, language, text);
            return text;
        }

    }
}
