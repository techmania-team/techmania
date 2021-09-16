using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FantomLib
{
    /// <summary>
    /// Localization of strings
    ///･Search in System language setting -> defaultLanguage (nothing -> "")
    ///·Inspector registration 'list' (List) is converted to Dictionary by 'Initialize()' method and used at runtime.
    ///·The 'id' field is required only when used with 'LocalizeStringResource' (make it a unique ID within the 'LocalizeStringResource').
    ///
    /// [*Specification change point from previous version]
    ///·In the previous version, when the language could not find, forcibly set the language to English or Japanese, font size to DEF_FONTSIZE,
    /// but the current version want to detect the state not found, so the language is 'Unkown', the font size is '0' the specification changed to return.
    ///·Initialize()' method (List -> Dictionary conversion) was automated by checking with the flag.
    ///·Changed the text field (LocalizeString.Data.text) of data for each language to multiple lines ('Multiline' attribute) on the inspector.
    ///
    /// 
    /// 文字列のローカライズ
    ///・システムの言語設定→デフォルト言語設定の順に検索（無いとき ""(空文字)）
    ///・インスペクタ登録用リスト（List）は Initialize() メソッドで Dictionary へ変換して、実行時に使われる。
    ///・ID フィールドは「LocalizeStringResource」で使用するときのみ必要（LocalizeStringResource 内で重複しない ID にする）。
    ///
    /// [※以前のバージョンからの仕様変更箇所]
    ///・以前のバージョンでは言語が見つからないとき、強制的に言語を英語か日本語、フォントサイズを DEF_FONTSIZE に設定していたが、
    ///  現バージョンでは見つからない状態も検出したいので、言語は Unkown、フォントサイズは 0 を返すように仕様変更した。
    ///・「Initialize()」メソッド(リスト→辞書変換)はフラグでチェックすることにより、自動化した。
    ///・各言語ごとのデータのテキストフィールド（LocalizeString.Data.text）をインスペクタ上で複数行（Multiline 属性）に変更した。
    /// </summary>
    [Serializable]
    public class LocalizeString
    {
        //Inspector Settings
        public string id;       //For 'LocalizeStringResource'

        //Default language setting (language not found in System Language)
        //デフォルト言語設定（システム言語で見つからなかったときの言語）
        public SystemLanguage defaultLanguage = SystemLanguage.English;

#region Properties and Local values Section

        private bool initialized = false;       //initialize done flag (false = need 'Initialize()')

        //private const int DEF_FONTSIZE = 14;    //default font size (UI.Text etc.)    //デフォルトのフォントサイズ(UI.Text など)

        /// <summary>
        /// Parameter for each language
        /// 
        /// 各言語ごとのデータ
        /// </summary>
        [Serializable]
        public class Data
        {
            public SystemLanguage language;
            [Multiline] public string text;
            public int fontSize;

            public Data(SystemLanguage language = SystemLanguage.English, string text = "", int fontSize = 0)
            {
                this.language = language;
                this.text = text;
                this.fontSize = fontSize;
            }

            public Data Clone()
            {
                return (Data)MemberwiseClone();
            }
        }

        //For Inspector or constructor setting
        //Note: List will not be used at runtime -> convert to Dictionary (table) (Converted with 'Initialize()')
        //インスペクタ or コンストラクタ登録用
        //※実行中には使用しないので注意 → 実行中は辞書(table)を使う（Initialize() で変換される）
        [SerializeField]
        private List<Data> list = new List<Data>()
        {
            new Data(SystemLanguage.Japanese, "日本語"),
            new Data(SystemLanguage.ChineseSimplified, "简体中文"),
            new Data(SystemLanguage.Korean, "한국어"),
            new Data(SystemLanguage.English, "English"),
        };


        //Data acquisition table (for runtime)
        //データ取得用テーブル（実行時用）
        private Dictionary<SystemLanguage, Data> table = new Dictionary<SystemLanguage, Data>();


        //Return data with indexer[language] (null if none)
        //インデクサ[言語] でデータを返す（無いときは null）
        public Data this[SystemLanguage language] {
            get {
                if (!initialized)
                    Initialize();
                if (table.ContainsKey(language))
                    return table[language];
                return null;
            }
        }


        //Language property (determined from the language setting of the current system)
        //･Search in System language setting -> defaultLanguage (nothing = Unknown)
        //言語のプロパティ（現在のシステムの言語設定から判別）
        //・データに言語がないとき→デフォルト言語の順に検索（無いとき=Unknown）
        public SystemLanguage Language {
            get {
                if (!initialized)
                    Initialize();
                if (table.ContainsKey(Application.systemLanguage))
                    return Application.systemLanguage;
                if (table.ContainsKey(defaultLanguage))
                    return defaultLanguage;

                //(*) In the previous version, when the language could not find, forcibly set the language to English or Japanese, 
                //    but the current version want to detect the state not found, so the language is 'Unkown' the specification changed to return.
                //※ 以前のバージョンでは言語が見つからないとき、強制的に英語か日本語に設定していたが、
                //   現バージョンでは見つからない状態も検出したいので、Unkownを返すように仕様変更した。
                //if (table.ContainsKey(SystemLanguage.English))
                //    return SystemLanguage.English;
                //if (table.ContainsKey(SystemLanguage.Japanese))
                //    return SystemLanguage.Japanese;

                return SystemLanguage.Unknown;
            }
        }


        //Object.ToString() override
        public override string ToString()
        {
            return Text;
        }

        //Localized string property (Data.text)
        //･Search in System language setting -> defaultLanguage (nothing -> "")
        //文字列取得のプロパティ（Data.text を取得）
        //・システム言語が見つからないとき→デフォルト言語（無いとき=""(空文字)）
        public string Text {
            get {
                if (!initialized)
                    Initialize();
                if (Language != SystemLanguage.Unknown)
                    return table[Language].text;
                return "";
            }
        }

        //If the text is empty, the default value will be returned instead.
        public string TextOrDefault(string def) {
            string t = Text;
            return string.IsNullOrEmpty(t) ? def : t;
        }

        //Specify the language and get the text (not found = defaultLanguage's text or 'def')
        public string TextByLanguage(SystemLanguage language, string def = "", bool notFoundIsDefaultLanguage = true)
        {
            {
                Data data = this[language];
                if (data != null)
                    return data.text;
            }
            if (notFoundIsDefaultLanguage)
            {
                Data data = this[Language];
                if (data != null)
                    return data.text;
            }
            return def;
        }


        //Font size property (Data.fontSize)
        //･Search in System language setting -> defaultLanguage (nothing -> 0)
        //フォントサイズのプロパティ（Data.fontSize を取得）
        //・見つからないとき→デフォルト言語の順に検索（無いとき=0）
        public int FontSize {
            get {
                if (!initialized)
                    Initialize();
                if (Language != SystemLanguage.Unknown)
                    return table[Language].fontSize;
                //(*) In the previous version, when the language could not find, forcibly set the font size to DEF_FONTSIZE, 
                //    but the current version want to detect the state not found, so the font size is '0' the specification changed to return.
                //※ 以前のバージョンでは言語が見つからないとき、強制的に DEF_FONTSIZE に設定していたが、
                //   現バージョンでは見つからない状態も検出したいので、0 を返すように仕様変更した。
                return 0;
            }
        }

        //If the font size is 0, the default value will be returned instead.
        public int FontSizeOrDefault(int def)
        {
            int fontSize = FontSize;
            return (fontSize == 0) ? def : fontSize;
        }

        //Specify the language and get the font size (not found = defaultLanguage's font size or 'def')
        public int FontSizeByLanguage(SystemLanguage language, int def = 0, bool notFoundIsDefaultLanguage = true)
        {
            {
                Data data = this[language];
                if (data != null)
                    return data.fontSize;
            }
            if (notFoundIsDefaultLanguage)
            {
                Data data = this[Language];
                if (data != null)
                    return data.fontSize;
            }
            return def;
        }


        //Check empty or duplication from item elements.
        private void CheckForErrors()
        {
            if (list.Count == 0)
            {
                Debug.LogWarning("[LocalizeString] 'List' is empty.");
            }
            else
            {
                if (table.Count != list.Count)
                    Debug.LogError("[LocalizeString] There is duplicate 'Language'.");  //「言語」が重複している
            }
        }

#endregion

        //Constructors
        //コンストラクタ
        public LocalizeString()
        {
        }

        //Constructor to initialize data from list
        //リストからデータを初期化するコンストラクタ
        public LocalizeString(List<Data> list)
        {
            this.list = list;
        }

        //Constructor to default language specification and initialize data from list
        //デフォルト言語指定とリストからデータを初期化するコンストラクタ
        public LocalizeString(SystemLanguage defaultLanguage, List<Data> list)
        {
            this.defaultLanguage = defaultLanguage;
            this.list = list;
        }


        //Create a Dictionary from the List.
        //リストから辞書を作る
        public void Initialize()
        {
            table.Clear();
            foreach (var item in list)
                table[item.language] = item;
            initialized = true;     //initialize done

#if UNITY_EDITOR
            CheckForErrors();    //Check for items (Editor only).
#endif
        }

        //Add to dictionary (* Duplicates are always overwritten)
        //辞書に追加（※重複は常に上書きされる）
        public void Add(Data newData)
        {
            table[newData.language] = newData;
            initialized = false;        //need 'Initialize()' flag
        }

        //Remove from dictionary (* Ignored when not found)
        //辞書から削除（※見つからないときは無視される）
        public void Remove(Data delData)
        {
            if (table.ContainsKey(delData.language))
            {
                table.Remove(delData.language);
                initialized = false;    //need 'Initialize()' flag
            }
        }

        //Remove from dictionary
        //辞書から削除（言語をキーにして検索）
        public void Remove(SystemLanguage language)
        {
            if (table.ContainsKey(language))
            {
                table.Remove(language);
                initialized = false;    //need 'Initialize()' flag
            }
        }


        //Deep copy
        public LocalizeString Clone()
        {
            LocalizeString loc = (LocalizeString)MemberwiseClone();
            loc.initialized = false;
            loc.list = new List<Data>(this.list.Count);
            foreach (var item in this.list)
                loc.list.Add(item.Clone());
            return loc;
        }
    }
}
