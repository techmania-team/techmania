using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FantomLib
{
    /// <summary>
    /// Extend the save type of PlayerPrefs
    ///･Save object (class, struct), Array, List or Dictionary -> JSON format
    ///･bool -> 1 or 0 (int)
    ///･long, double -> string type
    ///
    /// PlayerPrefs の保存タイプを拡張する
    ///・オブジェクト（クラス・構造体）、配列、リスト、辞書を保存。
    ///・bool 値を 1 or 0(int) で保存。
    ///・long, double 値を string で保存を追加。
    /// </summary>
    public static class XPlayerPrefs
    {
        /// <summary>
        /// Save bool
        ///･bool -> int (1 or 0)
        ///
        /// bool 値を保存する
        ///・int（1 or 0）で保存する
        /// </summary>
        /// <param name="key">Save key</param>
        /// <param name="flg">Save value</param>
        public static void SetBool(string key, bool flg)
        {
            PlayerPrefs.SetInt(key, flg ? 1 : 0);
        }


        /// <summary>
        /// Load bool
        ///･int (1 or 0) -> bool
        ///
        /// bool 値を読み込む
        ///・int（1 or 0) で保存した値を読み出し、bool 値に変換して返す
        /// </summary>
        /// <param name="key">Save key</param>
        /// <param name="def">Default value</param>
        /// <returns>Saved value</returns>
        public static bool GetBool(string key, bool def = false)
        {
            return PlayerPrefs.GetInt(key, def ? 1 : 0) != 0;
        }



        /// <summary>
        /// Save long
        ///･long -> string
        ///
        /// long 値を保存する
        ///・string で保存する
        /// </summary>
        /// <param name="key">Save key</param>
        /// <param name="value">Save value</param>
        public static void SetLong(string key, long value)
        {
            PlayerPrefs.SetString(key, value.ToString());
        }


        /// <summary>
        /// Load long
        ///･string -> long
        ///
        /// long 値を読み込む
        ///・string で保存した値を読み出し、long 値に変換して返す
        /// </summary>
        /// <param name="key">Save key</param>
        /// <param name="def">Default value</param>
        /// <returns>Saved value</returns>
        public static long GetLong(string key, long def = 0)
        {
            string s = PlayerPrefs.GetString(key, "");
            try {
                return string.IsNullOrEmpty(s) ? def : long.Parse(s);
            }
            catch {
                return def;
            }
        }


        /// <summary>
        /// Save double
        ///･double -> string
        ///
        /// double 値を保存する
        ///・string で保存する
        /// </summary>
        /// <param name="key">Save key</param>
        /// <param name="value">Save value</param>
        public static void SetDouble(string key, double value)
        {
            PlayerPrefs.SetString(key, value.ToString());
        }


        /// <summary>
        /// Load double
        ///･string -> double
        ///
        /// double 値を読み込む
        ///・string で保存した値を読み出し、double 値に変換して返す
        /// </summary>
        /// <param name="key">Save key</param>
        /// <param name="def">Default value</param>
        /// <returns>Saved value</returns>
        public static double GetDouble(string key, double def = 0)
        {
            string s = PlayerPrefs.GetString(key, "");
            try {
                return string.IsNullOrEmpty(s) ? def : double.Parse(s);
            }
            catch {
                return def;
            }
        }



        /// <summary>
        /// Save object (to JSON)
        /// 
        /// オブジェクトを JSON 形式（文字列型）に変換して保存する
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="key">Save key</param>
        /// <param name="obj">Save value</param>
        public static void SetObject<T>(string key, T obj)
        {
            PlayerPrefs.SetString(key, JsonUtility.ToJson(obj));
        }


        /// <summary>
        /// Load object (from JSON)
        /// 
        /// JSON 形式（文字列型）で保存されたデータをオブジェクトとして生成して返す
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="key">Save key</param>
        /// <param name="def">Default value</param>
        /// <returns>Saved value (newly create instance)</returns>
        public static T GetObject<T>(string key, T def = default(T))
        {
            string json = PlayerPrefs.GetString(key);
            return !string.IsNullOrEmpty(json) ? JsonUtility.FromJson<T>(json) : def;
        }


        /// <summary>
        /// Load object to be overwritten (from JSON)
        /// 
        /// JSON 形式（文字列型）で保存されたデータをオブジェクトに上書きする
        /// </summary>
        /// <typeparam name="T">Type of object</typeparam>
        /// <param name="key">Save key</param>
        /// <param name="obj">Saved value (to be overwritten)</param>
        public static void GetObjectOverwrite<T>(string key, ref T obj)
        {
            string json = PlayerPrefs.GetString(key);
            if (!string.IsNullOrEmpty(json))
                JsonUtility.FromJsonOverwrite(json, obj);
        }



        /// <summary>
        /// Save Array (to JSON)
        /// 
        /// 静的配列を JSON 形式（文字列型）に変換して保存する
        /// </summary>
        /// <typeparam name="T">Type of element</typeparam>
        /// <param name="key">Save key</param>
        /// <param name="arr">Save value</param>
        public static void SetArray<T>(string key, T[] arr)
        {
            SetObject(key, new ArrayWrap<T>(arr));
        }


        /// <summary>
        /// Load Array (from JSON)
        /// 
        /// JSON 形式（文字列型）で保存された要素を配列として生成して返す
        /// </summary>
        /// <typeparam name="T">Type of element</typeparam>
        /// <param name="key">Save key</param>
        /// <param name="def">Default value</param>
        /// <returns>Saved value (newly create instance)</returns>
        public static T[] GetArray<T>(string key, T[] def = null)
        {
            ArrayWrap<T> obj = GetObject<ArrayWrap<T>>(key);
            return obj != null ? obj.ToArray() : def;
        }



        /// <summary>
        /// Save List (to JSON)
        /// 
        /// リストを JSON 形式（文字列型）に変換して保存する
        /// </summary>
        /// <typeparam name="T">Type of element</typeparam>
        /// <param name="key">Save key</param>
        /// <param name="list">Save value</param>
        public static void SetList<T>(string key, List<T> list)
        {
            SetObject(key, new ListWrap<T>(list));
        }


        /// <summary>
        /// Load List (from JSON)
        /// 
        /// JSON 形式（文字列型）で保存された要素をリストとして生成して返す
        /// </summary>
        /// <typeparam name="T">Type of element</typeparam>
        /// <param name="key">Save key</param>
        /// <param name="def">Default value</param>
        /// <returns>Saved value (newly create instance)</returns>
        public static List<T> GetList<T>(string key, List<T> def = null)
        {
            ListWrap<T> obj = GetObject<ListWrap<T>>(key);
            return obj != null ? obj.ToList() : def;
        }



        /// <summary>
        /// Save Dictionary (to JSON)
        ///･Array of keys, Array of values pair -> JSON
        ///·In JSON, it is stored as a key array and value array (= TryGetArrayPair() can also be obtained as an array of pairs).
        ///
        /// 辞書を JSON 形式（文字列型）に変換して保存する
        ///・JSON ではキー配列と値配列として保存される（＝TryGetArrayPair()でペアの配列としても取得できる）。
        /// </summary>
        /// <typeparam name="K">Type of keys</typeparam>
        /// <typeparam name="V">Type of values</typeparam>
        /// <param name="key">Save key</param>
        /// <param name="dic">Save value</param>
        public static void SetDictionary<K, V>(string key, Dictionary<K, V> dic)
        {
            SetObject(key, new DictionaryWrap<K, V>(dic));
        }


        /// <summary>
        /// Load Dictionary (from JSON)
        ///･JSON -> Array of keys, Array of values pair -> Dictionary
        ///･In JSON, the dictionary is also saved as a key array and value array, so you can also get a dictionary saved with SetDictionary().
        ///
        /// JSON 形式（文字列型）で保存された要素を辞書として生成して返す
        ///・JSON では辞書もキー配列と値配列として保存されるため、SetArrayPair() 保存したペア配列も辞書として取得できる。
        /// </summary>
        /// <typeparam name="K">Type of keys</typeparam>
        /// <typeparam name="V">Type of values</typeparam>
        /// <param name="key">Save key</param>
        /// <param name="def">Default value</param>
        /// <returns>Saved value (newly create instance)</returns>
        public static Dictionary<K, V> GetDictionary<K, V>(string key, Dictionary<K, V> def = null)
        {
            DictionaryWrap<K, V> obj = GetObject<DictionaryWrap<K, V>>(key);
            return obj != null ? obj.ToDictionary() : def;
        }


        /// <summary>
        /// Save Array of keys, Array of values pair (to JSON)
        ///·In JSON, it is saved as key array and value array (= GetDictionary() can also be obtained as a dictionary).
        /// 
        /// キーと値のペア配列を保存する
        ///・JSON ではキー配列と値配列として保存される（＝GetDictionary()で辞書としても取得できる）。
        /// </summary>
        /// <typeparam name="K">Type of keys</typeparam>
        /// <typeparam name="V">Type of values<</typeparam>
        /// <param name="key">Save key</param>
        /// <param name="keys">Array of keys</param>
        /// <param name="values">Array of values</param>
        public static void SetArrayPair<K, V>(string key, K[] keys, V[] values)
        {
            SetObject(key, new DictionaryWrap<K, V>(keys, values));
        }


        /// <summary>
        /// Load Array of keys, Array of values pair (from JSON)
        ///･In JSON, the dictionary is also saved as a key array and value array, so you can also get a dictionary saved with SetDictionary().
        /// 
        /// キーと値のペア配列を取得する
        ///・JSON では辞書もキー配列と値配列として保存されるため、SetDictionary() で保存した辞書も取得できる。
        /// </summary>
        /// <typeparam name="K">Type of keys</typeparam>
        /// <typeparam name="V">Type of values</typeparam>
        /// <param name="key"></param>
        /// <param name="keys">Saved Array of keys</param>
        /// <param name="values">Saved Array of values</param>
        /// <returns>get it -> true</returns>
        public static bool TryGetArrayPair<K, V>(string key, out K[] keys, out V[] values)
        {
            DictionaryWrap<K, V> obj = GetObject<DictionaryWrap<K, V>>(key);
            if (obj == null)
            {
                keys = null;
                values = null;
                return false;
            }
            else
            {
                keys = obj.Keys;
                values = obj.Values;
                return true;
            }
        }




        //====================================================================
        // A wrapping class that allows JSON to handle type by making type a member of class
        //･It is basically for work, and it is assumed to abandon after use (conversion -> returns copy).
        //(*) It does not allow to null (empty element is acceptable)
        //
        //型をクラスのメンバにすることにより JSON で扱えるようにするラップクラス
        //・基本的に作業用で、使用後（変換→複製後）は放棄することを前提としている。
        //※null には対応してない（空要素は可）。

        //Wrap Array
        [Serializable]
        private class ArrayWrap<T>
        {
            public T[] array;

            public ArrayWrap(T[] array)
            {
                this.array = array;
            }

            public T[] ToArray()
            {
                try {
                    return (T[])array.Clone();  //Returns copy
                }
                catch {
                    return new T[0];    //Empty array
                }
            }
        }


        //Wrap List
        [Serializable]
        private class ListWrap<T>
        {
            public List<T> list;

            public ListWrap(List<T> list)
            {
                this.list = list;
            }

            public List<T> ToList()
            {
                return new List<T>(list);   //Returns copy
            }
        }


        //Wrap Dictionary or Array of keys, Array of values pair
        //(*) The keys and values pair Array must have the same length.
        //･When saving: dic -> create keys, values from constructor -> save with JSON (like "keys":[0,1,2,...];"values":[0,1,2,...]);
        //･When loading: JSON -> keys, values overwrite -> converted to dictionary (or arrays)
        //
        //辞書 または キー＆値ペア配列 をラップ
        //・セーブ時：コンストラクタから dic → keys, values を作成 → JSON で保存（"keys":[0,1,2,…];"values":[0,1,2,…]; のようになる）
        //・ロード時：JSON から → keys, values 上書き → 辞書に変換したものを返す
        //※キー＆値ペア配列は長さを同じにすること
        [Serializable]
        private class DictionaryWrap<K, V>
        {
            [SerializeField] private K[] keys;      //Array of keys (It is converted to JSON)
            [SerializeField] private V[] values;    //Array of values (It is converted to JSON)

            public DictionaryWrap(Dictionary<K, V> dic)
            {
                keys = dic.Keys.ToArray();
                values = dic.Values.ToArray();
            }

            public DictionaryWrap(K[] keys, V[] values)     //(*) Pair Array must have the same length.
            {
                this.keys = keys;
                this.values = values;
            }

            public Dictionary<K, V> ToDictionary()
            {
                try {
                    return keys.Select((k, i) => new { k, v = values[i] })
                        .ToDictionary(a => a.k, a => a.v);      //(*) An error occurs if there is a duplicate key.  //※重複キーがあるとエラーとなる
                }
                catch {
                    return new Dictionary<K, V>();  //Empty dictionary
                }
            }

            public K[] Keys {
                get {
                    try {
                        return (K[])keys.Clone();   //Returns copy
                    }
                    catch {
                        return new K[0];    //Empty array
                    }
                }
            }

            public V[] Values {
                get {
                    try {
                        return (V[])values.Clone(); //Returns copy
                    }
                    catch {
                        return new V[0];    //Empty array
                    }
                }
            }
        }

    }
}