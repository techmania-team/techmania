using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace FantomLib
{
    /// <summary>
    /// 'Param' class is basically the same as Dictionary prepared for easy handling of value type conversion and default value.
    ///･All keys and values are stored in string type.
    ///･"Parse()", "ParseToDictionary()" is method to convert text format like "key1=value1" to dictionary.
    ///
    /// Dictionary の値取得などを少し楽に使うクラス
    ///・キーと値は全て string 型で保持されている。取得時に型変換とデフォルト値を指定できる。
    ///・"key1=value1" のようなテキスト形式を変換（Parse(), ParseToDictionary()）して簡単に扱うために作ったラッパー的なクラス。
    /// </summary>
    public class Param : Dictionary<string, string>
    {

        //====================================================================
        //Constructors

        public Param() : base() { }

        public Param(Dictionary<string, string> dic) : base(dic) { }


        //====================================================================
        //Get/Set a value

        public string GetString(string key, string def = "")
        {
            return ContainsKey(key) ? this[key] : def;
        }

        public int GetInt(string key, int def = 0)
        {
            try {
                return ContainsKey(key) ? int.Parse(this[key]) : def;
            }
            catch {
                return def;
            }
        }

        public float GetFloat(string key, float def = 0)
        {
            try {
                return ContainsKey(key) ? float.Parse(this[key]) : def;
            }
            catch {
                return def;
            }
        }

        public bool GetBool(string key, bool def = false)
        {
            try {
                return ContainsKey(key) ? bool.Parse(this[key]) : def;
            }
            catch {
                return def;
            }
        }

        public void Set(string key, object value)
        {
            this[key] = value.ToString();
        }


        //====================================================================
        //etc.

        public override string ToString()
        {
            if (Count > 0)
                return this.Select(e => e.Key + " => " + e.Value).Aggregate((s, a) => s + ", " + a).ToString();
            return "";
        }


        //====================================================================
        //static methods

        /// <summary>
        /// Parsing text and generating a Dictionary
        ///･string: "key1=value1\nkey2=value2\nkey3=value3" -> Dictionary: dic[key1] = value1, dic[key2] = value2, dic[key3] = value3
        ///･Note that it does not check for invalid text.
        ///･Note that duplicate keys result in an error (returns null).
        ///･The generated Dictionary has both key and value as string type.
        ///
        /// テキストをパースして辞書を生成する
        ///・文字列："key1=value1\nkey2=value2\nkey3=value3" などキーと値のペアになっているテキストを区切り文字で分割して辞書を作る。
        ///・不正なテキストはチェックしてないので注意（※キーが重複してるとエラー（戻値が null）となるので注意）。
        ///・生成された辞書はキーと値共に文字列型となる。
        /// </summary>
        /// <param name="text">Text to parse</param>
        /// <param name="itemSeparator">Delimiter for each item</param>
        /// <param name="pairSeparator">Delimiter for Key and value</param>
        /// <returns>Dictionary created with key and value (failure or empty -> null)</returns>
        public static Dictionary<string, string> ParseToDictionary(string text, char itemSeparator = '\n', char pairSeparator = '=')
        {
            if (string.IsNullOrEmpty(text))
                return null;

            try {
                return text.Split(new char[] { itemSeparator }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Split(new char[] { pairSeparator }, 2))
                    .ToDictionary(a => a[0], a => a[1]);    //(*) Note that duplicate keys result in an error.
            }
            catch {
                return null;
            }
        }


        /// <summary>
        /// Parsing text and generating a Param
        ///･string: "key1=value1\nkey2=value2\nkey3=value3" -> Dictionary: dic[key1] = value1, dic[key2] = value2, dic[key3] = value3
        ///･Note that we do not check for invalid text
        ///･Note that duplicate keys result in an error.
        ///･The generated Dictionary has both key and value as string type.
        ///
        /// テキストをパースして Param（辞書）を生成する
        ///・文字列："key1=value1\nkey2=value2\nkey3=value3" などキーと値のペアになっているテキストを区切り文字で分割して辞書を作る。
        ///・不正なテキストはチェックしてないので注意。
        ///・生成された辞書はキーと値共に文字列型となる。
        /// </summary>
        /// <param name="text">Text to parse</param>
        /// <param name="itemSeparator">Delimiter for each item</param>
        /// <param name="pairSeparator">Delimiter for Key and value</param>
        /// <returns>Param created with key and value (failure or empty -> null)</returns>
        public static Param Parse(string text, char itemSeparator = '\n', char pairSeparator = '=')
        {
            Dictionary<string, string> dic = ParseToDictionary(text, itemSeparator, pairSeparator);
            return (dic != null) ? new Param(dic) : null;
        }


        /// <summary>
        /// Convert it to JSON format (string type) as a Dictionary and save it in PlayerPrefs
        ///(*) Param class is basically the same as a dictionary, and it is the same as XPlayerPrefs.SetDictionary(), because content classes (which inherit Dictionary) are handled easily.
        ///･In JSON, it is stored as a key array and value array (= XPlayerPrefs.TryGetArrayPair() can also be obtained as an array of pairs).
        /// 
        /// 辞書として JSON 形式（文字列型）に変換して PlayerPrefs に保存する
        ///※Param は基本的に辞書と同じでパラメタを簡単に扱うクラス（Dictionary を継承している）ため、内容的には XPlayerPrefs.SetDictionary() と同じ。
        ///・JSON ではキー配列と値配列として保存される（＝TryGetArrayPair()でペアの配列としても取得できる）。
        /// </summary>
        /// <param name="key">Save key</param>
        /// <param name="param">Save value (Param)</param>
        public static void SetPlayerPrefs(string key, Param param)
        {
            XPlayerPrefs.SetDictionary(key, param);
        }


        /// <summary>
        /// Generate and return elements saved in JSON format (string type) in PlayerPrefs as Param class.
        ///(*) Param class is basically the same as a dictionary, and it is the same as XPlayerPrefs.GetDictionary(), because content classes (which inherit Dictionary) are handled easily.
        ///･In JSON, the dictionary is also saved as a key array and a value array, so XPlayerPrefs.SetArrayPair() saved pair array can also be acquired as a dictionary.
        /// 
        /// PlayerPrefs に JSON 形式（文字列型）で保存された要素を辞書として生成して返す
        ///※Param は基本的に辞書と同じでパラメタを簡単に扱うクラス（Dictionary を継承している）ため、内容的には XPlayerPrefs.GetDictionary() と同じ。
        ///・JSON では辞書もキー配列と値配列として保存されるため、XPlayerPrefs.SetArrayPair() 保存したペア配列も辞書として取得できる。
        /// </summary>
        /// <param name="key">Save key</param>
        /// <param name="def">Defalut value</param>
        /// <returns>Saved value (Param: newly created)</returns>
        public static Param GetPlayerPrefs(string key, Param def = null)
        {
            Dictionary<string, string> dic = XPlayerPrefs.GetDictionary<string, string>(key);
            return (dic != null) ? new Param(dic) : def;
        }

    }

}