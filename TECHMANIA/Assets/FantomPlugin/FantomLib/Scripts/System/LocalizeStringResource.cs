using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FantomLib
{
    /// <summary>
    /// Localize String Resource
    /// 
    /// LocalizeString as a resource linked with ID (string type)
    ///·For 'LocalizeString' object retrieval, null is returned if ID can not be found.
    ///·In the 'Text' property is returned 'def' (default is empty character ("")) if language is not found, ID is not found, If ID itself is empty.
    ///·If ID is duplicated or empty, it can not be acquired (Error checked at startup only on Unity editor).
    ///·Inspector registration 'items' is converted to Dictionary by 'Initialize()' method and used at runtime.
    ///
    /// 
    /// LocalizeString と ID（文字列）を紐付けしたリソースとして扱う
    ///・LocalizeString オブジェクト取得では、ID が見つからない場合 null が返る。
    ///・Text プロパティでは、言語が見つからない、ID が見つからない、ID 自体が空 の場合、def（デフォルトでは空文字（""））が返る。
    ///・ID が重複、または空の場合は取得できない（Unity エディタ上のときのみ、起動時にエラーチェックされる）。
    ///・インスペクタ登録用リスト（items）は Initialize() メソッドで Dictionary へ変換して、実行時に使われる。
    /// </summary>
    public class LocalizeStringResource : MonoBehaviour
    {
        //Inspector settings
        [SerializeField] private LocalizeString[] items;    //It is used from 'Dictionary' at runtime (need 'Initialize()').

#region Properties and Local values Section

        private bool initialized = false;           //initialize done flag (false = need 'Initialize()')

        //Table for acquiring 'LocalizeString' from ID (for runtime)
        //ID から LocalizeString を取得するテーブル（実行時用）
        private Dictionary<string, LocalizeString> table = new Dictionary<string, LocalizeString>();

        //Return LocalizeString with indexer [ID] (null if none)
        //インデクサ [ID] で LocalizeString を返す（無いときは null）
        public LocalizeString this[string id] {
            get {
                if (!initialized)
                    Initialize();
                if (table.ContainsKey(id))
                    return table[id];
                return null;
            }
        }

        //Returns the localized character string linked with the ID (it will be 'def' if ID or language is not found)
        //ID に紐付けされた、ローカライズ文字列を返す（IDや言語が見つからないときは def になる）
        public string Text(string id, string def = "") {
            if (string.IsNullOrEmpty(id))
                return def;

            LocalizeString loc = this[id];
            if (loc != null)
                return loc.TextOrDefault(def);
            return def;
        }

        //Specify the language and returns the localized character string linked with the ID (it will be 'def' if ID or language is not found)
        //言語を指定して、ID に紐付けされたローカライズ文字列を返す（IDや言語が見つからないときは def になる）
        public string Text(string id, SystemLanguage language, string def = "", bool notFoundIsDefaultLanguage = true) {
            if (string.IsNullOrEmpty(id))
                return def;

            LocalizeString loc = this[id];
            if (loc != null)
                return loc.TextByLanguage(language, def, notFoundIsDefaultLanguage);  //If not found, it becomes 'def'.
            return def;
        }

        //Returns an array of localized strings linked with the ID (it will be empty("") if ID or language is not found)
        //ID に紐付けされた、ローカライズ文字列の配列を返す（IDや言語が見つからないときは空文字（""）になる）
        public string[] Texts(string[] id, string[] def = null) {
            string[] texts = new string[id.Length];
            for (int i = 0; i < id.Length; i++)
            {
                string t = (def != null && i < def.Length) ? def[i] : "";
                texts[i] = Text(id[i], t);
            }
            return texts;
        }

        //Specify the language and returns an array of localized strings linked with the ID (it will be empty("") if ID or language is not found)
        //言語を指定して、ID に紐付けされたローカライズ文字列の配列を返す（IDや言語が見つからないときは空文字（""）になる）
        public string[] Texts(string[] id, SystemLanguage language, string[] def = null, bool notFoundIsDefaultLanguage = true) {
            string[] texts = new string[id.Length];
            for (int i = 0; i < id.Length; i++)
            {
                string t = (def != null && i < def.Length) ? def[i] : "";
                texts[i] = Text(id[i], language, t, notFoundIsDefaultLanguage);
            }
            return texts;
        }


        //Returns the localized string's font size linked with the ID (it will be 'def' if ID or language is not found)
        //ID に紐付けされた、ローカライズ文字列のフォントサイズを返す（IDや言語が見つからないときは def になる）
        public int FontSize(string id, int def = 0) {
            if (string.IsNullOrEmpty(id))
                return def;

            LocalizeString loc = this[id];
            if (loc != null)
                return loc.FontSizeOrDefault(def);
            return def;
        }

        //Specify the language and returns the localized string's font size linked with the ID (it will be 'def' if ID or language is not found)
        //言語を指定して、ID に紐付けされたローカライズ文字列のフォントサイズを返す（IDや言語が見つからないときは def になる）
        public int FontSize(string id, SystemLanguage language, int def = 0, bool notFoundIsDefaultLanguage = true) {
            if (string.IsNullOrEmpty(id))
                return def;

            LocalizeString loc = this[id];
            if (loc != null)
                return loc.FontSizeByLanguage(language, def, notFoundIsDefaultLanguage);
            return def;
        }

        //Returns an array of localized string's font sizes linked with the ID (it will be 0 if ID or language is not found)
        //ID に紐付けされた、ローカライズ文字列のフォントサイズの配列を返す（IDや言語が見つからないときは 0 になる）
        public int[] FontSizes(string[] id, int[] def = null) {
            int[] sizes = new int[id.Length];
            for (int i = 0; i < id.Length; i++)
            {
                int t = (def != null && i < def.Length) ? def[i] : 0;
                sizes[i] = FontSize(id[i], t);
            }
            return sizes;
        }

        //Specify the language and returns an array of localized string's font sizes linked with the ID (it will be 0 if ID or language is not found)
        //言語を指定して、ID に紐付けされたローカライズ文字列のフォントサイズの配列を返す（IDや言語が見つからないときは 0になる）
        public int[] FontSizes(string[] id, SystemLanguage language, int[] def = null, bool notFoundIsDefaultLanguage = true) {
            int[] sizes = new int[id.Length];
            for (int i = 0; i < id.Length; i++)
            {
                int t = (def != null && i < def.Length) ? def[i] : 0;
                sizes[i] = FontSize(id[i], language, t, notFoundIsDefaultLanguage);
            }
            return sizes;
        }



        //Create a Dictionary from the List.
        //リストから辞書を作る
        public void Initialize()
        {
            table.Clear();
            foreach (var item in items)
            {
                if (!string.IsNullOrEmpty(item.id))
                    table[item.id] = item;
            }
            initialized = true;     //initialize done

#if UNITY_EDITOR
            CheckForErrors();    //Check for items (Editor only).
#endif
        }

        //Check empty or duplication from item elements.
        private void CheckForErrors()
        {
            if (items == null || items.Length == 0)
            {
                Debug.LogWarning("[" + gameObject.name + "] 'Items' is empty.");
            }
            else
            {
                if (table.Count != items.Length)
                    Debug.LogError("[" + gameObject.name + "] There is empty or duplicate 'ID'.");  //IDが空、または重複IDがある
            }
        }

#endregion

        // Use this for initialization
        private void Awake()
        {
            if (!initialized)
                Initialize();
        }

        // Update is called once per frame
        //private void Update()
        //{

        //}

#region Editor tool Section

#if UNITY_EDITOR

        //Busy flag
        public bool EditExecuting {
            get; private set;
        }

        //Whether the index is within range.
        public bool IsValidIndex(int index)
        {
            return (items != null && 0 <= index && index < items.Length);
        }

        //Data at the specified index position is copied & inserted.
        public bool InsetItem(int index)
        {
            if (!IsValidIndex(index))
                return false;

            EditExecuting = true;
            ArrayUtility.Insert(ref items, index, items[index].Clone());
            EditExecuting = false;
            return true;
        }

        //Data at the specified index position is deleted.
        public bool RemoveItem(int index)
        {
            if (!IsValidIndex(index))
                return false;

            EditExecuting = true;
            ArrayUtility.RemoveAt(ref items, index);
            EditExecuting = false;
            return true;
        }

        //It is a simple sequential search (returns the first one index).
        public int FindIndex(string id, bool startswith = false)
        {
            if (items == null)
                return -1;

            for (int i = 0; i < items.Length; i++)
            {
                if (startswith)
                {
                    if (items[i].id.StartsWith(id))
                        return i;
                }
                else
                {
                    if (items[i].id == id)
                        return i;
                }
            }
            return -1;  //Not found
        }

        //ID errors status
        public class IDValidStatus
        {
            public List<int> emptyIndex = new List<int>();
            public HashSet<string> duplicateID = new HashSet<string>();
            public HashSet<string> uniqID = new HashSet<string>();

            public void ResetStatus()
            {
                emptyIndex.Clear();
                duplicateID.Clear();
                uniqID.Clear();
            }

            public string GetEmptyError()
            {
                return (emptyIndex.Count > 0) ? 
                    emptyIndex.Select(e => e.ToString()).Aggregate((s, e) => s + ", " + e) : "";
            }

            public string GetDuplicateError()
            {
                return (duplicateID.Count > 0) ? 
                    string.Join(", ", duplicateID.ToArray()) : "";
            }
        }

        //Check the validity of the ID
        public void CheckIDValidity(ref IDValidStatus idValidStatus)
        {
            idValidStatus.ResetStatus();

            if (items == null || items.Length == 0)
                return;

            for (int i = 0; i < items.Length; i++)
            {
                string id = items[i].id;
                if (!string.IsNullOrEmpty(id))
                {
                    if (idValidStatus.uniqID.Contains(id))
                        idValidStatus.duplicateID.Add(id);
                    else
                        idValidStatus.uniqID.Add(id);
                }
                else
                    idValidStatus.emptyIndex.Add(i);
            }
        }

#endif

#endregion

    }
}
