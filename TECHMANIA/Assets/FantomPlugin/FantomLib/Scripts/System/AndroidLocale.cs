using System;
using System.Collections.Generic;

namespace FantomLib
{
    /// <summary>
    /// Locale for Android
    ///
    ///･Format : "language_country" or "language-country"
    ///･language : ISO 639 alpha-2 or alpha-3 language code
    ///･country(region) : ISO 639 alpha-2 or alpha-3 language code
    /// https://developer.android.com/reference/java/util/Locale
    ///
    /// 
    ///(*) In this plugin '_' and '-' are treated as the same (e.g. "en_US" = "en-US").
    ///    Three or more tags (e.g. "zh_TW_#Hant") can also be entered, but in the system it is equivalent to two tags (e.g. "zh_TW").
    ///    For detailed notation, refer to the following URL.
    /// (Locale list)
    /// http://fantom1x.blog130.fc2.com/blog-entry-295.html
    ///
    /// 
    ///※このプラグインでは '_' と '-' は同じものとして扱われます（例: "en_US" = "en-US"）。
    ///　３つ以上のタグ（e.g. "zh_TW_#Hant"）も入力できますが、システムでは２つのタグ（例: "zh_TW"）と同等になります。
    ///　詳しい表記は、以下の URL を参照して下さい。
    /// (Locale 一覧)
    /// http://fantom1x.blog130.fc2.com/blog-entry-295.html
    /// </summary>
    public static class AndroidLocale
    {
        public static readonly string Default = "(Default)";   //default for display

        //(*) "language_country" may be added (only "language" is all included). It is better to be as unique as possible.
        //    However, depending on the system, "language" and "language_country" are often the same (e.g. "en" = "en_US").
        //(*) Only index 'Default' must be [0]. Otherwise, you can change the order.
        //
        //※"language_country"を追加しても構わない（"language" のみは全て入っている）。なるべくユニークである方が良い。
        //　ただし、システムによっては "language" と "language_country" は同じである場合も多い（例: "ja" = "ja_JP"）。
        //※'Default' のみインデクスが [0] である必要がある。それ以外は順序を替えても構わない。
        public static readonly string[] ConstantValues =
        {
            Default,    //dummy, system default (*Do not change index:[0])

            "en",       //English
            "en_GB",    //United Kingdom (Great Britain)
            "ja",       //Japanese
            "ko",       //Korean
            "zh",       //Chinese
            "de",       //German
            "fr",       //French
            "it",       //Italian
            "es",       //Spanish
            "pt",       //Portuguese

            //(see more)
            //http://fantom1x.blog130.fc2.com/blog-entry-295.html
            "af",
            "agq",
            "ak",
            "am",
            "ar",
            "as",
            "asa",
            "az",
            "bas",
            "be",
            "bem",
            "bez",
            "bg",
            "bm",
            "bn",
            "bo",
            "br",
            "brx",
            "bs",
            "ca",
            "ce",
            "cgg",
            "chr",
            "cs",
            "cy",
            "da",
            "dav",
            "dje",
            "dsb",
            "dua",
            "dyo",
            "dz",
            "ebu",
            "ee",
            "el",
            "eo",
            "et",
            "eu",
            "ewo",
            "fa",
            "ff",
            "fi",
            "fil",
            "fo",
            "fur",
            "fy",
            "ga",
            "gd",
            "gl",
            "gsw",
            "gu",
            "guz",
            "gv",
            "ha",
            "haw",
            "hi",
            "hr",
            "hsb",
            "hu",
            "hy",
            "ig",
            "ii",
            "in",
            "is",
            "iw",
            "jgo",
            "ji",
            "jmc",
            "ka",
            "kab",
            "kam",
            "kde",
            "kea",
            "khq",
            "ki",
            "kk",
            "kkj",
            "kl",
            "kln",
            "km",
            "kn",
            "kok",
            "ks",
            "ksb",
            "ksf",
            "ksh",
            "kw",
            "ky",
            "lag",
            "lb",
            "lg",
            "lkt",
            "ln",
            "lo",
            "lrc",
            "lt",
            "lu",
            "luo",
            "luy",
            "lv",
            "mas",
            "mer",
            "mfe",
            "mg",
            "mgh",
            "mgo",
            "mk",
            "ml",
            "mn",
            "mr",
            "ms",
            "mt",
            "mua",
            "my",
            "mzn",
            "naq",
            "nb",
            "nd",
            "ne",
            "nl",
            "nmg",
            "nn",
            "nnh",
            "nus",
            "nyn",
            "om",
            "or",
            "os",
            "pa",
            "pl",
            "ps",
            "qu",
            "rm",
            "rn",
            "ro",
            "rof",
            "ru",
            "rw",
            "rwk",
            "sah",
            "saq",
            "sbp",
            "se",
            "seh",
            "ses",
            "sg",
            "shi",
            "si",
            "sk",
            "sl",
            "smn",
            "sn",
            "so",
            "sq",
            "sr",
            "sv",
            "sw",
            "ta",
            "te",
            "teo",
            "tg",
            "th",
            "ti",
            "tk",
            "to",
            "tr",
            "twq",
            "tzm",
            "ug",
            "uk",
            "ur",
            "uz",
            "vai",
            "vi",
            "vun",
            "wae",
            "xog",
            "yav",
            "yo",
            "zgh",
            "zu",
        };

        //Language only
        public const string English = "en";
        public const string Japanese = "ja";
        public const string Korean = "ko";
        public const string Chinese = "zh";
        public const string Arabic = "ar";
        public const string German = "de";
        public const string Greek = "el";
        public const string Spanish = "es";
        public const string Persian = "fa";
        public const string French = "fr";
        public const string Indonesian = "in";
        public const string Italian = "it";
        public const string Hebrew = "iw";
        public const string Dutch = "nl";
        public const string Portuguese = "pt";
        public const string Russian = "ru";
        public const string Swedish = "sv";
        public const string Thai = "th";
        public const string Turkish = "tr";
        public const string Vietnamese = "vi";

        //Including Country
        public const string US = "en_US";
        public const string UK = "en_GB";
        public const string Canada = "en_CA";
        public const string Japan = "ja_JP";
        public const string Korea = "ko_KR";
        public const string SimplifiedChinese = "zh_CN";
        public const string TraditionalChinese = "zh_TW";
        public const string China = SimplifiedChinese;
        public const string PRC = SimplifiedChinese;
        public const string Taiwan = TraditionalChinese;
        public const string Germany = "de_DE";
        public const string France = "fr_FR";
        public const string CanadaFrench = "fr_CA";
        public const string Italy = "it_IT";
        public const string BrazilPortuguese = "pt_BR";

    }
}
